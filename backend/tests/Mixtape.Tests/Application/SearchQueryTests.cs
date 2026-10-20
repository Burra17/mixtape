using Mixtape.Application.Music;

namespace Mixtape.Tests.Application;

public sealed class SearchQueryTests
{
    [TestCase("Veronica Maggio", "veronica maggio")]
    [TestCase("  HÅKAN   Hellström ", "håkan hellström")]
    [TestCase("Robyn\tDancing", "robyn dancing")]
    public void Create_TrimsLowercasesAndCollapsesWhitespace(string input, string expected)
    {
        Assert.That(SearchQuery.Create(input).Value.Value, Is.EqualTo(expected));
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void Create_RejectsEmptyQueries(string? input)
    {
        Assert.That(SearchQuery.Create(input).Error, Is.EqualTo(MusicErrors.InvalidQuery));
    }

    [Test]
    public void Create_AcceptsHundredCharactersButNotMore()
    {
        Assert.That(SearchQuery.Create(new string('a', SearchQuery.MaxLength)).IsSuccess, Is.True);
        Assert.That(SearchQuery.Create(new string('a', SearchQuery.MaxLength + 1)).Error, Is.EqualTo(MusicErrors.InvalidQuery));
    }
}
