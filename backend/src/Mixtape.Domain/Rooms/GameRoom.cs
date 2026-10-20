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

    private readonly List<Player> _players = [];

    private GameRoom(RoomCode code, string hostToken)
    {
        Code = code;
        HostToken = hostToken;
    }

    public RoomCode Code { get; }

    public string HostToken { get; }

    public GamePhase Phase { get; } = GamePhase.Lobby;

    public IReadOnlyList<Player> Players => _players;

    // Räknas upp vid varje ändring. Ögonblicksbilderna skickas utanför rummets lås och kan komma fram i fel ordning, så klienten behåller den med högst nummer.
    public int Revision { get; private set; }

    public bool CanStart => _players.Count >= MinPlayers;

    public bool IsHostConnected { get; private set; } = true;

    // Utan storskärmen står spelet still. Timrarna i #17 läser flaggan.
    public bool IsPaused => !IsHostConnected;

    // När den sista anslutningen försvann, eller null så länge någon är ansluten. En lobby där alla väntar framför TV:n räknas alltså aldrig som inaktiv.
    public DateTimeOffset? EmptySince { get; private set; }

    private bool IsAnyoneConnected => IsHostConnected || _players.Any(player => player.IsConnected);

    // Ett nytt rum börjar i lobbyn med en egen hemlig värd-token.
    public static GameRoom Create(RoomCode code) => new(code, SecretToken.Create());

    // Jämför i konstant tid, så att svarstiden inte avslöjar hur stor del av en gissad token som stämde.
    public bool IsHost(string? hostToken) =>
        hostToken is not null && CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(hostToken), Encoding.UTF8.GetBytes(HostToken));

    public bool IsAbandoned(DateTimeOffset now) => EmptySince is { } since && now - since >= AbandonedAfter;

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
    // Storskärmen är tillbaka, efter en omladdning eller ett avbrott i wifi, och pausen hävs.
    public Result ResumeHost(string? hostToken)
    {
        if (!IsHost(hostToken))
        {
            return RoomErrors.NotFound;
        }

        SetHostConnected(true);
        return Result.Success();
    }

    public void DisconnectHost(DateTimeOffset now)
    {
        SetHostConnected(false);
        UpdateEmptySince(now);
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
