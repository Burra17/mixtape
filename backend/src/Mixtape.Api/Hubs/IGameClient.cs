namespace Mixtape.Api.Hubs;

// Allt servern kan skicka till en klient. Speglas i frontend/src/services/hub/hubContract.ts.
public interface IGameClient
{
    Task Pong();
}
