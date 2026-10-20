using Mixtape.Domain.Common;

namespace Mixtape.Domain.Rooms;

public static class RoomErrors
{
    // Samma fel för ett rum som inte finns och för fel värd-token, så att ingen kan pröva sig fram till aktiva rum.
    public static readonly Error NotFound = new("room.not_found", "Rummet finns inte längre.");
}
