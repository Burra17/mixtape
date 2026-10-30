using Mixtape.Domain.Games.DropTheNeedle;
using Mixtape.Domain.Rooms;

namespace Mixtape.Tests.Domain;

public sealed class DropTheNeedleSettingsTests
{
    [Test]
    public void Default_IsTenSongsWithThirtySecondClipsForTwoPlayers()
    {
        Assert.That(DropTheNeedleSettings.Default.Songs, Is.EqualTo(10));
        Assert.That(DropTheNeedleSettings.Default.ClipSeconds, Is.EqualTo(30));
        Assert.That(DropTheNeedleSettings.Default.MinPlayers, Is.EqualTo(2));
        Assert.That(DropTheNeedleSettings.Default.CategoryId, Is.EqualTo("00s"));
    }

    [TestCase(5, 20)]
    [TestCase(10, 25)]
    [TestCase(20, 30)]
    public void Create_WithAllowedValues_Succeeds(int songs, int clipSeconds)
    {
        var settings = DropTheNeedleSettings.Create(songs, clipSeconds, "80s").Value;

        Assert.That(settings.Songs, Is.EqualTo(songs));
        Assert.That(settings.ClipSeconds, Is.EqualTo(clipSeconds));
    }

    [TestCase(4, 30)]
    [TestCase(25, 30)]
    [TestCase(10, 15)]
    [TestCase(10, 35)]
    public void Create_WithOtherValues_ReturnsInvalidSettings(int songs, int clipSeconds)
    {
        Assert.That(DropTheNeedleSettings.Create(songs, clipSeconds, "80s").Error, Is.EqualTo(RoomErrors.InvalidSettings));
    }

    [TestCase("")]
    [TestCase(" ")]
    public void Create_WithoutCategory_ReturnsInvalidSettings(string categoryId)
    {
        Assert.That(DropTheNeedleSettings.Create(10, 30, categoryId).Error, Is.EqualTo(RoomErrors.InvalidSettings));
    }

    [Test]
    public void With_OneAtATime_KeepsTheOthers()
    {
        var settings = DropTheNeedleSettings.Default.With(5, null, null).Value.With(null, 20, null).Value.With(null, null, "rock").Value;

        Assert.That(settings, Is.EqualTo(DropTheNeedleSettings.Create(5, 20, "rock").Value));
    }
}
