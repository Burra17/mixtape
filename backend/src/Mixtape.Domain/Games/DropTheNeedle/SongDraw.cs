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
        var songs = Distinct(pool);
        if (songs.Count < count)
        {
            return DropTheNeedleErrors.TooFewSongs;
        }

        var playedAt = history.Select((id, index) => (id, index)).ToDictionary(played => played.id, played => played.index);
        var unplayed = songs.Where(song => !playedAt.ContainsKey(Key(song))).ToArray();
        random.Shuffle(unplayed);
        var oldestFirst = songs.Where(song => playedAt.ContainsKey(Key(song))).OrderBy(song => playedAt[Key(song)]);
        return unplayed.Concat(oldestFirst).Take(count).ToList();
    }

    // Låtens nyckel i rummets historik.
    public static string Key(SubmittedTrack song) => song.Id.ToString(CultureInfo.InvariantCulture);

    // Samma låt kan finnas i flera av kategorins spellistor, ibland med olika id.
    private static List<SubmittedTrack> Distinct(IReadOnlyList<SubmittedTrack> pool)
    {
        var songs = new List<SubmittedTrack>();
        foreach (var song in pool)
        {
            if (!songs.Any(song.IsSameSongAs))
            {
                songs.Add(song);
            }
        }

        return songs;
    }
}
