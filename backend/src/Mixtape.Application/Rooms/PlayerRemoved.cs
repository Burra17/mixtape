namespace Mixtape.Application.Rooms;

// Spelaren som lämnade eller togs bort, och lobbyn utan den.
public sealed record PlayerRemoved(string PlayerId, LobbyState Lobby);
