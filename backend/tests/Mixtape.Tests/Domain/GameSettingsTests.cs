using Mixtape.Domain.Rooms;

namespace Mixtape.Tests.Domain;

public sealed class GameSettingsTests
{
    [Test]
    public void Default_IsFiveRoundsWithFifteenSecondClips()
    {
        Assert.That(GameSettings.Default.Rounds, Is.EqualTo(5));
        Assert.That(GameSettings.Default.ClipLength, Is.EqualTo(TimeSpan.FromSeconds(15)));
    }

    [TestCase(3, 10)]
    [TestCase(5, 15)]
    [TestCase(8, 20)]
    public void Create_WithAllowedValues_Succeeds(int rounds, int clipSeconds)
    {
        var settings = GameSettings.Create(rounds, clipSeconds).Value;

        Assert.That(settings.Rounds, Is.EqualTo(rounds));
        Assert.That(settings.ClipSeconds, Is.EqualTo(clipSeconds));
    }

    [TestCase(4, 15)]
    [TestCase(0, 15)]
    [TestCase(5, 30)]
    [TestCase(5, -1)]
    public void Create_WithOtherValues_ReturnsInvalidSettings(int rounds, int clipSeconds)
    {
        Assert.That(GameSettings.Create(rounds, clipSeconds).Error, Is.EqualTo(RoomErrors.InvalidSettings));
    }
}
