using Mixtape.Domain.Common;

namespace Mixtape.Domain.Rooms;

public static class EntryErrors
{
    public static readonly Error TrackTaken = new("entry.track_taken", "Någon hann före dig!");

    public static readonly Error AlreadySubmitted = new("entry.already_submitted", "Du har redan skickat in.");
}
