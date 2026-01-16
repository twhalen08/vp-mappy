using Microsoft.AspNetCore.SignalR;

namespace Mappy.Hubs
{
    public class LocationHub : Hub
    {
        public override Task OnConnectedAsync()
        {
            if (Context.User?.Identity?.IsAuthenticated != true)
            {
                Context.Abort();
                return Task.CompletedTask;
            }

            return base.OnConnectedAsync();
        }

        public string? GetCurrentUser()
        {
            return Context.User?.Identity?.Name;
        }
    }
}
