namespace Mixtape.Application.Connections;

// Vem en anslutning tillhör: en spelare i ett rum, eller rummets storskärm när PlayerId saknas.
public sealed record ConnectionOwner(string RoomCode, string? PlayerId)
{
    public bool IsHost => PlayerId is null;

    public static ConnectionOwner Host(string roomCode) => new(roomCode, null);

    public static ConnectionOwner Player(string roomCode, string playerId) => new(roomCode, playerId);
}
