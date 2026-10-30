using Mixtape.Domain.Games.AuxCord;
using Mixtape.Domain.Rooms;

namespace Mixtape.Tests.Domain;

public sealed class AuxCordSettingsTests
{
    [Test]
    public void Default_IsFiveRoundsWithFifteenSecondClipsAndNinetySecondsToSubmit()
    {
        Assert.That(AuxCordSettings.Default.Rounds, Is.EqualTo(5));
        Assert.That(AuxCordSettings.Default.ClipLength, Is.EqualTo(TimeSpan.FromSeconds(15)));
        Assert.That(AuxCordSettings.Default.SubmittingDuration, Is.EqualTo(TimeSpan.FromSeconds(90)));
    }

    [TestCase(3, 10, 60)]
    [TestCase(5, 15, 90)]
    [TestCase(8, 20, 120)]
    public void Create_WithAllowedValues_Succeeds(int rounds, int clipSeconds, int submittingSeconds)
    {
        var settings = AuxCordSettings.Create(rounds, clipSeconds, submittingSeconds).Value;

        Assert.That(settings.Rounds, Is.EqualTo(rounds));
        Assert.That(settings.ClipSeconds, Is.EqualTo(clipSeconds));
        Assert.That(settings.SubmittingSeconds, Is.EqualTo(submittingSeconds));
    }

    [TestCase(4, 15, 90)]
    [TestCase(0, 15, 90)]
    [TestCase(5, 30, 90)]
    [TestCase(5, -1, 90)]
    [TestCase(5, 15, 45)]
    [TestCase(5, 15, 180)]
    [TestCase(5, 15, 0)]
    public void Create_WithOtherValues_ReturnsInvalidSettings(int rounds, int clipSeconds, int submittingSeconds)
    {
        Assert.That(AuxCordSettings.Create(rounds, clipSeconds, submittingSeconds).Error, Is.EqualTo(RoomErrors.InvalidSettings));
    }
}
