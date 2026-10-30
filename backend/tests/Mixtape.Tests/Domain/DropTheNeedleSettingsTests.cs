using Mixtape.Domain.Games.DropTheNeedle;
using Mixtape.Domain.Rooms;

namespace Mixtape.Tests.Domain;

public sealed class DropTheNeedleSettingsTests
{
    [Test]
    public void Default_IsTenSongsWithThirtySecondClipsAndThirtySecondsToThinkForTwoPlayers()
    {
        Assert.That(DropTheNeedleSettings.Default.Songs, Is.EqualTo(10));
        Assert.That(DropTheNeedleSettings.Default.ClipSeconds, Is.EqualTo(30));
        Assert.That(DropTheNeedleSettings.Default.ThinkingSeconds, Is.EqualTo(30));
        Assert.That(DropTheNeedleSettings.Default.MinPlayers, Is.EqualTo(2));
        Assert.That(DropTheNeedleSettings.Default.CategoryId, Is.EqualTo("00s"));
    }

    [TestCase(5, 20, 0)]
    [TestCase(10, 25, 15)]
    [TestCase(20, 30, 30)]
    public void Create_WithAllowedValues_Succeeds(int songs, int clipSeconds, int thinkingSeconds)
    {
        var settings = DropTheNeedleSettings.Create(songs, clipSeconds, thinkingSeconds, "80s").Value;

        Assert.That(settings.Songs, Is.EqualTo(songs));
        Assert.That(settings.ClipSeconds, Is.EqualTo(clipSeconds));
        Assert.That(settings.ThinkingSeconds, Is.EqualTo(thinkingSeconds));
    }

    [TestCase(4, 30, 30)]
    [TestCase(25, 30, 30)]
    [TestCase(10, 15, 30)]
    [TestCase(10, 35, 30)]
    [TestCase(10, 30, 10)]
    [TestCase(10, 30, 45)]
    public void Create_WithOtherValues_ReturnsInvalidSettings(int songs, int clipSeconds, int thinkingSeconds)
    {
        Assert.That(DropTheNeedleSettings.Create(songs, clipSeconds, thinkingSeconds, "80s").Error, Is.EqualTo(RoomErrors.InvalidSettings));
    }

    [TestCase(20, 0, 20)]
    [TestCase(30, 30, 60)]
    public void GuessLength_IsTheClipAndThenTheTimeToThink(int clipSeconds, int thinkingSeconds, int guessSeconds)
    {
        var settings = DropTheNeedleSettings.Create(10, clipSeconds, thinkingSeconds, "80s").Value;

        Assert.That(settings.GuessLength, Is.EqualTo(TimeSpan.FromSeconds(guessSeconds)));
    }

    [TestCase("")]
    [TestCase(" ")]
    public void Create_WithoutCategory_ReturnsInvalidSettings(string categoryId)
    {
        Assert.That(DropTheNeedleSettings.Create(10, 30, 30, categoryId).Error, Is.EqualTo(RoomErrors.InvalidSettings));
    }

    [Test]
    public void With_OneAtATime_KeepsTheOthers()
    {
        var settings = DropTheNeedleSettings.Default.With(5, null, null, null).Value.With(null, 20, null, null).Value.With(null, null, 15, null).Value.With(null, null, null, "rock").Value;

        Assert.That(settings, Is.EqualTo(DropTheNeedleSettings.Create(5, 20, 15, "rock").Value));
    }
}
