namespace Mixtape.Domain.Games;

// Det värden ställt in för ett spelläge. Varje läge har sin egen typ med sina val.
public interface IGameSettings
{
    GameMode Mode { get; }

    int MinPlayers { get; }
}
