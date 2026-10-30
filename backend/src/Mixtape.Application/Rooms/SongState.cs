namespace Mixtape.Application.Rooms;

// Låten som spelas i Drop the Needle, som alla får se. Bara numret och kategorin, aldrig titel, artist eller omslag. Speglas i hubContract.ts.
public sealed record SongState(int Index, int Count, string CategoryId);
