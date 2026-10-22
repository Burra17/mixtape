using Mixtape.Application.Rooms;

namespace Mixtape.Api.Hubs;

// Allt servern kan skicka till en klient. Speglas i frontend/src/services/hub/hubContract.ts.
public interface IGameClient
{
    Task Pong();

    // Hela rummet vid varje ändring, till alla i rummet.
    Task GameStateUpdated(GameState state);

    // Bara till spelaren som värden tog bort.
    Task RemovedFromRoom();

    // Till alla mobiler i ett rum som inte finns längre, till exempel efter "Nytt spel".
    Task RoomClosed();
}
