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

        public void Spawn(Vector2 position, float angle, int ownerIndex, Color color, int maxBounces = 0)
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
            bullet.RemainingBounces = Math.Max(0, maxBounces);
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

                Vector2 previousPosition = bullet.Position;
                Vector2 nextPosition = previousPosition + bullet.Velocity * deltaTime;
                bullet.LifeRemaining -= deltaTime;

                bool offWorld =
                    nextPosition.X < -bullet.Radius ||
                    nextPosition.X > GameConstants.WorldWidth + bullet.Radius ||
                    nextPosition.Y < -bullet.Radius ||
                    nextPosition.Y > GameConstants.WorldHeight + bullet.Radius;

                bool hitWall = _levelManager.TrySweepCircle(
                    previousPosition,
                    nextPosition,
                    bullet.Radius,
                    out Vector2 hitPosition,
                    out Vector2 hitNormal);
                bullet.Position = hitWall ? hitPosition : nextPosition;

                if (hitWall && bullet.RemainingBounces > 0 && hitNormal.LengthSquared() > 0.000001f)
                {
                    bullet.Velocity = Vector2.Reflect(bullet.Velocity, hitNormal);
                    bullet.RemainingBounces--;
                    bullet.Position += hitNormal * 0.01f;
                    hitWall = false;
                }

                int spawnerIndex = spawnerManager.FindHit(bullet.Position, bullet.Radius);
                bool hitTarget = false;
                if (!hitWall && spawnerIndex >= 0)
                {
                    spawnerManager.ApplyDamage(spawnerIndex, 1f);
                    hitTarget = true;
                }
                else if (!hitWall)
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
