using Mixtape.Domain.Music;

namespace Mixtape.Tests;

// En inskickad låt där bara id, titel och artist spelar roll för testet. Omslag och länk följer id:t.
public static class TestTrack
{
    public static SubmittedTrack Of(long id, string title, string artist = "Artist") =>
        new(id, title, artist, "Album", $"https://omslag/{id}", $"https://klipp/{id}");
}
