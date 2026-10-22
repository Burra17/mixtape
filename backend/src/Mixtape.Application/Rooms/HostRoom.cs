namespace Mixtape.Application.Rooms;

// Värdens session och rummets bild, så att storskärmen får spelarlistan direkt när den skapat eller återupptagit rummet.
public sealed record HostRoom(HostSession Session, GameState State);
