using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Text;
using Mixtape.Domain.Common;
using Mixtape.Domain.Games;

namespace Mixtape.Domain.Rooms;

// Det som är gemensamt för alla spellägen: spelarna, värden, lobbyn, klockan, pausen och ställningen. Själva spelet är ett IGame.
public sealed class GameRoom
{
    public const int MaxPlayers = 8;

    // Så länge får ett rum stå helt tomt innan det städas bort.
    public static readonly TimeSpan AbandonedAfter = TimeSpan.FromMinutes(30);

    private readonly List<Player> _players = [];

    private readonly PhaseClock _clock = new();

    // Det som spelen dragit under kvällen, per spelläge, med det som användes längst tillbaka först, så att nästa spel kan dra annat.
    private readonly Dictionary<Type, List<string>> _usedContent = [];

    // Inställningarna för lägen som valts förut under kvällen. Det valda lägets står i Settings.
    private readonly Dictionary<GameMode, IGameSettings> _settingsByMode = [];

    private GameRoom(RoomCode code, string hostToken)
    {
        Code = code;
        HostToken = hostToken;
    }

    public RoomCode Code { get; }

    public string HostToken { get; }

    public RoomStage Stage { get; private set; } = RoomStage.Lobby;

    // Det valda lägets inställningar. Läget självt är Settings.Mode.
    public IGameSettings Settings { get; private set; } = GameModes.DefaultSettings(GameModes.Default);

    public IReadOnlyList<Player> Players => _players;

    // Spelet som pågår, eller det som just tog slut, så att slutställningen kan visa den sista rundan. Null i lobbyn.
    public IGame? Game { get; private set; }

    // Räknas upp vid varje ändring. Ögonblicksbilderna skickas utanför rummets lås och kan komma fram i fel ordning, så klienten behåller den med högst nummer.
    public int Revision { get; private set; }

    public bool CanStart => _players.Count >= Settings.MinPlayers;

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

    // Ett nytt rum börjar i lobbyn med en egen hemlig värd-token.
    public static GameRoom Create(RoomCode code) => new(code, SecretToken.Create());

    // Jämför i konstant tid, så att svarstiden inte avslöjar hur stor del av en gissad token som stämde.
    public bool IsHost(string? hostToken) =>
        hostToken is not null && CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(hostToken), Encoding.UTF8.GetBytes(HostToken));

    public bool IsAbandoned(DateTimeOffset now) => EmptySince is { } since && now - since >= AbandonedAfter;

    // Det som spel av den typen dragit i rummet under kvällen, med det som användes längst tillbaka först.
    public IReadOnlyList<string> UsedContent<TGame>()
        where TGame : IGame =>
        _usedContent.TryGetValue(typeof(TGame), out var used) ? used : [];

    // Flest poäng först. Vid lika poäng delas placeringen, och spelarna står i den ordning de gick med, eftersom OrderByDescending är stabil och _players står i den ordningen.
    public IReadOnlyList<Standing> Standings =>
        _players.OrderByDescending(player => player.Score).Select(player => new Standing(player, 1 + _players.Count(other => other.Score > player.Score))).ToList();

    // Tiden som är kvar av fasen, eller null i faser utan deadline.
    public TimeSpan? RemainingTime(DateTimeOffset now) => _clock.Remaining(now);

    // Ett läge som valts förut under kvällen får tillbaka sina inställningar.
    public Result ChooseMode(string? hostToken, GameMode mode)
    {
        if (!IsHost(hostToken))
        {
            return RoomErrors.NotFound;
        }

        if (Stage != RoomStage.Lobby)
        {
            return RoomErrors.AlreadyStarted;
        }

        if (!Enum.IsDefined(mode))
        {
            return RoomErrors.InvalidSettings;
        }

        if (mode != Settings.Mode)
        {
            _settingsByMode[Settings.Mode] = Settings;
            Settings = _settingsByMode.TryGetValue(mode, out var remembered) ? remembered : GameModes.DefaultSettings(mode);
            Revision++;
        }

        return Result.Success();
    }

    // change får det valda lägets inställningar och ger de nya. Inställningar för ett annat läge än det valda nekas.
    public Result UpdateSettings<TSettings>(string? hostToken, Func<TSettings, Result<TSettings>> change)
        where TSettings : IGameSettings
    {
        if (!IsHost(hostToken))
        {
            return RoomErrors.NotFound;
        }

        if (Stage != RoomStage.Lobby)
        {
            return RoomErrors.AlreadyStarted;
        }

        if (Settings is not TSettings current)
        {
            return RoomErrors.InvalidSettings;
        }

        var changed = change(current);
        if (!changed.IsSuccess)
        {
            return changed.Error!;
        }

        if (!changed.Value.Equals(current))
        {
            Settings = changed.Value;
            Revision++;
        }

        return Result.Success();
    }

    // Handlern skapar spelet med det som läget behöver, så att rummet inte känner till något spelläge.
    // createGame anropas först när värden, lobbyn och antalet spelare är kontrollerade, med det valda lägets inställningar.
    public Result StartGame(string? hostToken, Func<IGameSettings, Result<IGame>> createGame, DateTimeOffset now)
    {
        if (!IsHost(hostToken))
        {
            return RoomErrors.NotFound;
        }

        if (Stage != RoomStage.Lobby)
        {
            return RoomErrors.AlreadyStarted;
        }

        if (!CanStart)
        {
            return RoomErrors.NotEnoughPlayers;
        }

        var created = createGame(Settings);
        if (!created.IsSuccess)
        {
            return created.Error!;
        }

        var game = created.Value;
        if (!_usedContent.TryGetValue(game.GetType(), out var used))
        {
            used = [];
            _usedContent[game.GetType()] = used;
        }

        // Det som används igen flyttas sist, så att ordningen alltid är den senaste användningen.
        used.RemoveAll(game.UsedContent.Contains);
        used.AddRange(game.UsedContent);
        Game = game;
        Stage = RoomStage.InGame;
        Apply(game.Start(_players), now);
        return Result.Success();
    }

    // Efter slutställningen: samma spelare, även de som är borta, och samma inställningar, men poängen börjar om i lobbyn.
    // Det som spelen dragit står kvar, så att nästa StartGame drar annat.
    public Result PlayAgain(string? hostToken)
    {
        if (!IsHost(hostToken))
        {
            return RoomErrors.NotFound;
        }

        if (Stage != RoomStage.GameOver)
        {
            return RoomErrors.WrongPhase;
        }

        foreach (var player in _players)
        {
            player.ResetScore();
        }

        Game = null;
        Stage = RoomStage.Lobby;
        _clock.Stop();
        Revision++;
        return Result.Success();
    }

    // En spelares kommando till spelet. Rummet kontrollerar att ett spel av rätt typ pågår och att spelaren finns.
    // Är alla klara går rummet vidare direkt, utan att vänta på nästa tick.
    public Result Act<TGame>(string playerId, DateTimeOffset now, Func<TGame, Result> command)
        where TGame : IGame
    {
        if (!TryGetGame<TGame>(out var game))
        {
            return RoomErrors.WrongPhase;
        }

        if (_players.All(player => player.Id != playerId))
        {
            return PlayerErrors.NotFound;
        }

        var result = command(game);
        if (!result.IsSuccess)
        {
            return result;
        }

        Revision++;
        if (CanCompleteEarly)
        {
            CompletePhase(now);
        }

        return result;
    }

    // Läser eller ändrar spelet utan att bilden ändras, till exempel en fråga om fasen eller en ny klipplänk.
    public Result<T> WithGame<TGame, T>(Func<TGame, Result<T>> use)
        where TGame : IGame =>
        TryGetGame<TGame>(out var game) ? use(game) : RoomErrors.WrongPhase;

    public Result WithGame<TGame>(Func<TGame, Result> use)
        where TGame : IGame =>
        TryGetGame<TGame>(out var game) ? use(game) : RoomErrors.WrongPhase;

    // Som WithGame, men bara för storskärmen. Värd-token kontrolleras före fasen, så att den som inte är värd bara får veta att rummet inte finns.
    public Result<T> WithGameForHost<TGame, T>(string? hostToken, Func<TGame, Result<T>> use)
        where TGame : IGame =>
        IsHost(hostToken) ? WithGame(use) : RoomErrors.NotFound;

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
        if (Stage != RoomStage.Lobby)
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
        if (Stage != RoomStage.Lobby)
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

        if (Stage != RoomStage.Lobby)
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

    // Värden avslutar i förtid och alla går till slutställningen. Bara färdiga rundor räknas, så spelet tar bort en avbruten runda.
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

        Game!.AbortRound();
        IsPausedByHost = false;
        if (IsHostConnected)
        {
            _clock.Resume(now);
        }

        EndGame();
        return Result.Success();
    }

    private bool TryGetGame<TGame>([MaybeNullWhen(false)] out TGame game)
        where TGame : IGame
    {
        if (Stage == RoomStage.InGame && Game is TGame current)
        {
            game = current;
            return true;
        }

        game = default;
        return false;
    }

    // Alla spelets faser har en nedräkning, men inte lobbyn och slutställningen.
    private bool HasDeadline => Stage == RoomStage.InGame;

    // Under paus står spelet still även när alla är klara. När storskärmen är tillbaka går fasen vidare vid nästa tick.
    private bool CanCompleteEarly => !IsPaused && IsEveryoneDone;

    // Bara anslutna spelare räknas, så en mobil som är borta hindrar inte de andra. Utan någon ansluten gäller deadline.
    private bool IsEveryoneDone => Stage == RoomStage.InGame && Game!.IsEveryoneDone(_players.Where(player => player.IsConnected).ToList());

    // Fasen är slut, vid sin deadline eller när alla är klara, och spelet säger vad som händer sedan.
    private void CompletePhase(DateTimeOffset now) => Apply(Game!.CompletePhase(), now);

    // Nästa fas börjar med en ny klocka, eller så är spelet slut.
    private void Apply(Step step, DateTimeOffset now)
    {
        if (step.Duration is { } duration)
        {
            _clock.Start(duration, now);
            Revision++;
        }
        else
        {
            EndGame();
        }
    }

    private void EndGame()
    {
        Stage = RoomStage.GameOver;
        _clock.Stop();
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
