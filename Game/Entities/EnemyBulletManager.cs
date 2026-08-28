using System;
using Microsoft.Xna.Framework;
using TwinStickShooter.Core;

namespace TwinStickShooter.Entities
{
    /// <summary>
    /// Administra las balas disparadas por enemigos mediante un pool separado.
    /// </summary>
    public class EnemyBulletManager
    {
        private readonly ObjectPool<Bullet> _pool;
        private readonly LevelManager _levelManager;

        public EnemyBulletManager(LevelManager levelManager)
        {
            _pool = new ObjectPool<Bullet>(GameConstants.MaxEnemyBullets);
            _levelManager = levelManager;
        }

        /// <summary>Array fijo de balas activas e inactivas, usado por el renderer.</summary>
        public Bullet[] Bullets => _pool.Items;

        public void Spawn(Vector2 position, float angle, Color color)
        {
            if (!_pool.TryAcquire(out int index, out Bullet bullet))
            {
                return;
            }

            bullet.PoolIndex = index;
            bullet.Position = position;
            bullet.Velocity = new Vector2(
                (float)Math.Cos(angle),
                (float)Math.Sin(angle)) * GameConstants.TurretBulletSpeed;
            bullet.LifeRemaining = GameConstants.BulletLifetimeSeconds;
            bullet.Color = color;
        }

        public void Update(float deltaTime, Player[] players)
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
                bool hitPlayer = false;

                for (int j = 0; j < players.Length; j++)
                {
                    Player player = players[j];
                    if (!player.IsActive)
                    {
                        continue;
                    }

                    float distance = Vector2.Distance(bullet.Position, player.Position);
                    if (distance < bullet.Radius + player.Radius)
                    {
                        player.TakeDamage(GameConstants.TurretBulletDamage);
                        hitPlayer = true;
                        break;
                    }
                }

                if (bullet.LifeRemaining <= 0f || offWorld || hitWall || hitPlayer)
                {
                    bullet.Active = false;
                    _pool.Release(bullet.PoolIndex);
                }
            }
        }
    }
}