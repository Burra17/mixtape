namespace Mixtape.Application.Rooms;

// Länken till klippet som spelas, bara till storskärmen. Index visar vilket klipp svaret gäller. Speglas i hubContract.ts.
public sealed record Clip(int Index, string PreviewUrl);
