using Microsoft.Xna.Framework;
using TwinStickShooter.Core;

namespace TwinStickShooter.Entities
{
    /// <summary>
    /// Proyectil administrado por ObjectPool&lt;Bullet&gt;. Nunca se instancia
    /// con `new Bullet()` fuera del pool; los campos se reescriben en
    /// Reset() (al reciclarse) y en BulletManager.Spawn() (al dispararse).
    /// </summary>
    public class Bullet : IPoolable
    {
        public bool Active;
        public int PoolIndex;
        public Vector2 Position;
        public Vector2 Velocity;
        public int OwnerIndex;
        public float LifeRemaining;
        public Color Color;

        public void Reset()
        {
            Active = true;
            Position = Vector2.Zero;
            Velocity = Vector2.Zero;
            OwnerIndex = -1;
            LifeRemaining = 0f;
            Color = Color.White;
        }
    }
}
