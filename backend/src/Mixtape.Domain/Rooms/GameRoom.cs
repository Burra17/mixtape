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

    public static readonly TimeSpan SubmittingDuration = TimeSpan.FromSeconds(45);

    public static readonly TimeSpan VotingDuration = TimeSpan.FromSeconds(20);

    // Platshållare tills #22 ger avslöjandet sitt innehåll.
    public static readonly TimeSpan RevealingDuration = TimeSpan.FromSeconds(15);

    private readonly List<Player> _players = [];

    private readonly PhaseClock _clock = new();

    private GameRoom(RoomCode code, string hostToken)
    {
        Code = code;
        HostToken = hostToken;
    }

    public RoomCode Code { get; }

    public string HostToken { get; }

    public GamePhase Phase { get; private set; } = GamePhase.Lobby;

    public GameSettings Settings { get; private set; } = GameSettings.Default;

    // 1 i första rundan och 0 i lobbyn.
    public int Round { get; private set; }

    public IReadOnlyList<Player> Players => _players;

    // Räknas upp vid varje ändring. Ögonblicksbilderna skickas utanför rummets lås och kan komma fram i fel ordning, så klienten behåller den med högst nummer.
    public int Revision { get; private set; }

    public bool CanStart => _players.Count >= MinPlayers;

    public bool IsHostConnected { get; private set; } = true;

    // Utan storskärmen står spelet still, och fasens klocka fryses.
    public bool IsPaused => !IsHostConnected;

    // När den sista anslutningen försvann, eller null så länge någon är ansluten. En lobby där alla väntar framför TV:n räknas alltså aldrig som inaktiv.
    public DateTimeOffset? EmptySince { get; private set; }

    private bool IsAnyoneConnected => IsHostConnected || _players.Any(player => player.IsConnected);

    // Platshållare tills #20 spelar ett klipp per bidrag.
    private TimeSpan PlayingDuration => Settings.ClipLength * _players.Count;

    // Ett nytt rum börjar i lobbyn med en egen hemlig värd-token.
    public static GameRoom Create(RoomCode code) => new(code, SecretToken.Create());

    // Jämför i konstant tid, så att svarstiden inte avslöjar hur stor del av en gissad token som stämde.
    public bool IsHost(string? hostToken) =>
        hostToken is not null && CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(hostToken), Encoding.UTF8.GetBytes(HostToken));

    public bool IsAbandoned(DateTimeOffset now) => EmptySince is { } since && now - since >= AbandonedAfter;

    // Tiden som är kvar av fasen, eller null i faser utan deadline.
    public TimeSpan? RemainingTime(DateTimeOffset now) => _clock.Remaining(now);

    public Result UpdateSettings(string? hostToken, int rounds, int clipSeconds)
    {
        if (!IsHost(hostToken))
        {
            return RoomErrors.NotFound;
        }

        if (Phase != GamePhase.Lobby)
        {
            return RoomErrors.AlreadyStarted;
        }

        var parsed = GameSettings.Create(rounds, clipSeconds);
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

    public Result StartGame(string? hostToken, DateTimeOffset now)
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

        Round = 1;
        Enter(GamePhase.Submitting, SubmittingDuration, now);
        return Result.Success();
    }

    // Anropas av klockan på servern. Ger true när fasen bytts, så att bilden bara skickas vid en ändring.
    public bool Tick(DateTimeOffset now)
    {
        if (!_clock.IsExpired(now))
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
        _clock.Resume(now);
        return Result.Success();
    }

    public void DisconnectHost(DateTimeOffset now)
    {
        SetHostConnected(false);
        _clock.Pause(now);
        UpdateEmptySince(now);
    }

    // Fasen är slut, vid sin deadline eller när alla är klara (#19), och nästa fas börjar med en ny klocka.
    private void CompletePhase(DateTimeOffset now)
    {
        switch (Phase)
        {
            case GamePhase.Submitting:
                Enter(GamePhase.Playing, PlayingDuration, now);
                break;
            case GamePhase.Playing:
                Enter(GamePhase.Voting, VotingDuration, now);
                break;
            case GamePhase.Voting:
                Enter(GamePhase.Revealing, RevealingDuration, now);
                break;
            case GamePhase.Revealing when Round < Settings.Rounds:
                Round++;
                Enter(GamePhase.Submitting, SubmittingDuration, now);
                break;
            case GamePhase.Revealing:
                Phase = GamePhase.GameOver;
                _clock.Stop();
                Revision++;
                break;
            default:
                throw new InvalidOperationException($"Fasen {Phase} har ingen deadline att nå.");
        }
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
