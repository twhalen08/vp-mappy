using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Mappy.Services
{
    public class OverlayTokenOptions
    {
        public string Issuer { get; set; } = "MappyOverlay";
        public string Audience { get; set; } = "MappyOverlay";
        public string SigningKey { get; set; } = "replace-with-strong-key";
        public int TokenLifetimeMinutes { get; set; } = 5;
    }

    public class OverlayTokenService
    {
        private readonly OverlayTokenOptions _options;
        private readonly SymmetricSecurityKey _signingKey;
        private readonly JwtSecurityTokenHandler _tokenHandler = new JwtSecurityTokenHandler();

        public OverlayTokenService(IOptions<OverlayTokenOptions> options)
        {
            _options = options.Value;
            _signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey));
        }

        public string CreateToken(string avatarName)
        {
            var now = DateTime.UtcNow;
            var claims = new[]
            {
                new Claim("name", avatarName)
            };

            var credentials = new SigningCredentials(_signingKey, SecurityAlgorithms.HmacSha256);
            var token = new JwtSecurityToken(
                issuer: _options.Issuer,
                audience: _options.Audience,
                claims: claims,
                notBefore: now,
                expires: now.AddMinutes(_options.TokenLifetimeMinutes),
                signingCredentials: credentials);

            return _tokenHandler.WriteToken(token);
        }

        public ClaimsPrincipal? ValidateToken(string token)
        {
            try
            {
                return _tokenHandler.ValidateToken(token, BuildTokenValidationParameters(_options), out _);
            }
            catch
            {
                return null;
            }
        }

        public static TokenValidationParameters BuildTokenValidationParameters(OverlayTokenOptions options)
        {
            var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.SigningKey));
            return new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = options.Issuer,
                ValidateAudience = true,
                ValidAudience = options.Audience,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = signingKey,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromSeconds(30),
                NameClaimType = "name"
            };
        }
    }
}
