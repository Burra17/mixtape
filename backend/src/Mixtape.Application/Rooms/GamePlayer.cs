namespace Mixtape.Application.Rooms;

// Det alla i rummet får veta om en spelare. Token skickas aldrig, och bara att spelaren skickat in, röstat eller gissat, inte vad. Speglas i hubContract.ts.
public sealed record GamePlayer(string Id, string Name, int ColorIndex, bool IsConnected, bool HasSubmitted, bool HasVoted, bool HasGuessed);
