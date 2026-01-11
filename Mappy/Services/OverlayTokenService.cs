using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace Mappy.Services
{
    public interface IOverlayTokenService
    {
        string CreateToken(string avatarName, int sessionId);
        bool TryValidateToken(string token, out OverlayTokenPayload payload);
    }

    public class OverlayTokenService : IOverlayTokenService
    {
        private readonly OverlayAuthOptions _options;

        public OverlayTokenService(IOptions<OverlayAuthOptions> options)
        {
            _options = options.Value;
        }

        public string CreateToken(string avatarName, int sessionId)
        {
            var payload = new OverlayTokenPayload
            {
                AvatarName = avatarName,
                SessionId = sessionId,
                ExpiresAtUtc = DateTimeOffset.UtcNow.AddMinutes(_options.TokenLifetimeMinutes).ToUnixTimeSeconds()
            };

            var payloadJson = JsonSerializer.Serialize(payload);
            var payloadBytes = Encoding.UTF8.GetBytes(payloadJson);
            var payloadEncoded = Base64UrlEncode(payloadBytes);

            var signature = ComputeSignature(payloadEncoded);
            return $"{payloadEncoded}.{signature}";
        }

        public bool TryValidateToken(string token, out OverlayTokenPayload payload)
        {
            payload = default;

            if (string.IsNullOrWhiteSpace(token))
            {
                return false;
            }

            var parts = token.Split('.');
            if (parts.Length != 2)
            {
                return false;
            }

            var payloadEncoded = parts[0];
            var signature = parts[1];
            var expectedSignature = ComputeSignature(payloadEncoded);

            if (!CryptographicOperations.FixedTimeEquals(
                    Encoding.UTF8.GetBytes(signature),
                    Encoding.UTF8.GetBytes(expectedSignature)))
            {
                return false;
            }

            OverlayTokenPayload? parsedPayload;
            try
            {
                var payloadJson = Encoding.UTF8.GetString(Base64UrlDecode(payloadEncoded));
                parsedPayload = JsonSerializer.Deserialize<OverlayTokenPayload>(payloadJson);
            }
            catch (Exception)
            {
                return false;
            }

            if (parsedPayload == null)
            {
                return false;
            }

            if (parsedPayload.ExpiresAtUtc <= DateTimeOffset.UtcNow.ToUnixTimeSeconds())
            {
                return false;
            }

            payload = parsedPayload;
            return true;
        }

        private string ComputeSignature(string payloadEncoded)
        {
            var keyBytes = Encoding.UTF8.GetBytes(_options.Secret);
            using var hmac = new HMACSHA256(keyBytes);
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payloadEncoded));
            return Base64UrlEncode(hash);
        }

        private static string Base64UrlEncode(byte[] bytes)
        {
            return Convert.ToBase64String(bytes)
                .Replace("+", "-")
                .Replace("/", "_")
                .TrimEnd('=');
        }

        private static byte[] Base64UrlDecode(string encoded)
        {
            var padded = encoded.Replace("-", "+").Replace("_", "/");
            switch (padded.Length % 4)
            {
                case 2:
                    padded += "==";
                    break;
                case 3:
                    padded += "=";
                    break;
            }
            return Convert.FromBase64String(padded);
        }
    }

    public sealed record OverlayTokenPayload
    {
        public string AvatarName { get; init; }
        public int SessionId { get; init; }
        public long ExpiresAtUtc { get; init; }
    }
}
