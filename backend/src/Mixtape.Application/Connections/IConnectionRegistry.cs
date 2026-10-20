namespace Mixtape.Application.Connections;

// Kopplingen mellan SignalR-anslutningar och deras ägare. Domänen vet aldrig något om ConnectionId, som byts vid varje återanslutning.
public interface IConnectionRegistry
{
    // En anslutning har bara en ägare, så en ny ersätter den gamla.
    void Register(string connectionId, ConnectionOwner owner);

    // Tar bort anslutningen och ger ägaren, eller null om anslutningen inte var kopplad.
    ConnectionOwner? Unregister(string connectionId);

    bool HasConnection(ConnectionOwner owner);
}
