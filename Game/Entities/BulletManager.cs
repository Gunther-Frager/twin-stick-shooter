using System;
using Microsoft.Xna.Framework;
using TwinStickShooter.Core;

namespace TwinStickShooter.Entities
{
    /// <summary>
    /// Administra el ciclo de vida de las balas usando ObjectPool&lt;Bullet&gt;.
    /// Update() recorre el array fijo (tamaño MaxBullets) filtrando por
    /// Active: sin listas dinámicas, sin allocations por frame.
    /// </summary>
    public class BulletManager
    {
        private readonly ObjectPool<Bullet> _pool;

        public BulletManager()
        {
            _pool = new ObjectPool<Bullet>(GameConstants.MaxBullets);
        }

        /// <summary>Array fijo de balas (activas e inactivas); usado por el renderer.</summary>
        public Bullet[] Bullets => _pool.Items;

        public void Spawn(Vector2 position, float angle, int ownerIndex, Color color)
        {
            if (!_pool.TryAcquire(out int index, out Bullet bullet))
            {
                return; // pool lleno: se descarta el disparo en vez de alocar de más
            }

            bullet.PoolIndex = index;
            bullet.Position = position;
            bullet.Velocity = new Vector2(
                (float)Math.Cos(angle),
                (float)Math.Sin(angle)) * GameConstants.BulletSpeed;
            bullet.OwnerIndex = ownerIndex;
            bullet.LifeRemaining = GameConstants.BulletLifetimeSeconds;
            bullet.Color = color;
        }

        public void Update(float deltaTime)
        {
            Bullet[] items = _pool.Items;

            for (int i = 0; i < items.Length; i++)
            {
                Bullet bullet = items[i];
                if (!bullet.Active)
                {
                    continue;
                }

                bullet.Position += bullet.Velocity * deltaTime;
                bullet.LifeRemaining -= deltaTime;

                bool offScreen =
                    bullet.Position.X < -GameConstants.BulletRadius ||
                    bullet.Position.X > GameConstants.ScreenWidth + GameConstants.BulletRadius ||
                    bullet.Position.Y < -GameConstants.BulletRadius ||
                    bullet.Position.Y > GameConstants.ScreenHeight + GameConstants.BulletRadius;

                if (bullet.LifeRemaining <= 0f || offScreen)
                {
                    bullet.Active = false;
                    _pool.Release(bullet.PoolIndex);
                }
            }
        }
    }
}
