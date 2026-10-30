using Mixtape.Domain.Music;

namespace Mixtape.Domain.Games.DropTheNeedle;

// En spelares låsta gissning. ReceivedAt är när servern tog emot den, före anropet till Deezer, så att snabbast inte beror på Deezers svarstid.
public sealed record Guess(string PlayerId, SubmittedTrack Track, DateTimeOffset ReceivedAt);
