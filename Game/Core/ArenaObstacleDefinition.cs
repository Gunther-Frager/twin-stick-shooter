using Microsoft.Xna.Framework;

namespace TwinStickShooter.Core
{
    public sealed class ArenaObstacleDefinition
    {
        public ArenaObstacleDefinition(MapCapsule capsuleInTiles, bool destructible, int hitPoints)
        {
            CapsuleInTiles = capsuleInTiles;
            IsDestructible = destructible;
            HitPoints = hitPoints;
        }

        public MapCapsule CapsuleInTiles { get; }
        public bool IsDestructible { get; }
        public int HitPoints { get; }
    }
}