using Mixtape.Application.Connections;
using Mixtape.Application.Music;
using Mixtape.Application.Rooms;
using Mixtape.Infrastructure.Categories;

namespace Mixtape.Tests.Application;

// Ett startat spel med Anna, Bertil och Cecilia, vars anslutningar heter som de själva.
public static class GameStarter
{
    public static async Task<StartedGame> StartAsync(IGameRoomRepository rooms, IConnectionRegistry connections, TimeProvider time)
    {
        var host = (await new CreateRoomHandler(rooms, connections, time).HandleAsync(null, "host")).Value.Session;
        var join = new JoinRoomHandler(rooms, connections, time);
        var players = new List<PlayerSession>();
        foreach (var name in new[] { "Anna", "Bertil", "Cecilia" })
        {
            players.Add((await join.HandleAsync(host.RoomCode, name, name)).Value.Session);
        }

        await new StartGameHandler(rooms, new NumberedPrompts(), NoSongs, time).HandleAsync(host);
        return new StartedGame(host, players);
    }

    // Aux Cord Battles hämtar inga låtar, så kategorierna används aldrig här.
    private static readonly CategorySongs NoSongs = new(new CategoryCatalog([new Category("00s", "00-tal", [1])]), new NoPlaylists());

    // Testerna handlar inte om promptarna, så de behöver inte komma från den riktiga banken.
    private sealed class NumberedPrompts : IPromptProvider
    {
        public IReadOnlyList<string> Draw(int count, IReadOnlyCollection<string> used) =>
            Enumerable.Range(1, count).Select(round => $"Prompt {round}").ToList();
    }

    private sealed class NoPlaylists : MusicCatalogStub;
}
