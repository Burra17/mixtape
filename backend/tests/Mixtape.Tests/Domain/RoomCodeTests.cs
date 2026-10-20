using Mixtape.Domain.Rooms;

namespace Mixtape.Tests.Domain;

public sealed class RoomCodeTests
{
    [Test]
    public void Generate_UsesOnlyFourLettersFromTheAlphabet()
    {
        var random = new Random(42);

        for (var i = 0; i < 1000; i++)
        {
            var code = RoomCode.Generate(random).Value;

            Assert.That(code, Has.Length.EqualTo(4));
            Assert.That(code.All(RoomCode.Alphabet.Contains), Is.True, code);
        }
    }

    [Test]
    public void Alphabet_HasNoVowelsOrLookalikes()
    {
        Assert.That(RoomCode.Alphabet.Intersect("AEIOUYÅÄÖQ0123456789"), Is.Empty);
    }

    [TestCase("kxpb", "KXPB")]
    [TestCase(" KXPB ", "KXPB")]
    public void Parse_AcceptsLowercaseAndSpaces(string input, string expected)
    {
        Assert.That(RoomCode.Parse(input)?.Value, Is.EqualTo(expected));
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("KXP")]
    [TestCase("KXPBB")]
    [TestCase("KAPB")]
    [TestCase("KXP1")]
    public void Parse_RejectsInvalidCodes(string? input)
    {
        Assert.That(RoomCode.Parse(input), Is.Null);
    }
}
