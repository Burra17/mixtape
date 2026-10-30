namespace Mixtape.Domain.Games.DropTheNeedle;

// En spelares resultat för en låt. Fastest är den som snabbast enligt servern fick rätt låt, och Points är allt spelaren fick för låten.
public sealed record SongResult(string PlayerId, GuessScore Score, bool Fastest, int Points);
