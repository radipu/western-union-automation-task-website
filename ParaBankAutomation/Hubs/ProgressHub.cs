using Microsoft.AspNetCore.SignalR;

namespace ParaBankAutomation.Hubs;

public sealed class ProgressHub : Hub
{
    // All push events are initiated server-side via IHubContext<ProgressHub>.
    // This class is intentionally minimal.
}
