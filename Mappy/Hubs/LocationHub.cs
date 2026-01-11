using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Mappy.Services;

namespace Mappy.Hubs
{
    public class LocationHub : Hub
    {
        private readonly OverlayTokenService _overlayTokenService;

        public LocationHub(OverlayTokenService overlayTokenService)
        {
            _overlayTokenService = overlayTokenService;
        }

        public override async Task OnConnectedAsync()
        {
            var token = Context.GetHttpContext()?.Request.Query["access_token"].ToString();
            if (!_overlayTokenService.TryValidateToken(token, out var payload))
            {
                await Clients.Caller.SendAsync("TokenInvalid");
                Context.Abort();
                return;
            }

            await Clients.Caller.SendAsync("AssignAvatar", payload.AvatarName);
            await base.OnConnectedAsync();
        }
    }
}
