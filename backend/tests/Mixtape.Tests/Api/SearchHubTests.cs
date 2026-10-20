using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Mixtape.Api.Hubs;
using Mixtape.Application.Music;
using Mixtape.Application.Rooms;
using Mixtape.Domain.Common;

namespace Mixtape.Tests.Api;

public sealed class SearchHubTests
{
    private WebApplicationFactory<Program> _factory = null!;

    // Deezer byts mot en fast lista, så att testet aldrig går mot nätet.
    [OneTimeSetUp]
    public void StartApi()
    {
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services => services.AddSingleton<IMusicCatalog, FixedCatalog>()));
    }

    [OneTimeTearDown]
    public void StopApi()
    {
        _factory.Dispose();
    }

    [Test]
    public async Task Search_AsPlayer_ReturnsTracks()
    {
        await using var host = CreateConnection();
        await using var anna = CreateConnection();
        await Task.WhenAll(host.StartAsync(), anna.StartAsync());
        var room = (await host.InvokeAsync<HubResponse<HostSession>>(nameof(GameHub.CreateRoom), null)).Value!;
        await anna.InvokeAsync<HubResponse<PlayerSession>>(nameof(GameHub.JoinRoom), room.RoomCode, "Anna");

        var result = await anna.InvokeAsync<HubResponse<IReadOnlyList<Track>>>(nameof(GameHub.Search), "Veronica Maggio");

        Assert.That(result.Value, Is.EqualTo(FixedCatalog.Tracks));
    }

    [Test]
    public async Task Search_AsHostOrOutsideARoom_ReturnsRoomNotFound()
    {
        await using var host = CreateConnection();
        await using var stranger = CreateConnection();
        await Task.WhenAll(host.StartAsync(), stranger.StartAsync());
        await host.InvokeAsync<HubResponse<HostSession>>(nameof(GameHub.CreateRoom), null);

        var asHost = await host.InvokeAsync<HubResponse<IReadOnlyList<Track>>>(nameof(GameHub.Search), "kent");
        var asStranger = await stranger.InvokeAsync<HubResponse<IReadOnlyList<Track>>>(nameof(GameHub.Search), "kent");

        Assert.That(asHost.Error?.Code, Is.EqualTo("room.not_found"));
        Assert.That(asStranger.Error?.Code, Is.EqualTo("room.not_found"));
    }

    private HubConnection CreateConnection()
    {
        var server = _factory.Server;
        return new HubConnectionBuilder()
            .WithUrl(new Uri(server.BaseAddress, "gamehub"), HttpTransportType.LongPolling, options =>
            {
                options.HttpMessageHandlerFactory = _ => server.CreateHandler();
            })
            .Build();
    }

    private sealed class FixedCatalog : IMusicCatalog
    {
        public static readonly IReadOnlyList<Track> Tracks = [new Track(44112901, "Jag kommer", "Veronica Maggio", "Satan i gatan", "c", false)];

        public Task<Result<IReadOnlyList<Track>>> SearchAsync(SearchQuery query, CancellationToken cancellationToken = default) =>
            Task.FromResult(Result<IReadOnlyList<Track>>.Success(Tracks));

        public Task<Result<PlayableTrack>> GetPlayableTrackAsync(long trackId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("Hubben spelar inga klipp än.");
    }
}
