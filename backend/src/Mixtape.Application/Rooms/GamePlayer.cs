namespace Mixtape.Application.Rooms;

// Det alla i rummet får veta om en spelare. Token skickas aldrig, och bara att spelaren skickat in, inte vilken låt. Speglas i hubContract.ts.
public sealed record GamePlayer(string Id, string Name, int ColorIndex, bool IsConnected, bool HasSubmitted);
