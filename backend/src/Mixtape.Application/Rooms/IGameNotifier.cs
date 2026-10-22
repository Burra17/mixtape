namespace Mixtape.Application.Rooms;

// Når rummets klienter när servern själv ändrar rummet, utan att någon anropat hubben. Implementeras i Api med hubbens kontext.
public interface IGameNotifier
{
    Task GameStateUpdatedAsync(GameState state);
}
