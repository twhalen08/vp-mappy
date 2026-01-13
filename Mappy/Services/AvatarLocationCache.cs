using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Mappy.Services
{
    public sealed record AvatarLocationSnapshot(string AvatarName, double X, double Z, double Altitude);

    public sealed class AvatarLocationCache
    {
        private readonly ConcurrentDictionary<string, AvatarLocationSnapshot> _locations = new();

        public void Update(string avatarName, double x, double z, double altitude)
        {
            _locations[avatarName] = new AvatarLocationSnapshot(avatarName, x, z, altitude);
        }

        public IReadOnlyCollection<AvatarLocationSnapshot> GetSnapshot()
        {
            return _locations.Values.ToList();
        }
    }
}
