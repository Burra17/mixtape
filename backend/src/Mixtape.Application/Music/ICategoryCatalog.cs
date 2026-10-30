namespace Mixtape.Application.Music;

public interface ICategoryCatalog
{
    // I den ordning de visas i lobbyn.
    IReadOnlyList<Category> All { get; }

    Category? Find(string id);
}
