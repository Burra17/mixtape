using Mixtape.Domain.Common;

namespace Mixtape.Domain.Rooms;

public static class RoomErrors
{
    // Samma fel för ett rum som inte finns och för fel värd-token, så att ingen kan pröva sig fram till aktiva rum.
    public static readonly Error NotFound = new("room.not_found", "Rummet finns inte längre.");

    public static readonly Error Full = new("room.full", "Rummet är fullt.");

    public static readonly Error AlreadyStarted = new("room.already_started", "Spelet har redan börjat.");

    public static readonly Error NotEnoughPlayers = new("room.not_enough_players", $"Det behövs minst {GameRoom.MinPlayers} spelare.");

    // Spelaren gör något som inte hör till fasen, till exempel söker under röstningen.
    public static readonly Error WrongPhase = new("room.wrong_phase", "Det går inte just nu.");

    public static readonly Error InvalidSettings = new("room.invalid_settings", "Inställningen finns inte.");
}
