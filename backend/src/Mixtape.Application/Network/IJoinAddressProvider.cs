namespace Mixtape.Application.Network;

public interface IJoinAddressProvider
{
    // Laptopens IPv4-adresser som mobilerna kan nå, med den mest sannolika först.
    IReadOnlyList<string> GetAddresses();
}
