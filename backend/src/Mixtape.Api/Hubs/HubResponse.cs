using Mixtape.Domain.Common;

namespace Mixtape.Api.Hubs;

// Formen på varje svar från hubben: antingen ett värde eller ett fel. Speglas i hubContract.ts.
public sealed record HubResponse<T>(T? Value, Error? Error)
{
    public static HubResponse<T> From(Result<T> result) => result.IsSuccess ? new(result.Value, null) : new(default, result.Error);
}
