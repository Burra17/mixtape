using Mixtape.Domain.Rooms;

namespace Mixtape.Application.Rooms;

// Det storskärmen behöver för att visa sitt rum och komma tillbaka till det. Speglas i hubContract.ts.
public sealed record HostSession(string RoomCode, string HostToken)
{
    public static HostSession For(GameRoom room) => new(room.Code.Value, room.HostToken);
}
