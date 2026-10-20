namespace Mixtape.Application.Rooms;

// Det nya rummet, och koden till värdens gamla rum om "Nytt spel" tog bort det, så att dess mobiler kan få veta att det stängts.
public sealed record CreatedRoom(HostSession Session, LobbyState Lobby, string? ClosedRoomCode);
