using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Mappy.Services;

namespace Mappy.Hubs
{
    public class LocationHub : Hub
    {
        private readonly OverlayTokenService _overlayTokenService;
        private readonly AvatarLocationCache _locationCache;

        public LocationHub(OverlayTokenService overlayTokenService, AvatarLocationCache locationCache)
        {
            _overlayTokenService = overlayTokenService;
            _locationCache = locationCache;
        }

        public override async Task OnConnectedAsync()
        {
            var token = GetTokenFromRequest();
            if (!_overlayTokenService.TryValidateToken(token, out var payload))
            {
                await Clients.Caller.SendAsync("TokenInvalid");
                Context.Abort();
                return;
            }

            await Clients.Caller.SendAsync("AssignAvatar", payload.AvatarName);
            await Clients.Caller.SendAsync("ReceiveLocationsSnapshot", _locationCache.GetSnapshot());
            await base.OnConnectedAsync();
        }

        private string? GetTokenFromRequest()
        {
            var httpContext = Context.GetHttpContext();
            if (httpContext == null)
            {
                return null;
            }

            var query = httpContext.Request.Query;
            var token = query["access_token"].ToString();
            if (string.IsNullOrWhiteSpace(token))
            {
                token = query["token"].ToString();
            }

            if (string.IsNullOrWhiteSpace(token))
            {
                var authorization = httpContext.Request.Headers["Authorization"].ToString();
                const string bearerPrefix = "Bearer ";
                if (!string.IsNullOrWhiteSpace(authorization) &&
                    authorization.StartsWith(bearerPrefix, System.StringComparison.OrdinalIgnoreCase))
                {
                    token = authorization.Substring(bearerPrefix.Length).Trim();
                }
            }

            return string.IsNullOrWhiteSpace(token) ? null : token;
        }
    }
}
