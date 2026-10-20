using Mixtape.Domain.Common;

namespace Mixtape.Domain.Rooms;

public static class PlayerErrors
{
    public static readonly Error InvalidName = new("player.invalid_name", $"Namnet ska vara 1–{PlayerName.MaxLength} tecken.");

    public static readonly Error NameTaken = new("player.name_taken", "Namnet är upptaget.");

    // Samma fel för en spelare som inte finns och för fel spelar-token.
    public static readonly Error NotFound = new("player.not_found", "Spelaren finns inte i rummet.");
}
