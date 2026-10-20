namespace Mixtape.Application.Rooms;

// Spelarens egen session och lobbyn som den ser ut efter att spelaren gått med.
public sealed record JoinedRoom(PlayerSession Session, LobbyState Lobby);
