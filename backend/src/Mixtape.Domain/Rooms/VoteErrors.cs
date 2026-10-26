using Mixtape.Domain.Common;

namespace Mixtape.Domain.Rooms;

public static class VoteErrors
{
    public static readonly Error OwnEntry = new("vote.own_entry", "Du kan inte rösta på din egen låt.");

    public static readonly Error AlreadyVoted = new("vote.already_voted", "Du har redan röstat.");

    public static readonly Error NotFound = new("vote.not_found", "Låten finns inte i omröstningen.");
}
