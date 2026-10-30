using Mixtape.Domain.Games.DropTheNeedle;
using Mixtape.Domain.Music;

namespace Mixtape.Tests.Domain;

public sealed class SongDrawTests
{
    private static readonly IReadOnlyList<SubmittedTrack> FiveSongs =
        Enumerable.Range(1, 5).Select(id => TestTrack.Of(id, $"Låt {id}", $"Artist {id}")).ToList();

    [Test]
    public void Draw_WithNoHistory_GivesTheCountWithoutDuplicates()
    {
        var drawn = SongDraw.Draw(FiveSongs, 3, [], new Random(1)).Value;

        Assert.That(drawn, Has.Count.EqualTo(3));
        Assert.That(drawn.Select(song => song.Id), Is.Unique);
    }

    [Test]
    public void Draw_TakesUnplayedSongsBeforePlayedOnes()
    {
        var drawn = SongDraw.Draw(FiveSongs, 2, ["1", "2", "3"], new Random(1)).Value;

        Assert.That(drawn.Select(song => song.Id), Is.EquivalentTo(new long[] { 4, 5 }));
    }

    [Test]
    public void Draw_WhenUnplayedRunOut_TakesTheSongsPlayedLongestAgoFirst()
    {
        var drawn = SongDraw.Draw(FiveSongs, 4, ["3", "1", "2", "4"], new Random(1)).Value;

        Assert.That(drawn.Select(song => song.Id), Is.EqualTo(new long[] { 5, 3, 1, 2 }));
    }

    [Test]
    public void Draw_TheSameSongFromTwoPlaylists_CountsOnce()
    {
        IReadOnlyList<SubmittedTrack> pool =
        [
            TestTrack.Of(1, "Back In Black", "AC/DC"),
            TestTrack.Of(1, "Back In Black", "AC/DC"),
            TestTrack.Of(2, "Back In Black", "AC/DC"),
            TestTrack.Of(3, "Take on Me", "a-ha"),
        ];

        var drawn = SongDraw.Draw(pool, 2, [], new Random(1)).Value;

        Assert.That(drawn.Select(song => song.Title), Is.EquivalentTo(new[] { "Back In Black", "Take on Me" }));
    }

    [Test]
    public void Draw_ASongPlayedUnderAnotherId_CountsAsPlayed()
    {
        IReadOnlyList<SubmittedTrack> pool =
        [
            TestTrack.Of(1, "Back In Black", "AC/DC"),
            TestTrack.Of(2, "Back In Black", "AC/DC"),
            TestTrack.Of(3, "Take on Me", "a-ha"),
        ];

        var drawn = SongDraw.Draw(pool, 1, ["2"], new Random(1)).Value;

        Assert.That(drawn.Single().Title, Is.EqualTo("Take on Me"));
    }

    [Test]
    public void Draw_WithFewerSongsThanTheCount_ReturnsTooFewSongs()
    {
        Assert.That(SongDraw.Draw(FiveSongs, 6, [], new Random(1)).Error, Is.EqualTo(DropTheNeedleErrors.TooFewSongs));
    }

    [Test]
    public void Key_IsTheTrackId()
    {
        Assert.That(SongDraw.Key(TestTrack.Of(248297032, "Yellow")), Is.EqualTo("248297032"));
    }
}
