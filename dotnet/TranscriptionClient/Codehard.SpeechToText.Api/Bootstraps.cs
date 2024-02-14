using Codehard.SpeechToText.Api.Abstractions;
using Microsoft.AspNetCore.SignalR;

namespace Codehard.SpeechToText.Api;

internal static class Bootstraps
{
    public static void MapDiscoverableHub<THub>(this WebApplication application)
        where THub : Hub, IDiscoverableHub
    {
        application.MapHub<THub>(THub.Route);
    }
}