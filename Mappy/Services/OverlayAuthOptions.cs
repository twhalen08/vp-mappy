namespace Mappy.Services
{
    public class OverlayAuthOptions
    {
        public string Secret { get; set; } = string.Empty;
        public int TokenLifetimeMinutes { get; set; } = 15;
        public string OverlayBaseUrl { get; set; } = string.Empty;
    }
}
