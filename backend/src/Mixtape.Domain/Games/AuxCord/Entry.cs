using Mixtape.Domain.Music;

namespace Mixtape.Domain.Games.AuxCord;

// Ett bidrag i rundan. Vem som skickade in vad stannar på servern till avslöjandet. LinkFetchedAt avgör när klippets länk förnyas, se CurrentClip.
public sealed record Entry(string PlayerId, SubmittedTrack Track, DateTimeOffset LinkFetchedAt);
