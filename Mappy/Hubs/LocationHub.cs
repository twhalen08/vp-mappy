using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Mappy.Services;

namespace Mappy.Hubs
{
    public class LocationHub : Hub
    {
        private readonly IOverlayTokenService _overlayTokenService;

        public LocationHub(IOverlayTokenService overlayTokenService)
        {
            _overlayTokenService = overlayTokenService;
        }

        public override Task OnConnectedAsync()
        {
            var httpContext = Context.GetHttpContext();
            var token = httpContext?.Request.Query["token"].ToString();

            if (!_overlayTokenService.TryValidateToken(token, out var payload))
            {
                Context.Abort();
                return Task.CompletedTask;
            }

            Context.Items["avatarName"] = payload.AvatarName;
            Context.Items["sessionId"] = payload.SessionId;

            return base.OnConnectedAsync();
        }
    }
}
