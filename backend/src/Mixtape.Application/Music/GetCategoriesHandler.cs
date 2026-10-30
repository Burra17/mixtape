namespace Mixtape.Application.Music;

// Kategorierna till lobbyn. De är samma för alla rum och ändras inte medan servern kör, så de skickas inte med i varje bild.
public sealed class GetCategoriesHandler(ICategoryCatalog categories)
{
    public IReadOnlyList<CategoryOption> Handle() => categories.All.Select(category => new CategoryOption(category.Id, category.Name)).ToList();
}
