using API.Hubs;
using Domain.Interfaces.Services.Shared;
using Microsoft.AspNetCore.SignalR;

namespace Infrastructure.Services.Shared
{
    public class LiveNotifier : ILiveNotifier
    {
        private readonly IHubContext<LiveSessionHub> _hub;

        public LiveNotifier(IHubContext<LiveSessionHub> hub) => _hub = hub;

        public async Task NotifySessionEndedAsync(string sessionId, CancellationToken ct = default)
        {
            await _hub.Clients
                .Group(sessionId)
                .SendAsync("SessionEnded", cancellationToken: ct);
        }
    }
}