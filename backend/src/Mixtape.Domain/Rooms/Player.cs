using System.Security.Cryptography;
using System.Text;
using Mixtape.Domain.Common;

namespace Mixtape.Domain.Rooms;

public sealed class Player
{
    private Player(PlayerName name, int colorIndex)
    {
        Name = name;
        ColorIndex = colorIndex;
    }

    // Id:t syns för alla i rummet. Token är hemlig och visar att det är spelaren själv som anropar.
    public string Id { get; } = Guid.NewGuid().ToString("N");

    public string Token { get; } = SecretToken.Create();

    public PlayerName Name { get; }

    // 1–8, en plats i paletten. Färgen själv finns bara i frontenden.
    public int ColorIndex { get; }

    internal static Player Create(PlayerName name, int colorIndex) => new(name, colorIndex);

    // Jämför i konstant tid av samma skäl som värd-token.
    public bool HasToken(string? token) =>
        token is not null && CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(token), Encoding.UTF8.GetBytes(Token));
}
