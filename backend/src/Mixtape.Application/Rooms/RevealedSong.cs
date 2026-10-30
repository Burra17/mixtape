using Mixtape.Domain.Music;

namespace Mixtape.Application.Rooms;

// En låt i Drop the Needles avslöjande, det rätta svaret eller en spelares gissning. Ingen länk. Speglas i hubContract.ts.
public sealed record RevealedSong(string Title, string Artist, string Album, string CoverUrl)
{
    public static RevealedSong From(SubmittedTrack track) => new(track.Title, track.Artist, track.Album, track.CoverUrl);
}
