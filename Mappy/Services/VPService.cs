using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using VpNet;
using Mappy.Hubs;
using Mappy.Services;
using Microsoft.Extensions.Options;

namespace Mappy
{
    public class VPService : IHostedService
    {
        private VirtualParadiseClient _client;
        private Timer _pollTimer;
        private readonly IHubContext<LocationHub> _hubContext;
        private readonly IOverlayTokenService _overlayTokenService;
        private readonly OverlayAuthOptions _overlayAuthOptions;
        // Store names of avatars that are ghosted.
        private readonly HashSet<string> _ghostedAvatars = new HashSet<string>();
        private readonly HashSet<int> _overlaySentSessions = new HashSet<int>();

        public VPService(
            IHubContext<LocationHub> hubContext,
            IOverlayTokenService overlayTokenService,
            IOptions<OverlayAuthOptions> overlayAuthOptions)
        {
            _client = new VirtualParadiseClient();
            _hubContext = hubContext;
            _overlayTokenService = overlayTokenService;
            _overlayAuthOptions = overlayAuthOptions.Value;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            Console.WriteLine("VPService starting...");

            // Configure the VP client.
            _client.Configuration = new VirtualParadiseClientConfiguration
            {
                ApplicationName = "VPService",
                ApplicationVersion = "1.0",
                BotName = "Mappy",
                UserName = "Tom",  // Replace with your bot username
                World = new World { Name = "Blizzard" } // Replace with your world name
            };

            // Subscribe to the AvatarLeft event.
            _client.AvatarLeft += async (sender, e) =>
            {
                Console.WriteLine($"[Avatar Left] {e.Avatar.Name}");
                // Do not remove ghosted avatars from _ghostedAvatars so that ghost mode persists.
                await _hubContext.Clients.All.SendAsync("RemoveAvatar", e.Avatar.Name);
                _overlaySentSessions.Remove(e.Avatar.Session);
            };


            _client.AvatarEntered += async (sender, e) =>
            {
                await SendOverlayToAvatar(e.Avatar);
            };
            // Subscribe to chat messages for ghost/unghost commands.
            _client.ChatMessageReceived += async (sender, e) =>
            {
                string message = e.ChatMessage.Message;
                string avatarName = e.Avatar.Name;

                if (message.StartsWith("!ghost", StringComparison.OrdinalIgnoreCase))
                {
                    if (_ghostedAvatars.Add(avatarName))
                    {
                        _client.ConsoleMessage(e.Avatar, "", "Ghost Mode 👻 Activated - You are hidden on the livemap.");
                        // Immediately broadcast removal.
                        await _hubContext.Clients.All.SendAsync("RemoveAvatar", avatarName);
                    }
                }
                else if (message.StartsWith("!unghost", StringComparison.OrdinalIgnoreCase))
                {
                    if (_ghostedAvatars.Remove(avatarName))
                    {
                        _client.ConsoleMessage(e.Avatar, "", "Ghost Mode 👻 Deactivated - You're visible on the livemap");
                        // Optionally, force a location update by calling PollAvatarPositions (or let the next polling cycle send an update).
                    }
                }
            };

            try
            {
                // Login and enter the world (replace "" with your password)
                await _client.LoginAndEnterAsync("xxxxxxxxx", true);
                Console.WriteLine("Logged into Virtual Paradise.");

                foreach (var avatar in _client.Avatars)
                {
                    if (!avatar.IsBot)
                    {
                        await SendOverlayToAvatar(avatar);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error logging in: " + ex.Message);
                return;
            }

            // Start polling avatar positions every 500 ms.
            _pollTimer = new Timer(async state => await PollAvatarPositions(), null, 0, 500);
        }

        private async Task PollAvatarPositions()
        {
            try
            {
                var avatars = _client.Avatars;
                Console.WriteLine($"Polling positions for {avatars.Count} avatars:");
                foreach (var avatar in avatars)
                {
                    if (avatar.IsBot)
                        continue;

                    if (!_overlaySentSessions.Contains(avatar.Session))
                    {
                        await SendOverlayToAvatar(avatar);
                    }

                    // If the avatar is ghosted, skip sending location updates.
                    if (_ghostedAvatars.Contains(avatar.Name))
                    {
                        // Ensure any marker for this avatar is removed.
                        await _hubContext.Clients.All.SendAsync("RemoveAvatar", avatar.Name);
                        continue;
                    }

                    // Get up-to-date position using the session id.
                    var avQuery = _client.GetAvatar(avatar.Session);
                    var pos = avQuery.Location.Position;
                    Console.WriteLine($"  {avatar.Name} (Session: {avatar.Session}): ({pos.X:F2}, {pos.Y:F2}, {pos.Z:F2})");

                    // Broadcast the update to connected SignalR clients.
                    await _hubContext.Clients.All.SendAsync("ReceiveLocation",
                        avatar.Name, pos.X, pos.Z, pos.Y);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error polling avatar positions: " + ex.Message);
            }
        }

        private Task SendOverlayToAvatar(Avatar avatar)
        {
            var token = _overlayTokenService.CreateToken(avatar.Name, avatar.Session);
            var overlayUrl = $"{_overlayAuthOptions.OverlayBaseUrl.TrimEnd('/')}/minimap.html?token={Uri.EscapeDataString(token)}&user={Uri.EscapeDataString(avatar.Name)}";
            _client.UrlSendOverlay(avatar, overlayUrl);
            _overlaySentSessions.Add(avatar.Session);
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            Console.WriteLine("VPService stopping...");
            _pollTimer?.Dispose();
            return Task.CompletedTask;
        }
    }
}
