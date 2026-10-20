namespace Mixtape.Application.Rooms;

// Det mobilen behöver för att känna igen sig själv i lobbyn och, från #11, komma tillbaka till rummet. Speglas i hubContract.ts.
public sealed record PlayerSession(string RoomCode, string PlayerId, string PlayerToken);
