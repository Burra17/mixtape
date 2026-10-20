using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Mixtape.Application.Rooms;

namespace Mixtape.Infrastructure.Rooms;

// Städar bort rum som stått tomma i GameRoom.AbandonedAfter. En gång i minuten räcker, eftersom gränsen är en halvtimme.
public sealed class RoomCleanupService(CleanUpRoomsHandler cleanUp, TimeProvider time, ILogger<RoomCleanupService> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval, time);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            var removed = await cleanUp.HandleAsync();
            if (removed > 0)
            {
                logger.LogInformation("Städade bort {Count} tomma rum", removed);
            }
        }
    }
}
