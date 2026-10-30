using System.Text.Json;
using Mixtape.Application.Music;
using Mixtape.Domain.Games.DropTheNeedle;

namespace Mixtape.Infrastructure.Categories;

// Kategorierna i categories.json. Filen är inbäddad i assemblyn, som promptbanken, så att den inte kan saknas när appen körs.
public sealed class CategoryCatalog : ICategoryCatalog
{
    private const string ResourceName = "categories.json";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    // Kastar om filen är trasig.
    public CategoryCatalog(IReadOnlyList<Category> categories)
    {
        Validate(categories);
        All = categories;
    }

    public IReadOnlyList<Category> All { get; }

    public static CategoryCatalog LoadEmbedded()
    {
        using var stream = typeof(CategoryCatalog).Assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException($"{ResourceName} saknas i Mixtape.Infrastructure.");
        var categories = JsonSerializer.Deserialize<List<Category>>(stream, JsonOptions) ?? throw new InvalidOperationException($"{ResourceName} är tom.");
        return new CategoryCatalog(categories);
    }

    public Category? Find(string id) => All.FirstOrDefault(category => category.Id == id);

    private static void Validate(IReadOnlyList<Category> categories)
    {
        if (categories.Any(category => string.IsNullOrWhiteSpace(category.Id) || string.IsNullOrWhiteSpace(category.Name) || category.Playlists is not { Count: > 0 }))
        {
            throw new InvalidOperationException("En kategori saknar id, namn eller spellista.");
        }

        if (categories.Select(category => category.Id).Distinct().Count() != categories.Count)
        {
            throw new InvalidOperationException("Två kategorier har samma id.");
        }

        // Standardvalet ligger i domänen, så filen måste ha den kategorin.
        if (categories.All(category => category.Id != DropTheNeedleSettings.Default.CategoryId))
        {
            throw new InvalidOperationException($"Standardkategorin {DropTheNeedleSettings.Default.CategoryId} saknas.");
        }
    }
}
