using Mixtape.Domain.Games.DropTheNeedle;
using Mixtape.Domain.Music;

namespace Mixtape.Tests.Domain;

// Titlarna och artisterna är riktiga exempel från Deezer.
public sealed class SongMatcherTests
{
    [TestCase("Dancing Queen", "Dancing Queen (Live)")]
    [TestCase("Dancing Queen", "Dancing Queen - Remastered 2001")]
    [TestCase("Don't Look Back In Anger", "Don't Look Back In Anger (Remastered)")]
    [TestCase("Get Lucky (feat. Pharrell Williams and Nile Rodgers)", "Get Lucky (Radio Edit - feat. Pharrell Williams and Nile Rodgers)")]
    [TestCase("Crazy In Love (feat. JAŸ-Z)", "Crazy in Love")]
    [TestCase("Crazy In Love feat. JAY-Z", "Crazy In Love")]
    [TestCase("Lose Yourself ft. Someone", "Lose Yourself")]
    [TestCase("Song featuring Someone", "Song")]
    [TestCase("Heroes [2017 Remaster]", "\"Heroes\"")]
    [TestCase("Don't Look Back In Anger", "Dont Look Back in Anger")]
    [TestCase("Déjà Vu", "Deja Vu")]
    [TestCase("Hard Knock Life", "Hard-Knock Life")]
    public void Score_TheSameTitleAndArtistInAnotherVersion_IsTheSong(string guessTitle, string correctTitle)
    {
        var score = SongMatcher.Score(Track(guessTitle, "ABBA"), Track(correctTitle, "ABBA"));

        Assert.That(score, Is.EqualTo(GuessScore.Song));
    }

    [TestCase("Beyonce", "Beyoncé")]
    [TestCase("ACDC", "AC/DC")]
    [TestCase("The Beatles", "Beatles")]
    [TestCase("beatles", "The Beatles")]
    [TestCase("Simon and Garfunkel", "Simon & Garfunkel")]
    [TestCase("  Daft   Punk ", "Daft Punk")]
    [TestCase("Jay Z", "JAY-Z")]
    public void Score_TheSameArtistWrittenDifferently_IsTheSong(string guessArtist, string correctArtist)
    {
        var score = SongMatcher.Score(Track("Yellow", guessArtist), Track("Yellow", correctArtist));

        Assert.That(score, Is.EqualTo(GuessScore.Song));
    }

    [Test]
    public void Score_UnderPressureWithBowieAsMainArtist_MatchesQueensVersionThroughTheContributors()
    {
        var guess = Track("Under Pressure", "David Bowie", ["David Bowie", "Queen"]);
        var correct = Track("Under Pressure (feat. David Bowie)", "Queen", ["Queen", "David Bowie"]);

        Assert.That(SongMatcher.Score(guess, correct), Is.EqualTo(GuessScore.Song));
    }

    [Test]
    public void Score_AnotherSongByAContributor_IsTheArtist()
    {
        var guess = Track("Heroes", "David Bowie", ["David Bowie"]);
        var correct = Track("Under Pressure (feat. David Bowie)", "Queen", ["Queen", "David Bowie"]);

        Assert.That(SongMatcher.Score(guess, correct), Is.EqualTo(GuessScore.Artist));
    }

    [Test]
    public void Score_WithoutContributors_UsesTheMainArtist()
    {
        var guess = Track("Fernando", "ABBA");
        var correct = Track("Dancing Queen", "ABBA", []);

        Assert.That(SongMatcher.Score(guess, correct), Is.EqualTo(GuessScore.Artist));
    }

    [Test]
    public void Score_ACoverByAnotherArtist_IsWrong()
    {
        var guess = Track("Get Lucky - Tribute to Daft Punk", "Get Lucky");
        var correct = Track("Get Lucky (feat. Pharrell Williams and Nile Rodgers)", "Daft Punk");

        Assert.That(SongMatcher.Score(guess, correct), Is.EqualTo(GuessScore.None));
    }

    [Test]
    public void Score_TheRightArtistButAnotherSong_IsTheArtist()
    {
        Assert.That(SongMatcher.Score(Track("Fernando", "ABBA"), Track("Dancing Queen (Live)", "ABBA")), Is.EqualTo(GuessScore.Artist));
    }

    [Test]
    public void Score_EverythingWrong_IsNone()
    {
        Assert.That(SongMatcher.Score(Track("Yellow", "Coldplay"), Track("In the End", "Linkin Park")), Is.EqualTo(GuessScore.None));
    }

    [Test]
    public void Score_TheSameTitleByAnotherArtist_IsNone()
    {
        Assert.That(SongMatcher.Score(Track("Hallelujah", "Jeff Buckley"), Track("Hallelujah", "Leonard Cohen")), Is.EqualTo(GuessScore.None));
    }

    [Test]
    public void Score_ATitleThatIsOnlyAVersion_DoesNotMatchEverything()
    {
        Assert.That(SongMatcher.Score(Track("(Intro)", "ABBA"), Track("(Outro)", "ABBA")), Is.EqualTo(GuessScore.Artist));
    }

    private static SubmittedTrack Track(string title, string artist, IReadOnlyList<string>? contributors = null) =>
        new(1, title, artist, "Album", "omslag", "klipp", contributors);
}
