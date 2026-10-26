namespace Mixtape.Domain.Rooms;

// En spelares plats i ställningen. Place räknas som i sport, så två på delad första plats följs av en trea.
public sealed record Standing(Player Player, int Place);
