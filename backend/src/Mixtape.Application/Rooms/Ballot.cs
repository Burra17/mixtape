namespace Mixtape.Application.Rooms;

// Bara till spelaren själv: numret på det egna bidraget och på bidraget spelaren röstat på, eller null. Står aldrig i GameState, som alla får. Speglas i hubContract.ts.
public sealed record Ballot(int? OwnEntry, int? VotedEntry);
