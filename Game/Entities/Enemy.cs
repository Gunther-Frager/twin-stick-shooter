using Microsoft.Xna.Framework;
using TwinStickShooter.Core;

namespace TwinStickShooter.Entities
{
    /// <summary>
    /// Enemigo administrado por ObjectPool&lt;Enemy&gt;. Nunca se instancia
    /// con `new Enemy()` fuera del pool; los campos se reescriben en
    /// Reset() (al reciclarse) y en EnemyManager.Spawn() (al activarse).
    /// </summary>
    public class Enemy : IPoolable
    {
        public bool Active { get; set; }
        public int PoolIndex;
        public Vector2 Position { get; set; }
        public float Radius { get; set; }
        public Color Color { get; set; }
        public Vector2 Velocity;
        public EnemyType Type;
        public float Health;
        public float MaxHealth;
        public float ShootCooldown;
        public float ShootTimer;
        public float DetectionRange;
        public Vector2 RoamDirection;
        public float RoamChangeTimer;

        public void Reset()
        {
            Active = true;
            Position = Vector2.Zero;
            Velocity = Vector2.Zero;
            Radius = GameConstants.EnemyRadius;
            Color = Color.Red;
            Type = EnemyType.Swarmer;
            Health = GameConstants.SwarmerHealth;
            MaxHealth = GameConstants.SwarmerHealth;
            ShootCooldown = 0f;
            ShootTimer = 0f;
            DetectionRange = 0f;
            RoamDirection = Vector2.Zero;
            RoamChangeTimer = 0f;
        }
    }
}