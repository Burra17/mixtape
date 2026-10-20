using System.Security.Cryptography;
using System.Text;
using Mixtape.Domain.Common;

namespace Mixtape.Domain.Rooms;

public sealed class GameRoom
{
    public const int MinPlayers = 3;
    public const int MaxPlayers = 8;

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

    // Ett nytt rum börjar i lobbyn med en egen hemlig värd-token.
    public static GameRoom Create(RoomCode code) => new(code, SecretToken.Create());

    // Jämför i konstant tid, så att svarstiden inte avslöjar hur stor del av en gissad token som stämde.
    public bool IsHost(string? hostToken) =>
        hostToken is not null && CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(hostToken), Encoding.UTF8.GetBytes(HostToken));

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
        Revision++;
        return joined;
    }

    // Spelaren lämnar själv och frigör sin plats och färg. Under spelet finns ingen "Lämna".
    public Result<Player> Leave(string? playerToken)
    {
        if (Phase != GamePhase.Lobby)
        {
            return RoomErrors.AlreadyStarted;
        }

        return RemoveWhere(player => player.HasToken(playerToken));
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
