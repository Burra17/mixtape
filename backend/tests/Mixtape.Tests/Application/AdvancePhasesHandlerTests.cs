using Mixtape.Application.Rooms;
using Mixtape.Domain.Rooms;
using Mixtape.Infrastructure.Connections;
using Mixtape.Infrastructure.Prompts;
using Mixtape.Infrastructure.Rooms;

namespace Mixtape.Tests.Application;

public sealed class AdvancePhasesHandlerTests
{
    private FakeTimeProvider _time = null!;
    private InMemoryGameRoomRepository _rooms = null!;
    private RecordingNotifier _notifier = null!;
    private AdvancePhasesHandler _advance = null!;

    [SetUp]
    public void SetUp()
    {
        _time = new FakeTimeProvider(new DateTimeOffset(2026, 10, 20, 20, 0, 0, TimeSpan.Zero));
        _rooms = new InMemoryGameRoomRepository();
        _notifier = new RecordingNotifier();
        _advance = new AdvancePhasesHandler(_rooms, _notifier, _time);
    }

    [Test]
    public async Task Handle_AfterTheDeadline_SendsTheNextPhaseOnce()
    {
        var host = await StartGameAsync();

        _time.Advance(GameRoom.SubmittingDuration);
        await _advance.HandleAsync();
        await _advance.HandleAsync();

        var sent = _notifier.Sent.Single();
        Assert.That(sent.RoomCode, Is.EqualTo(host.RoomCode));
        Assert.That(sent.Phase, Is.EqualTo(GamePhase.Playing));
        Assert.That(sent.RemainingMs, Is.EqualTo(3 * 15_000));
    }

    [Test]
    public async Task Handle_BeforeTheDeadline_SendsNothing()
    {
        await StartGameAsync();

        _time.Advance(GameRoom.SubmittingDuration - TimeSpan.FromSeconds(1));
        await _advance.HandleAsync();

        Assert.That(_notifier.Sent, Is.Empty);
    }

    [Test]
    public async Task Handle_RoomsInTheLobby_SendsNothing()
    {
        await new CreateRoomHandler(_rooms, new InMemoryConnectionRegistry(), _time).HandleAsync(null, "host");

        _time.Advance(TimeSpan.FromHours(1));
        await _advance.HandleAsync();

        Assert.That(_notifier.Sent, Is.Empty);
    }

    private async Task<HostSession> StartGameAsync()
    {
        var connections = new InMemoryConnectionRegistry();
        var host = (await new CreateRoomHandler(_rooms, connections, _time).HandleAsync(null, "host")).Value.Session;
        var join = new JoinRoomHandler(_rooms, connections, _time);
        foreach (var name in new[] { "Anna", "Bertil", "Cecilia" })
        {
            await join.HandleAsync(host.RoomCode, name, name);
        }

        await new StartGameHandler(_rooms, PromptBank.LoadEmbedded(new Random(1)), _time).HandleAsync(host);
        return host;
    }

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
