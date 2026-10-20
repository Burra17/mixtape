namespace Mixtape.Domain.Common;

// Utfallet av en regel som kan säga nej. Inte sealed, eftersom Result<T> bygger vidare på den.
public class Result
{
    protected Result(Error? error)
    {
        Error = error;
    }

    public Error? Error { get; }

    public bool IsSuccess => Error is null;

    public static Result Success() => new(null);

    public static Result Failure(Error error) => new(error);

    public static implicit operator Result(Error error) => Failure(error);
}
