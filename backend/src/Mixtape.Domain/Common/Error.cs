namespace Mixtape.Domain.Common;

// Code är till för koden och testerna, Message visas för spelaren.
public sealed record Error(string Code, string Message);
