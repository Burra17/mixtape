namespace Mixtape.Application.Rooms;

// Det storskärmen behöver för att visa sitt rum och komma tillbaka till det. Speglas i hubContract.ts.
public sealed record HostSession(string RoomCode, string HostToken);
