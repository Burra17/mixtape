using Microsoft.Extensions.DependencyInjection;
using Mixtape.Application.Connections;
using Mixtape.Application.Music;
using Mixtape.Application.Rooms;

namespace Mixtape.Application;

public static class DependencyInjection
{
    // Registrerar Application-lagrets tjänster, alltså handlers.
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddTransient<CreateRoomHandler>();
        services.AddTransient<CheckRoomHandler>();
        services.AddTransient<ResumeRoomHandler>();
        services.AddTransient<PauseGameHandler>();
        services.AddTransient<ContinueGameHandler>();
        services.AddTransient<EndGameHandler>();
        services.AddTransient<JoinRoomHandler>();
        services.AddTransient<RejoinRoomHandler>();
        services.AddTransient<LeaveRoomHandler>();
        services.AddTransient<RemovePlayerHandler>();
        services.AddTransient<ChooseModeHandler>();
        services.AddTransient<UpdateAuxCordSettingsHandler>();
        services.AddTransient<UpdateDropTheNeedleSettingsHandler>();
        services.AddTransient<StartGameHandler>();
        services.AddTransient<AdvancePhasesHandler>();
        services.AddTransient<DisconnectHandler>();
        services.AddTransient<CleanUpRoomsHandler>();
        services.AddTransient<SearchTracksHandler>();
        services.AddTransient<SubmitTrackHandler>();
        services.AddTransient<GetClipHandler>();
        services.AddTransient<VoteHandler>();
        services.AddTransient<GetBallotHandler>();
        services.AddTransient<PlayAgainHandler>();
        return services;
    }
}
