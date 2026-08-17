using Microsoft.Xna.Framework;
using TwinStickShooter.Core;

namespace TwinStickShooter.Entities
{
    /// <summary>
    /// Administra el ciclo de vida de los enemigos usando ObjectPool&lt;Enemy&gt;.
    /// Update() recorre el array fijo (tamaño MaxEnemies) filtrando por
    /// Active: sin listas dinámicas, sin allocations por frame.
    /// </summary>
    public class EnemyManager
    {
        private readonly ObjectPool<Enemy> _pool;
        private readonly LevelManager _levelManager;

        public EnemyManager(LevelManager levelManager)
        {
            _pool = new ObjectPool<Enemy>(GameConstants.MaxEnemies);
            _levelManager = levelManager;
        }

        /// <summary>Array fijo de enemigos (activas e inactivas); usado por el renderer.</summary>
        public Enemy[] Enemies => _pool.Items;

        public void Spawn(Vector2 position, Vector2 velocity)
        {
            if (!_pool.TryAcquire(out int index, out Enemy enemy))
            {
                return; // pool lleno: se descarta el spawn en vez de alocar de más
            }

            enemy.PoolIndex = index;
            enemy.Position = position;
            enemy.Velocity = velocity;
            enemy.Radius = GameConstants.EnemyRadius;
            enemy.Color = Color.Red;
        }

        public void Update(float deltaTime)
        {
            Enemy[] items = _pool.Items;

            for (int i = 0; i < items.Length; i++)
            {
                Enemy enemy = items[i];
                if (!enemy.Active)
                {
                    continue;
                }

                enemy.Position += enemy.Velocity * deltaTime;

                bool offWorld =
                    enemy.Position.X < -enemy.Radius ||
                    enemy.Position.X > GameConstants.WorldWidth + enemy.Radius ||
                    enemy.Position.Y < -enemy.Radius ||
                    enemy.Position.Y > GameConstants.WorldHeight + enemy.Radius;

                bool hitWall = _levelManager.CheckCollision(enemy.Position, enemy.Radius);

                if (offWorld || hitWall)
                {
                    enemy.Active = false;
                    _pool.Release(enemy.PoolIndex);
                }
            }
        }
    }
}