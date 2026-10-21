using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Mixtape.Application.Rooms;

namespace Mixtape.Infrastructure.Rooms;

// Driver fasernas klocka. Ett varv var 250:e ms gör att en fas byts högst en kvarts sekund efter sin deadline, och det märks inte.
public sealed class GameClockService(AdvancePhasesHandler advancePhases, TimeProvider time, ILogger<GameClockService> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMilliseconds(250);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval, time);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            // Samma skäl som i RoomCleanupService: ett undantag får inte stoppa backenden mitt i spelet.
            try
            {
                await advancePhases.HandleAsync();
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Klockan kunde inte byta fas");
            }
        }
    }
}
