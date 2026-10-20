using System.Security.Cryptography;
using System.Text;
using Mixtape.Domain.Common;

namespace Mixtape.Domain.Rooms;

// Spelarna läggs till i #10.
public sealed class GameRoom
{
    private GameRoom(RoomCode code, string hostToken)
    {
        Code = code;
        HostToken = hostToken;
    }

    public RoomCode Code { get; }

    public string HostToken { get; }

    public GamePhase Phase { get; } = GamePhase.Lobby;

    // Ett nytt rum börjar i lobbyn med en egen hemlig värd-token.
    public static GameRoom Create(RoomCode code) => new(code, SecretToken.Create());

    // Jämför i konstant tid, så att svarstiden inte avslöjar hur stor del av en gissad token som stämde.
    public bool IsHost(string? hostToken) =>
        hostToken is not null && CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(hostToken), Encoding.UTF8.GetBytes(HostToken));
}
