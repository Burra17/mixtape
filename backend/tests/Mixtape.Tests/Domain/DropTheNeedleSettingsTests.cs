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
    }

    [TestCase(5, 20)]
    [TestCase(10, 25)]
    [TestCase(20, 30)]
    public void Create_WithAllowedValues_Succeeds(int songs, int clipSeconds)
    {
        var settings = DropTheNeedleSettings.Create(songs, clipSeconds).Value;

        Assert.That(settings.Songs, Is.EqualTo(songs));
        Assert.That(settings.ClipSeconds, Is.EqualTo(clipSeconds));
    }

    [TestCase(4, 30)]
    [TestCase(25, 30)]
    [TestCase(10, 15)]
    [TestCase(10, 35)]
    public void Create_WithOtherValues_ReturnsInvalidSettings(int songs, int clipSeconds)
    {
        Assert.That(DropTheNeedleSettings.Create(songs, clipSeconds).Error, Is.EqualTo(RoomErrors.InvalidSettings));
    }

    [Test]
    public void With_OneAtATime_KeepsTheOther()
    {
        var settings = DropTheNeedleSettings.Default.With(5, null).Value.With(null, 20).Value;

        Assert.That(settings, Is.EqualTo(DropTheNeedleSettings.Create(5, 20).Value));
    }
}
