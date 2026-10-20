using System.Net.NetworkInformation;
using System.Net.Sockets;
using Mixtape.Application.Network;

namespace Mixtape.Infrastructure.Network;

public sealed class JoinAddressProvider : IJoinAddressProvider
{
    // WSL, Docker, Hyper-V och VPN skapar egna kort som mobilerna inte når. De känns igen på namnet.
    private static readonly string[] VirtualMarkers = ["virtual", "vethernet", "hyper-v", "vmware", "virtualbox", "wsl", "docker", "vpn", "tap"];

    public IReadOnlyList<string> GetAddresses()
    {
        var candidates = NetworkInterface.GetAllNetworkInterfaces()
            .Where(nic => nic.OperationalStatus == OperationalStatus.Up)
            .Where(nic => nic.NetworkInterfaceType is not (NetworkInterfaceType.Loopback or NetworkInterfaceType.Tunnel))
            .Where(nic => !IsVirtual(nic))
            .Select(nic => (Properties: nic.GetIPProperties(), Wireless: nic.NetworkInterfaceType == NetworkInterfaceType.Wireless80211))
            .ToList();

        // Kort med en default gateway kommer först, och bland dem wifi, eftersom mobilerna sitter på wifi.
        return candidates
            .OrderByDescending(c => c.Properties.GatewayAddresses.Any(g => g.Address.AddressFamily == AddressFamily.InterNetwork))
            .ThenByDescending(c => c.Wireless)
            .SelectMany(c => c.Properties.UnicastAddresses)
            .Where(a => a.Address.AddressFamily == AddressFamily.InterNetwork)
            .Select(a => a.Address.ToString())
            .Distinct()
            .ToList();
    }

    private static bool IsVirtual(NetworkInterface nic)
    {
        var text = $"{nic.Name} {nic.Description}".ToLowerInvariant();
        return VirtualMarkers.Any(text.Contains);
    }
}
