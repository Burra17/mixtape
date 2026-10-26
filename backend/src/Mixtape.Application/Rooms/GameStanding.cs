namespace Mixtape.Application.Rooms;

// En spelares plats i ställningen, som alla får se. Score gäller hela spelet och RoundPoints rundan. Speglas i hubContract.ts.
public sealed record GameStanding(string PlayerId, int Score, int RoundPoints, int Place);
