using System.Globalization;
using Mixtape.Domain.Common;
using Mixtape.Domain.Music;

namespace Mixtape.Domain.Games.DropTheNeedle;

// Lottar låtarna till ett spel ur kategorins låtar.
public static class SongDraw
{
    // history är låtarnas id som rummet spelat, med den som spelades längst tillbaka först.
    // Låtar som aldrig spelats kommer först i slumpad ordning, och när de inte räcker de som spelades längst tillbaka.
    public static Result<IReadOnlyList<SubmittedTrack>> Draw(IReadOnlyList<SubmittedTrack> pool, int count, IReadOnlyList<string> history, Random random)
    {
        var songs = Group(pool);
        if (songs.Count < count)
        {
            return DropTheNeedleErrors.TooFewSongs;
        }

        var playedAt = history.Select((id, index) => (id, index)).ToDictionary(played => played.id, played => played.index);

        // En låt som finns i flera spellistor kan ha spelats under vilket av sina id som helst, och den senaste gången räknas.
        int? LastPlayed(List<SubmittedTrack> copies) =>
            copies.Select(copy => playedAt.TryGetValue(Key(copy), out var index) ? index : (int?)null).Max();

        var unplayed = songs.Where(copies => LastPlayed(copies) is null).ToArray();
        random.Shuffle(unplayed);
        var oldestFirst = songs.Where(copies => LastPlayed(copies) is not null).OrderBy(LastPlayed);
        return unplayed.Concat(oldestFirst).Take(count).Select(copies => copies[0]).ToList();
    }

    // Låtens nyckel i rummets historik.
    public static string Key(SubmittedTrack song) => song.Id.ToString(CultureInfo.InvariantCulture);

    // Samma låt kan finnas i flera av kategorins spellistor, ibland med olika id. Varje låt blir en grupp med alla sina kopior.
    private static List<List<SubmittedTrack>> Group(IReadOnlyList<SubmittedTrack> pool)
    {
        var songs = new List<List<SubmittedTrack>>();
        foreach (var track in pool)
        {
            if (songs.FirstOrDefault(copies => copies[0].IsSameSongAs(track)) is { } copies)
            {
                copies.Add(track);
            }
            else
            {
                songs.Add([track]);
            }
        }

        return songs;
    }
}
