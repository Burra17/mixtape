namespace Mixtape.Application.Rooms;

// Spelaren som lämnade eller togs bort, och rummets bild utan den.
public sealed record PlayerRemoved(string PlayerId, GameState State);
