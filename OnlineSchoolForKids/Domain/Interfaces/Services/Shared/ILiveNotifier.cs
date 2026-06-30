namespace Domain.Interfaces.Services.Shared
{
    public interface ILiveNotifier
    {
        /// <summary>Broadcasts to everyone in the session that it has ended.</summary>
        Task NotifySessionEndedAsync(string sessionId, CancellationToken ct = default);
    }
}
