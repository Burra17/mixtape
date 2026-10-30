using Mixtape.Application.Connections;
using Mixtape.Application.Rooms;
using Mixtape.Domain.Rooms;
using Mixtape.Infrastructure.Connections;
using Mixtape.Infrastructure.Rooms;
using Mixtape.Tests.Domain;

namespace Mixtape.Tests.Application;

public sealed class AdvancePhasesHandlerTests
{
    private FakeTimeProvider _time = null!;
    private InMemoryGameRoomRepository _rooms = null!;
    private RecordingNotifier _notifier = null!;
    private InMemoryConnectionRegistry _connections = null!;
    private AdvancePhasesHandler _advance = null!;

    [SetUp]
    public void SetUp()
    {
        _time = new FakeTimeProvider(new DateTimeOffset(2026, 10, 20, 20, 0, 0, TimeSpan.Zero));
        _rooms = new InMemoryGameRoomRepository();
        _notifier = new RecordingNotifier();
        _connections = new InMemoryConnectionRegistry();
        _advance = new AdvancePhasesHandler(_rooms, _notifier, _time);
    }

    [Test]
    public async Task Handle_AfterTheDeadline_SendsTheNextPhaseOnce()
    {
        var game = await GameStarter.StartAsync(_rooms, _connections, _time);
        var host = game.Host;
        await SubmitTwoAsync(game);

        _time.Advance(GameSettings.Default.SubmittingDuration);
        await _advance.HandleAsync();
        await _advance.HandleAsync();

        var sent = _notifier.Sent.Single();
        Assert.That(sent.RoomCode, Is.EqualTo(host.RoomCode));
        Assert.That(sent.Phase, Is.EqualTo(GamePhase.Playing));
        Assert.That(sent.RemainingMs, Is.EqualTo(15_000));
    }

    [Test]
    public async Task Handle_BeforeTheDeadline_SendsNothing()
    {
        await GameStarter.StartAsync(_rooms, _connections, _time);

        _time.Advance(GameSettings.Default.SubmittingDuration - TimeSpan.FromSeconds(1));
        await _advance.HandleAsync();

        Assert.That(_notifier.Sent, Is.Empty);
    }

    [Test]
    public async Task Handle_RoomsInTheLobby_SendsNothing()
    {
        await new CreateRoomHandler(_rooms, _connections, _time).HandleAsync(null, "host");

        _time.Advance(TimeSpan.FromHours(1));
        await _advance.HandleAsync();

        Assert.That(_notifier.Sent, Is.Empty);
    }

    [Test]
    public async Task Handle_WhenTheLastPlayerWhoHadNotSubmittedDisconnects_SendsTheNextPhase()
    {
        var game = await GameStarter.StartAsync(_rooms, _connections, _time);
        await SubmitTwoAsync(game);

        await new DisconnectHandler(_rooms, _connections, _time).HandleAsync(_connections.Unregister("Cecilia")!);
        await _advance.HandleAsync();

        Assert.That(_notifier.Sent.Single().Phase, Is.EqualTo(GamePhase.Playing));
    }

    // Två bidrag, så att rundan inte hoppas över vid deadline, men Cecilia är inte klar.
    private Task SubmitTwoAsync(StartedGame game) =>
        _rooms.WithRoomAsync<bool>(game.Host.RoomCode, room =>
        {
            room.SubmitTrack(game.Players[0].PlayerId, TestTrack.Of(1, "Ett", "Artist"), _time.GetUtcNow());
            room.SubmitTrack(game.Players[1].PlayerId, TestTrack.Of(2, "Två", "Artist"), _time.GetUtcNow());
            return true;
        });

    private sealed class RecordingNotifier : IGameNotifier
    {
        public List<GameState> Sent { get; } = [];

        public Task GameStateUpdatedAsync(GameState state)
        {
            Sent.Add(state);
            return Task.CompletedTask;
        }
    }
}
