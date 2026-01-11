using System;
using Microsoft.Extensions.Caching.Memory;

namespace Mappy.Services
{
    public sealed record OverlayTokenPayload(string AvatarName, string? WorldName, int? SessionId);

    public sealed class OverlayTokenService
    {
        private readonly IMemoryCache _cache;
        private readonly TimeSpan _ttl = TimeSpan.FromMinutes(5);
        private readonly bool _consumeOnValidate = true;

        public OverlayTokenService(IMemoryCache cache)
        {
            _cache = cache;
        }

        public string GenerateToken(string avatarName, string? worldName = null, int? sessionId = null)
        {
            if (string.IsNullOrWhiteSpace(avatarName))
            {
                throw new ArgumentException("Avatar name is required.", nameof(avatarName));
            }

            var token = Guid.NewGuid().ToString("N");
            var payload = new OverlayTokenPayload(avatarName, worldName, sessionId);
            _cache.Set(token, payload, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = _ttl
            });
            return token;
        }

        public bool TryValidateToken(string? token, out OverlayTokenPayload payload)
        {
            payload = null!;
            if (string.IsNullOrWhiteSpace(token))
            {
                return false;
            }

            if (!_cache.TryGetValue(token, out OverlayTokenPayload cached))
            {
                return false;
            }

            payload = cached;
            if (_consumeOnValidate)
            {
                _cache.Remove(token);
            }
            return true;
        }
    }
}
