using System.Security.Cryptography;

namespace Mixtape.Domain.Common;

public static class SecretToken
{
    // 16 slumpade byte från en kryptografisk källa, så att ingen kan gissa sig till någon annans token.
    public static string Create() => Convert.ToHexString(RandomNumberGenerator.GetBytes(16));
}
