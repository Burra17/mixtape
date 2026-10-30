using Mixtape.Domain.Music;

namespace Mixtape.Domain.Games.DropTheNeedle;

// En spelares resultat för en låt. Fastest är den som snabbast enligt servern fick rätt låt, Points är allt spelaren fick för låten och Guess är null om spelaren inte gissade.
public sealed record SongResult(string PlayerId, GuessScore Score, bool Fastest, int Points, SubmittedTrack? Guess);
