using Mixtape.Domain.Common;
using Mixtape.Domain.Music;
using Mixtape.Domain.Rooms;

namespace Mixtape.Domain.Games.AuxCord;

// Aux Cord Battles: alla skickar in en låt till rundans prompt, låtarna spelas anonymt och alla röstar. Varje röst ger 1 poäng.
public sealed class AuxCordGame : IPlaysClips, ISearchesTracks
{
    public static readonly TimeSpan VotingDuration = TimeSpan.FromSeconds(30);

    // Storskärmen visar bidragen först och ställningen de sista 8 sekunderna. Delningen står i frontendens revealing-feature.
    public static readonly TimeSpan RevealingDuration = TimeSpan.FromSeconds(20);

    private readonly AuxCordSettings _settings;

    private readonly IReadOnlyList<string> _prompts;

    private readonly Random _random;

    private readonly List<Entry> _entries = [];

    // Rundans röster: spelarens id och numret på bidraget som fick rösten. Nollställs när nästa runda börjar.
    private readonly Dictionary<string, int> _votes = [];

    private IReadOnlyList<Player> _players = [];

    private bool _previousRoundSkipped;

    // Klippet som spelas, som plats i bidragen. Bidragen står i uppspelningens ordning när uppspelningen börjat.
    private int _clipIndex;

    // prompts är en per runda, dragna av servern. Fel antal är en bugg i den som drog dem, inte ett regelfel.
    // random slumpar uppspelningens ordning, och testerna seedar den.
    public AuxCordGame(IReadOnlyList<string> prompts, AuxCordSettings settings, Random random)
    {
        if (prompts.Count != settings.Rounds || prompts.Distinct().Count() != prompts.Count)
        {
            throw new ArgumentException($"En prompt per runda behövs, alla olika: {settings.Rounds} rundor men {prompts.Count} promptar, varav {prompts.Distinct().Count()} olika.", nameof(prompts));
        }

        _prompts = prompts.ToList();
        _settings = settings;
        _random = random;
    }

    public AuxCordPhase Phase { get; private set; }

    // 1 i första rundan.
    public int Round { get; private set; }

    public string Prompt => _prompts[Round - 1];

    public IReadOnlyCollection<string> UsedContent => _prompts;

    // Rundans bidrag, i uppspelningens slumpade ordning från och med uppspelningen. Nollställs när nästa runda börjar.
    public IReadOnlyList<Entry> Entries => _entries;

    // Bidraget som spelas, eller null utanför uppspelningen.
    public Entry? CurrentClip => Phase == AuxCordPhase.Playing ? _entries[_clipIndex] : null;

    // 1 för det första klippet.
    public int ClipNumber => _clipIndex + 1;

    // Sant under inskicket i rundan efter en runda som hoppades över, så att storskärmen kan förklara varför prompten byttes.
    public bool SkippedRound => Phase == AuxCordPhase.Submitting && _previousRoundSkipped;

    public bool HasSubmitted(string playerId) => _entries.Any(entry => entry.PlayerId == playerId);

    public bool HasVoted(string playerId) => _votes.ContainsKey(playerId);

    // Numret på spelarens eget bidrag, 1 för det första i uppspelningen, eller null om spelaren inte skickade in.
    public int? EntryNumberOf(string playerId) => _entries.FindIndex(entry => entry.PlayerId == playerId) is >= 0 and var index ? index + 1 : null;

    // Numret på bidraget som spelaren röstade på, eller null.
    public int? VoteOf(string playerId) => _votes.TryGetValue(playerId, out var entry) ? entry : null;

    // Antalet röster på bidraget med det numret.
    public int VotesFor(int entry) => _votes.Values.Count(vote => vote == entry);

    // Rösterna på spelarens eget bidrag i rundan, alltså rundans poäng. 0 för den som inte skickade in.
    // Avsteg från #22, som ville spara rundans poäng för sig: de räknas ur rösterna, som ligger kvar tills nästa runda börjar och även efter sista rundan, så en egen kopia kan aldrig skilja sig från dem.
    public int RoundPointsOf(string playerId) => EntryNumberOf(playerId) is { } entry ? VotesFor(entry) : 0;

    public Step Start(IReadOnlyList<Player> players)
    {
        _players = players;
        return StartRound(1, skippedPrevious: false);
    }

    // Spelaren kan bara söka och skicka in medan rundan tar emot bidrag.
    public Result CanSearch() => Phase == AuxCordPhase.Submitting ? Result.Success() : RoomErrors.WrongPhase;

    // Bidraget är låst när det väl skickats in.
    public Result SubmitTrack(string playerId, SubmittedTrack track, DateTimeOffset now)
    {
        if (Phase != AuxCordPhase.Submitting)
        {
            return RoomErrors.WrongPhase;
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
        return Result.Success();
    }

    // Spelaren kan bara se sin egen röstsedel medan rundan röstar.
    public Result CanVote() => Phase == AuxCordPhase.Voting ? Result.Success() : RoomErrors.WrongPhase;

    // entry är bidragets nummer i uppspelningen. Rösten är låst när den väl lagts.
    public Result Vote(string playerId, int entry)
    {
        if (Phase != AuxCordPhase.Voting)
        {
            return RoomErrors.WrongPhase;
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
        return Result.Success();
    }

    public Result<CurrentClip> GetCurrentClip() =>
        CurrentClip is { } entry ? new CurrentClip(ClipNumber, entry.Track, entry.LinkFetchedAt) : RoomErrors.WrongPhase;

    // En ny länk för en låt efter en lång paus.
    public Result RefreshTrack(long trackId, string previewUrl, IReadOnlyList<string>? contributors, DateTimeOffset now)
    {
        if (Phase != AuxCordPhase.Playing)
        {
            return RoomErrors.WrongPhase;
        }

        var index = _entries.FindIndex(entry => entry.Track.Id == trackId);
        if (index >= 0)
        {
            _entries[index] = _entries[index] with { Track = _entries[index].Track with { PreviewUrl = previewUrl, Contributors = contributors ?? _entries[index].Track.Contributors }, LinkFetchedAt = now };
        }

        return Result.Success();
    }

    // Storskärmen visar att klippet inte gick att spela, och klockan går ut som vanligt, eftersom alla ändå ser låten.
    public Step? SkipUnplayable(long trackId) => null;

    // Med färre än 2 bidrag väntar inskicket till deadline, så att den som är borta hinner tillbaka innan rundan hoppas över.
    public bool IsEveryoneDone(IReadOnlyList<Player> connected) =>
        Phase switch
        {
            AuxCordPhase.Submitting => _entries.Count >= 2 && connected.Count > 0 && connected.All(player => HasSubmitted(player.Id)),
            AuxCordPhase.Voting => connected.Count > 0 && connected.All(player => HasVoted(player.Id)),
            _ => false,
        };

    public Step CompletePhase()
    {
        switch (Phase)
        {
            case AuxCordPhase.Submitting when _entries.Count < 2:
                return SkipRound();
            case AuxCordPhase.Submitting:
                return StartPlaying();
            case AuxCordPhase.Playing when _clipIndex < _entries.Count - 1:
                _clipIndex++;
                return Enter(AuxCordPhase.Playing, _settings.ClipLength);
            case AuxCordPhase.Playing:
                return Enter(AuxCordPhase.Voting, VotingDuration);
            case AuxCordPhase.Voting:
                AwardPoints();
                return Enter(AuxCordPhase.Revealing, RevealingDuration);
            case AuxCordPhase.Revealing when Round < _settings.Rounds:
                return StartRound(Round + 1, skippedPrevious: false);
            default:
                return Step.Finished;
        }
    }

    // En avbruten runda tas bort med sina röster. En runda som redan avslöjats är färdig och står kvar.
    public void AbortRound()
    {
        if (Phase != AuxCordPhase.Revealing)
        {
            ClearRound();
        }
    }

    // En runda med färre än 2 bidrag har inget att rösta om. Den räknas ändå som spelad, så att spelets längd går att förutse.
    private Step SkipRound() => Round < _settings.Rounds ? StartRound(Round + 1, skippedPrevious: true) : Step.Finished;

    // Ordningen slumpas här, så att den inte avslöjar i vilken ordning bidragen kom in.
    private Step StartPlaying()
    {
        var shuffled = _entries.ToArray();
        _random.Shuffle(shuffled);
        _entries.Clear();
        _entries.AddRange(shuffled);
        _clipIndex = 0;
        return Enter(AuxCordPhase.Playing, _settings.ClipLength);
    }

    // Röstningen är slut, vid deadline eller när alla röstat. Varje röst ger 1 poäng till bidragets ägare.
    private void AwardPoints()
    {
        foreach (var entry in _votes.Values)
        {
            _players.First(player => player.Id == _entries[entry - 1].PlayerId).AddPoints(1);
        }
    }

    private Step StartRound(int round, bool skippedPrevious)
    {
        Round = round;
        _previousRoundSkipped = skippedPrevious;
        ClearRound();
        return Enter(AuxCordPhase.Submitting, _settings.SubmittingDuration);
    }

    private void ClearRound()
    {
        _entries.Clear();
        _votes.Clear();
    }

    private Step Enter(AuxCordPhase phase, TimeSpan duration)
    {
        Phase = phase;
        return Step.Next(duration);
    }
}
