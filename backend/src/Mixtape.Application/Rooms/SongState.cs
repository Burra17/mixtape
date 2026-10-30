namespace Mixtape.Application.Rooms;

// Låten som spelas i Drop the Needle, som alla får se. Bara numret och kategorin, aldrig titel, artist eller omslag. Speglas i hubContract.ts.
// Index är låtens nummer som visas. Clip byts vid varje nytt klipp, också när en låt hoppas över, och är det som GetClip svarar med.
public sealed record SongState(int Index, int Count, string CategoryId, int Clip);
