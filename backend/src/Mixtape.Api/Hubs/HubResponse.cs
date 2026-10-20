using Mixtape.Domain.Common;

namespace Mixtape.Api.Hubs;

// Formen på varje svar från hubben: antingen ett värde eller ett fel. Speglas i hubContract.ts.
public sealed record HubResponse<T>(T? Value, Error? Error)
{
    public static HubResponse<T> From(Result<T> result) => result.IsSuccess ? new(result.Value, null) : new(default, result.Error);

    // För handlers som ger mer än klienten ska ha, till exempel lobbyn som i stället skickas med LobbyUpdated.
    public static HubResponse<T> From<TSource>(Result<TSource> result, Func<TSource, T> select) =>
        result.IsSuccess ? new(select(result.Value), null) : new(default, result.Error);
}
