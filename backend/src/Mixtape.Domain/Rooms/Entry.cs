namespace Mixtape.Domain.Rooms;

// Ett bidrag i rundan. Vem som skickade in vad stannar på servern till avslöjandet.
public sealed record Entry(string PlayerId, SubmittedTrack Track);
