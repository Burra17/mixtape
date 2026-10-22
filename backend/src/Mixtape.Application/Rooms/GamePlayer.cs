namespace Mixtape.Application.Rooms;

// Det alla i rummet får veta om en spelare. Token skickas aldrig. Speglas i hubContract.ts.
public sealed record GamePlayer(string Id, string Name, int ColorIndex, bool IsConnected);
