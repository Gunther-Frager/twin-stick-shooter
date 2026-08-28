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
        private readonly LevelManager _levelManager;

        public BulletManager(LevelManager levelManager)
        {
            _pool = new ObjectPool<Bullet>(GameConstants.MaxBullets);
            _levelManager = levelManager;
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

        public void Update(float deltaTime, EnemyManager enemyManager, SpawnerManager spawnerManager)
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

                bool offWorld =
                    bullet.Position.X < -bullet.Radius ||
                    bullet.Position.X > GameConstants.WorldWidth + bullet.Radius ||
                    bullet.Position.Y < -bullet.Radius ||
                    bullet.Position.Y > GameConstants.WorldHeight + bullet.Radius;

                bool hitWall = _levelManager.CheckCollision(bullet.Position, bullet.Radius);

                int spawnerIndex = spawnerManager.FindHit(bullet.Position, bullet.Radius);
                bool hitTarget = false;
                if (spawnerIndex >= 0)
                {
                    spawnerManager.ApplyDamage(spawnerIndex, 1f);
                    hitTarget = true;
                }
                else
                {
                    int enemyIndex = enemyManager.FindHit(bullet.Position, bullet.Radius);
                    if (enemyIndex >= 0)
                    {
                        enemyManager.ApplyDamage(enemyIndex, 1f);
                        hitTarget = true;
                    }
                }

                if (bullet.LifeRemaining <= 0f || offWorld || hitWall || hitTarget)
                {
                    bullet.Active = false;
                    _pool.Release(bullet.PoolIndex);
                }
            }
        }
    }
}
