namespace Mixtape.Application.Rooms;

// Spelarens egen session och rummets bild som den ser ut efter att spelaren gått med.
public sealed record JoinedRoom(PlayerSession Session, GameState State);
