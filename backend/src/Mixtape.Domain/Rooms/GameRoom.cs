using System.Security.Cryptography;
using System.Text;
using Mixtape.Domain.Common;

namespace Mixtape.Domain.Rooms;

public sealed class GameRoom
{
    public const int MinPlayers = 3;
    public const int MaxPlayers = 8;

    // Så länge får ett rum stå helt tomt innan det städas bort.
    public static readonly TimeSpan AbandonedAfter = TimeSpan.FromMinutes(30);

    public static readonly TimeSpan VotingDuration = TimeSpan.FromSeconds(30);

    // Storskärmen visar bidragen först och ställningen de sista 8 sekunderna. Delningen står i frontendens revealing-feature.
    public static readonly TimeSpan RevealingDuration = TimeSpan.FromSeconds(20);

    private readonly List<Player> _players = [];

    private readonly PhaseClock _clock = new();

    private readonly List<string> _prompts = [];

    private readonly HashSet<string> _usedPrompts = [];

    private readonly List<Entry> _entries = [];

    private readonly Random _random;

    // Rundans röster: spelarens id och numret på bidraget som fick rösten. Nollställs när nästa runda börjar.
    private readonly Dictionary<string, int> _votes = [];

    private bool _previousRoundSkipped;

    // Klippet som spelas, som plats i bidragen. Bidragen står i uppspelningens ordning när uppspelningen börjat.
    private int _clipIndex;

    private GameRoom(RoomCode code, string hostToken, Random random)
    {
        Code = code;
        HostToken = hostToken;
        _random = random;
    }

    public RoomCode Code { get; }

    public string HostToken { get; }

    public GamePhase Phase { get; private set; } = GamePhase.Lobby;

    public GameSettings Settings { get; private set; } = GameSettings.Default;

    // 1 i första rundan och 0 i lobbyn.
    public int Round { get; private set; }

    // Rundans prompt, från inskicket till avslöjandet.
    public string? Prompt => Phase is GamePhase.Lobby or GamePhase.GameOver ? null : _prompts[Round - 1];

    // Alla promptar som rummet har använt under kvällen, så att nästa spel kan dra andra.
    public IReadOnlyCollection<string> UsedPrompts => _usedPrompts;

    public IReadOnlyList<Player> Players => _players;

    // Rundans bidrag, i uppspelningens slumpade ordning från och med uppspelningen. Nollställs när nästa runda börjar.
    public IReadOnlyList<Entry> Entries => _entries;

    // Bidraget som spelas, eller null utanför uppspelningen.
    public Entry? CurrentClip => Phase == GamePhase.Playing ? _entries[_clipIndex] : null;

    // 1 för det första klippet.
    public int ClipNumber => _clipIndex + 1;

    // Sant under inskicket i rundan efter en runda som hoppades över, så att storskärmen kan förklara varför prompten byttes.
    public bool SkippedRound => Phase == GamePhase.Submitting && _previousRoundSkipped;

    // Räknas upp vid varje ändring. Ögonblicksbilderna skickas utanför rummets lås och kan komma fram i fel ordning, så klienten behåller den med högst nummer.
    public int Revision { get; private set; }

    public bool CanStart => _players.Count >= MinPlayers;

    public bool IsHostConnected { get; private set; } = true;

    // Värden har pausat med flit. Pausen ligger kvar även om storskärmen laddas om, tills värden fortsätter.
    public bool IsPausedByHost { get; private set; }

    // Utan storskärmen, eller när värden pausat, står spelet still och fasens klocka fryses.
    public bool IsPaused => !IsHostConnected || IsPausedByHost;

    // När båda gäller vinner HostAway, eftersom storskärmen då inte kan fortsätta förrän den är tillbaka.
    public PauseReason? PauseReason => !IsHostConnected ? Rooms.PauseReason.HostAway : IsPausedByHost ? Rooms.PauseReason.PausedByHost : null;

    // När den sista anslutningen försvann, eller null så länge någon är ansluten. En lobby där alla väntar framför TV:n räknas alltså aldrig som inaktiv.
    public DateTimeOffset? EmptySince { get; private set; }

    private bool IsAnyoneConnected => IsHostConnected || _players.Any(player => player.IsConnected);

    // Ett nytt rum börjar i lobbyn med en egen hemlig värd-token. random slumpar uppspelningens ordning, och testerna seedar den.
    // Avsteg från #20, som ville ha random som argument när uppspelningen börjar: den börjar både i Tick och i det sista inskicket, och båda vägarna skulle då behöva den.
    public static GameRoom Create(RoomCode code, Random random) => new(code, SecretToken.Create(), random);

    // Jämför i konstant tid, så att svarstiden inte avslöjar hur stor del av en gissad token som stämde.
    public bool IsHost(string? hostToken) =>
        hostToken is not null && CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(hostToken), Encoding.UTF8.GetBytes(HostToken));

    public bool IsAbandoned(DateTimeOffset now) => EmptySince is { } since && now - since >= AbandonedAfter;

    public bool HasSubmitted(string playerId) => _entries.Any(entry => entry.PlayerId == playerId);

    public bool HasVoted(string playerId) => _votes.ContainsKey(playerId);

    // Numret på spelarens eget bidrag, 1 för det första i uppspelningen, eller null om spelaren inte skickade in.
    public int? EntryNumberOf(string playerId) => _entries.FindIndex(entry => entry.PlayerId == playerId) is >= 0 and var index ? index + 1 : null;

    // Numret på bidraget som spelaren röstade på, eller null.
    public int? VoteOf(string playerId) => _votes.TryGetValue(playerId, out var entry) ? entry : null;

    // Antalet röster på bidraget med det numret.
    public int VotesFor(int entry) => _votes.Values.Count(vote => vote == entry);

    // Rösterna på spelarens eget bidrag i rundan, alltså rundans poäng. 0 för den som inte skickade in.
    // Avsteg från #22, som ville spara rundans poäng i rummet: de räknas ur rösterna, som ligger kvar tills nästa runda börjar och även efter sista rundan, så en egen kopia kan aldrig skilja sig från dem.
    public int RoundPointsOf(string playerId) => EntryNumberOf(playerId) is { } entry ? VotesFor(entry) : 0;

    // Flest poäng först. Vid lika poäng delas placeringen, och spelarna står i den ordning de gick med, eftersom OrderByDescending är stabil och _players står i den ordningen.
    public IReadOnlyList<Standing> Standings =>
        _players.OrderByDescending(player => player.Score).Select(player => new Standing(player, 1 + _players.Count(other => other.Score > player.Score))).ToList();

    // Tiden som är kvar av fasen, eller null i faser utan deadline.
    public TimeSpan? RemainingTime(DateTimeOffset now) => _clock.Remaining(now);

    // Bara inställningen som ändras skickas, och null behåller de andra. Två snabba klick skriver då inte över varandra med ett gammalt värde.
    public Result UpdateSettings(string? hostToken, int? rounds, int? clipSeconds, int? submittingSeconds)
    {
        if (!IsHost(hostToken))
        {
            return RoomErrors.NotFound;
        }

        if (Phase != GamePhase.Lobby)
        {
            return RoomErrors.AlreadyStarted;
        }

        var parsed = GameSettings.Create(rounds ?? Settings.Rounds, clipSeconds ?? Settings.ClipSeconds, submittingSeconds ?? Settings.SubmittingSeconds);
        if (!parsed.IsSuccess)
        {
            return parsed.Error!;
        }

        if (parsed.Value != Settings)
        {
            Settings = parsed.Value;
            Revision++;
        }

        return Result.Success();
    }

    // prompts är en per runda, dragna av servern. Fel antal är en bugg i den som drog dem, inte ett regelfel.
    public Result StartGame(string? hostToken, IReadOnlyList<string> prompts, DateTimeOffset now)
    {
        if (!IsHost(hostToken))
        {
            return RoomErrors.NotFound;
        }

        if (Phase != GamePhase.Lobby)
        {
            return RoomErrors.AlreadyStarted;
        }

        if (!CanStart)
        {
            return RoomErrors.NotEnoughPlayers;
        }

        if (prompts.Count != Settings.Rounds || prompts.Distinct().Count() != prompts.Count)
        {
            throw new ArgumentException($"En prompt per runda behövs, alla olika: {Settings.Rounds} rundor men {prompts.Count} promptar, varav {prompts.Distinct().Count()} olika.", nameof(prompts));
        }

        _prompts.Clear();
        _prompts.AddRange(prompts);
        _usedPrompts.UnionWith(prompts);
        StartRound(1, skippedPrevious: false, now);
        return Result.Success();
    }

    // Efter slutställningen: samma spelare, även de som är borta, och samma inställningar, men poängen börjar om i lobbyn.
    // De använda promptarna står kvar, så att nästa StartGame drar nya.
    public Result PlayAgain(string? hostToken)
    {
        if (!IsHost(hostToken))
        {
            return RoomErrors.NotFound;
        }

        if (Phase != GamePhase.GameOver)
        {
            return RoomErrors.WrongPhase;
        }

        foreach (var player in _players)
        {
            player.ResetScore();
        }

        Round = 0;
        _previousRoundSkipped = false;
        _prompts.Clear();
        ClearRound();
        Phase = GamePhase.Lobby;
        _clock.Stop();
        Revision++;
        return Result.Success();
    }

    // Spelaren kan bara söka och skicka in medan rundan tar emot bidrag.
    public Result CanChooseTrack() => Phase == GamePhase.Submitting ? Result.Success() : RoomErrors.WrongPhase;

    // Bidraget är låst när det väl skickats in. Är alla klara går rummet vidare direkt, utan att vänta på nästa tick.
    public Result SubmitTrack(string playerId, SubmittedTrack track, DateTimeOffset now)
    {
        if (Phase != GamePhase.Submitting)
        {
            return RoomErrors.WrongPhase;
        }

        if (_players.All(player => player.Id != playerId))
        {
            return PlayerErrors.NotFound;
        }

        if (HasSubmitted(playerId))
        {
            return EntryErrors.AlreadySubmitted;
        }

        if (_entries.Any(entry => entry.Track.IsSameSongAs(track)))
        {
            return EntryErrors.TrackTaken;
        }

        _entries.Add(new Entry(playerId, track, now));
        Revision++;
        if (CanCompleteEarly)
        {
            CompletePhase(now);
        }

        return Result.Success();
    }

    // Spelaren kan bara se sin egen röstsedel medan rundan röstar.
    public Result CanVote() => Phase == GamePhase.Voting ? Result.Success() : RoomErrors.WrongPhase;

    // entry är bidragets nummer i uppspelningen. Rösten är låst när den väl lagts. Har alla röstat går rummet vidare direkt.
    public Result Vote(string playerId, int entry, DateTimeOffset now)
    {
        if (Phase != GamePhase.Voting)
        {
            return RoomErrors.WrongPhase;
        }

        if (_players.All(player => player.Id != playerId))
        {
            return PlayerErrors.NotFound;
        }

        if (HasVoted(playerId))
        {
            return VoteErrors.AlreadyVoted;
        }

        if (entry < 1 || entry > _entries.Count)
        {
            return VoteErrors.NotFound;
        }

        if (EntryNumberOf(playerId) == entry)
        {
            return VoteErrors.OwnEntry;
        }

        _votes[playerId] = entry;
        Revision++;
        if (CanCompleteEarly)
        {
            CompletePhase(now);
        }

        return Result.Success();
    }

    // Storskärmen hämtar klippets länk själv, så att länken aldrig finns i bilden som alla får.
    public Result<Entry> CurrentClipFor(string? hostToken)
    {
        if (!IsHost(hostToken))
        {
            return RoomErrors.NotFound;
        }

        return CurrentClip is { } clip ? clip : RoomErrors.WrongPhase;
    }

    // En ny länk för en låt efter en lång paus. Låten letas upp på id, eftersom klippet kan ha bytts medan länken hämtades.
    public Result RenewLink(long trackId, string previewUrl, DateTimeOffset now)
    {
        if (Phase != GamePhase.Playing)
        {
            return RoomErrors.WrongPhase;
        }

        var index = _entries.FindIndex(entry => entry.Track.Id == trackId);
        if (index >= 0)
        {
            _entries[index] = _entries[index] with { Track = _entries[index].Track with { PreviewUrl = previewUrl }, LinkFetchedAt = now };
        }

        return Result.Success();
    }

    // Anropas av klockan på servern. Ger true när fasen bytts, så att bilden bara skickas vid en ändring.
    // Fasen tar också slut här när alla är klara, eftersom den sista som inte var klar kan ha tappat anslutningen.
    public bool Tick(DateTimeOffset now)
    {
        if (!_clock.IsExpired(now) && !CanCompleteEarly)
        {
            return false;
        }

        CompletePhase(now);
        return true;
    }

    // Spelaren får den första lediga färgen, så att en färg som frigjorts när någon lämnat delas ut igen.
    public Result<Player> Join(string? name)
    {
        if (Phase != GamePhase.Lobby)
        {
            return RoomErrors.AlreadyStarted;
        }

        if (_players.Count >= MaxPlayers)
        {
            return RoomErrors.Full;
        }

        var parsed = PlayerName.Create(name);
        if (!parsed.IsSuccess)
        {
            return parsed.Error!;
        }

        if (_players.Any(player => player.Name.IsSameAs(parsed.Value)))
        {
            return PlayerErrors.NameTaken;
        }

        var colorIndex = Enumerable.Range(1, MaxPlayers).First(index => _players.All(player => player.ColorIndex != index));
        var joined = Player.Create(parsed.Value, colorIndex);
        _players.Add(joined);
        EmptySince = null;
        Revision++;
        return joined;
    }

    // Spelaren lämnar själv och frigör sin plats och färg. Under spelet finns ingen "Lämna".
    // now behövs eftersom den som lämnar kan vara den sista som var ansluten.
    public Result<Player> Leave(string? playerToken, DateTimeOffset now)
    {
        if (Phase != GamePhase.Lobby)
        {
            return RoomErrors.AlreadyStarted;
        }

        var left = RemoveWhere(player => player.HasToken(playerToken));
        UpdateEmptySince(now);
        return left;
    }

    // Värden tar bort en spelare i lobbyn. Spelaren kan sedan gå med igen som ny.
    public Result<Player> RemovePlayer(string? hostToken, string? playerId)
    {
        // Värd-token kontrolleras före fasen, så att den som inte är värd bara får veta att rummet inte finns.
        if (!IsHost(hostToken))
        {
            return RoomErrors.NotFound;
        }

        if (Phase != GamePhase.Lobby)
        {
            return RoomErrors.AlreadyStarted;
        }

        return RemoveWhere(player => player.Id == playerId);
    }

    // Mobilen kommer tillbaka med sin token efter en tappad anslutning. Ingen faskontroll, eftersom platsen behålls hela spelet.
    public Result<Player> Rejoin(string? playerToken) => SetPlayerConnected(player => player.HasToken(playerToken), true);

    // Servern vet vilken spelare anslutningen tillhörde, så id räcker här. En spelare som redan tagits bort ger NotFound.
    public Result<Player> Disconnect(string playerId, DateTimeOffset now)
    {
        var disconnected = SetPlayerConnected(player => player.Id == playerId, false);
        UpdateEmptySince(now);
        return disconnected;
    }

    // Närvaron nedan gäller i alla faser, precis som Rejoin, så ingen av metoderna kontrollerar fasen.
    // Storskärmen är tillbaka, efter en omladdning eller ett avbrott i wifi, och fasen får tillbaka tiden som var kvar.
    public Result ResumeHost(string? hostToken, DateTimeOffset now)
    {
        if (!IsHost(hostToken))
        {
            return RoomErrors.NotFound;
        }

        SetHostConnected(true);
        if (!IsPausedByHost)
        {
            _clock.Resume(now);
        }

        return Result.Success();
    }

    // Bara faser med nedräkning går att pausa. En paus som redan gäller är ingen ändring.
    public Result Pause(string? hostToken, DateTimeOffset now)
    {
        if (!IsHost(hostToken))
        {
            return RoomErrors.NotFound;
        }

        if (!HasDeadline)
        {
            return RoomErrors.WrongPhase;
        }

        if (!IsPausedByHost)
        {
            IsPausedByHost = true;
            _clock.Pause(now);
            Revision++;
        }

        return Result.Success();
    }

    public Result Continue(string? hostToken, DateTimeOffset now)
    {
        if (!IsHost(hostToken))
        {
            return RoomErrors.NotFound;
        }

        if (!HasDeadline)
        {
            return RoomErrors.WrongPhase;
        }

        if (IsPausedByHost)
        {
            IsPausedByHost = false;
            if (IsHostConnected)
            {
                _clock.Resume(now);
            }

            Revision++;
        }

        return Result.Success();
    }

    public void DisconnectHost(DateTimeOffset now)
    {
        SetHostConnected(false);
        _clock.Pause(now);
        UpdateEmptySince(now);
    }

    // Värden avslutar i förtid och alla går till slutställningen. Bara färdiga rundor räknas, så en avbruten runda tas bort med sina röster.
    // Klockan återupptas innan den stoppas, eftersom en fryst klocka annars skulle frysa första fasen i nästa spel.
    public Result End(string? hostToken, DateTimeOffset now)
    {
        if (!IsHost(hostToken))
        {
            return RoomErrors.NotFound;
        }

        if (!HasDeadline)
        {
            return RoomErrors.WrongPhase;
        }

        if (Phase != GamePhase.Revealing)
        {
            ClearRound();
        }

        IsPausedByHost = false;
        if (IsHostConnected)
        {
            _clock.Resume(now);
        }

        EndGame();
        return Result.Success();
    }

    private bool HasDeadline => Phase is GamePhase.Submitting or GamePhase.Playing or GamePhase.Voting or GamePhase.Revealing;

    // Under paus står spelet still även när alla är klara. När storskärmen är tillbaka går fasen vidare vid nästa tick.
    private bool CanCompleteEarly => !IsPaused && IsEveryoneDone;

    // Bara anslutna spelare räknas, så en mobil som är borta hindrar inte de andra. Utan någon ansluten gäller deadline.
    // Med färre än 2 bidrag väntar inskicket till deadline, så att den som är borta hinner tillbaka innan rundan hoppas över.
    private bool IsEveryoneDone
    {
        get
        {
            var connected = _players.Where(player => player.IsConnected).ToList();
            return Phase switch
            {
                GamePhase.Submitting => _entries.Count >= 2 && connected.Count > 0 && connected.All(player => HasSubmitted(player.Id)),
                GamePhase.Voting => connected.Count > 0 && connected.All(player => HasVoted(player.Id)),
                _ => false,
            };
        }
    }

    // Fasen är slut, vid sin deadline eller när alla är klara, och nästa fas börjar med en ny klocka.
    private void CompletePhase(DateTimeOffset now)
    {
        switch (Phase)
        {
            case GamePhase.Submitting when _entries.Count < 2:
                SkipRound(now);
                break;
            case GamePhase.Submitting:
                StartPlaying(now);
                break;
            case GamePhase.Playing when _clipIndex < _entries.Count - 1:
                _clipIndex++;
                Enter(GamePhase.Playing, Settings.ClipLength, now);
                break;
            case GamePhase.Playing:
                Enter(GamePhase.Voting, VotingDuration, now);
                break;
            case GamePhase.Voting:
                AwardPoints();
                Enter(GamePhase.Revealing, RevealingDuration, now);
                break;
            case GamePhase.Revealing when Round < Settings.Rounds:
                StartRound(Round + 1, skippedPrevious: false, now);
                break;
            case GamePhase.Revealing:
                EndGame();
                break;
            default:
                throw new InvalidOperationException($"Fasen {Phase} har ingen deadline att nå.");
        }
    }

    // En runda med färre än 2 bidrag har inget att rösta om. Den räknas ändå som spelad, så att spelets längd går att förutse.
    private void SkipRound(DateTimeOffset now)
    {
        if (Round < Settings.Rounds)
        {
            StartRound(Round + 1, skippedPrevious: true, now);
        }
        else
        {
            EndGame();
        }
    }

    // Ordningen slumpas här, så att den inte avslöjar i vilken ordning bidragen kom in.
    private void StartPlaying(DateTimeOffset now)
    {
        var shuffled = _entries.ToArray();
        _random.Shuffle(shuffled);
        _entries.Clear();
        _entries.AddRange(shuffled);
        _clipIndex = 0;
        Enter(GamePhase.Playing, Settings.ClipLength, now);
    }

    // Röstningen är slut, vid deadline eller när alla röstat. Varje röst ger 1 poäng till bidragets ägare.
    private void AwardPoints()
    {
        foreach (var entry in _votes.Values)
        {
            _players.First(player => player.Id == _entries[entry - 1].PlayerId).AddPoints(1);
        }
    }

    private void StartRound(int round, bool skippedPrevious, DateTimeOffset now)
    {
        Round = round;
        _previousRoundSkipped = skippedPrevious;
        ClearRound();
        Enter(GamePhase.Submitting, Settings.SubmittingDuration, now);
    }

    // Rundans bidrag och röster, som nollställs när en ny runda börjar, när spelet börjar om och när en avbruten runda tas bort.
    private void ClearRound()
    {
        _entries.Clear();
        _votes.Clear();
    }

    private void EndGame()
    {
        Phase = GamePhase.GameOver;
        _clock.Stop();
        Revision++;
    }

    private void Enter(GamePhase phase, TimeSpan duration, DateTimeOffset now)
    {
        Phase = phase;
        _clock.Start(duration, now);
        Revision++;
    }

    // Klockan startar när den sista anslutningen försvinner och nollställs när någon kommer tillbaka. Ett rum som redan är tomt behåller sin tid.
    private void UpdateEmptySince(DateTimeOffset now) => EmptySince = IsAnyoneConnected ? null : EmptySince ?? now;

    private void SetHostConnected(bool connected)
    {
        if (connected)
        {
            EmptySince = null;
        }

        if (IsHostConnected != connected)
        {
            IsHostConnected = connected;
            Revision++;
        }
    }

    private Result<Player> SetPlayerConnected(Func<Player, bool> match, bool connected)
    {
        var player = _players.FirstOrDefault(match);
        if (player is null)
        {
            return PlayerErrors.NotFound;
        }

        if (connected)
        {
            EmptySince = null;
        }

        if (player.SetConnected(connected))
        {
            Revision++;
        }

        return player;
    }

    private Result<Player> RemoveWhere(Func<Player, bool> match)
    {
        var player = _players.FirstOrDefault(match);
        if (player is null)
        {
            return PlayerErrors.NotFound;
        }

        _players.Remove(player);
        Revision++;
        return player;
    }
}
