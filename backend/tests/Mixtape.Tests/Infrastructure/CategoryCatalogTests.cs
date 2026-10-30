using Mixtape.Application.Music;
using Mixtape.Domain.Games.DropTheNeedle;
using Mixtape.Infrastructure.Categories;

namespace Mixtape.Tests.Infrastructure;

public sealed class CategoryCatalogTests
{
    [Test]
    public void LoadEmbedded_TheRealFile_HasTheEightCategoriesInTheirOrder()
    {
        var catalog = CategoryCatalog.LoadEmbedded();

        Assert.That(catalog.All.Select(category => category.Id), Is.EqualTo(new[] { "70s", "80s", "90s", "00s", "10s", "swedish", "rock", "today" }));
        Assert.That(catalog.Find("80s")!.Playlists, Is.EqualTo(new long[] { 1913763402, 8873745702 }));
        Assert.That(catalog.Find(DropTheNeedleSettings.Default.CategoryId), Is.Not.Null);
    }

    [Test]
    public void Find_AnUnknownId_ReturnsNull()
    {
        Assert.That(CategoryCatalog.LoadEmbedded().Find("disco"), Is.Null);
    }

    [Test]
    public void Create_WithTheSameIdTwice_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => new CategoryCatalog([new Category("00s", "00-tal", [1]), new Category("00s", "Igen", [2])]));
    }

    [Test]
    public void Create_WithoutPlaylists_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => new CategoryCatalog([new Category("00s", "00-tal", [])]));
    }

    [Test]
    public void Create_WithoutTheDefaultCategory_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => new CategoryCatalog([new Category("rock", "Rock", [1])]));
    }
}
