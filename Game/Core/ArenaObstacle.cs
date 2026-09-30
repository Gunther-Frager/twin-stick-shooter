using Microsoft.Xna.Framework;

namespace TwinStickShooter.Core
{
    public sealed class ArenaObstacle
    {
        public ArenaObstacle(MapCapsule capsule, bool destructible, int hitPoints)
        {
            Capsule = capsule;
            IsDestructible = destructible;
            MaxHitPoints = destructible ? hitPoints : 0;
            HitPoints = MaxHitPoints;
        }

        public MapCapsule Capsule { get; }
        public bool IsDestructible { get; }
        public int MaxHitPoints { get; }
        public int HitPoints { get; private set; }
        public Color OutlineColor => IsDestructible
            ? Color.Lerp(new Color(255, 170, 40), new Color(255, 45, 25), 1f - HitPoints / (float)MaxHitPoints)
            : new Color(175, 190, 205);

        public bool ApplyDamage(int amount)
        {
            if (!IsDestructible || amount <= 0) return false;
            HitPoints = System.Math.Max(0, HitPoints - amount);
            return HitPoints == 0;
        }
    }
}