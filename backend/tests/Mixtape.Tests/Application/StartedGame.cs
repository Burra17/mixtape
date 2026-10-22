using Mixtape.Application.Rooms;

namespace Mixtape.Tests.Application;

public sealed record StartedGame(HostSession Host, IReadOnlyList<PlayerSession> Players);
