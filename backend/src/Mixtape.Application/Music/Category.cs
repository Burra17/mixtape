namespace Mixtape.Application.Music;

// En kategori i Drop the Needle. Id är stabilt och sparas i inställningarna, Name är det som visas och Playlists är Deezers spellistor.
public sealed record Category(string Id, string Name, IReadOnlyList<long> Playlists);
