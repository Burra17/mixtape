using Mixtape.Domain.Rooms;

namespace Mixtape.Tests.Domain;

public sealed class PlayerNameTests
{
    [TestCase("  Anna  ", "Anna")]
    [TestCase("Anna   Karin", "Anna Karin")]
    [TestCase("Anna\t\nKarin", "Anna Karin")]
    public void Create_TrimsAndCollapsesWhitespace(string input, string expected)
    {
        Assert.That(PlayerName.Create(input).Value.Value, Is.EqualTo(expected));
    }

    [TestCase("A")]
    [TestCase("Sixteen chars ok")]
    [TestCase("🎸🎸🎸🎸🎸🎸🎸🎸🎸🎸🎸🎸🎸🎸🎸🎸")]
    [TestCase("👨‍👩‍👧‍👦")]
    public void Create_AcceptsOneToSixteenVisibleCharacters(string input)
    {
        Assert.That(PlayerName.Create(input).IsSuccess, Is.True);
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    [TestCase("Seventeen chars!!")]
    [TestCase("🎸🎸🎸🎸🎸🎸🎸🎸🎸🎸🎸🎸🎸🎸🎸🎸🎸")]
    public void Create_RejectsEmptyOrTooLongNames(string? input)
    {
        Assert.That(PlayerName.Create(input).Error, Is.EqualTo(PlayerErrors.InvalidName));
    }

    [Test]
    public void IsSameAs_IgnoresCase()
    {
        var name = PlayerName.Create("Åsa").Value;

        Assert.That(name.IsSameAs(PlayerName.Create("ÅSA").Value), Is.True);
    }
}
