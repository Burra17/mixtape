namespace Mixtape.Domain.Games.DropTheNeedle;

// En spelares resultat för en låt. Points är 2 för rätt låt, 1 för rätt artist och 1 extra till den som snabbast fick rätt låt.
public sealed record SongResult(string PlayerId, GuessScore Score, bool Fastest, int Points);
