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
        public bool Active;
        public int PoolIndex;
        public Vector2 Position;
        public Vector2 Velocity;
        public float Radius;
        public Color Color;

        public void Reset()
        {
            Active = true;
            Position = Vector2.Zero;
            Velocity = Vector2.Zero;
            Radius = GameConstants.EnemyRadius;
            Color = Color.Red;
        }
    }
}