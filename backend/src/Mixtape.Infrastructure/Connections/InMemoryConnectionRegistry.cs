using System.Collections.Concurrent;
using Mixtape.Application.Connections;

namespace Mixtape.Infrastructure.Connections;

// Anslutningarna lever lika länge som processen, precis som rummen. Registreras som singleton.
public sealed class InMemoryConnectionRegistry : IConnectionRegistry
{
    private readonly ConcurrentDictionary<string, ConnectionOwner> _owners = new();

    public void Register(string connectionId, ConnectionOwner owner) => _owners[connectionId] = owner;

    public ConnectionOwner? Unregister(string connectionId) => _owners.TryRemove(connectionId, out var owner) ? owner : null;

    // Går igenom alla anslutningar. Det är några tiotal en kväll, så en uppslagstabell åt andra hållet behövs inte.
    public bool HasConnection(ConnectionOwner owner) => _owners.Values.Contains(owner);
}
