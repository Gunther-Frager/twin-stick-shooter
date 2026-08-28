using System;
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
        private readonly Random _random;

        public EnemyManager(LevelManager levelManager)
        {
            _pool = new ObjectPool<Enemy>(GameConstants.MaxEnemies);
            _levelManager = levelManager;
            _random = new Random();
        }

        /// <summary>Array fijo de enemigos (activas e inactivas); usado por el renderer.</summary>
        public Enemy[] Enemies => _pool.Items;

        /// <summary>Cantidad de enemigos activos actualmente.</summary>
        public int ActiveCount
        {
            get
            {
                int count = 0;
                Enemy[] items = _pool.Items;
                for (int i = 0; i < items.Length; i++)
                {
                    if (items[i].Active)
                    {
                        count++;
                    }
                }
                return count;
            }
        }

        public void Spawn(Vector2 position, Vector2 velocity, EnemyType type = EnemyType.Swarmer)
        {
            if (!_pool.TryAcquire(out int index, out Enemy enemy))
            {
                return; // pool lleno: se descarta el spawn en vez de alocar de más
            }

            enemy.PoolIndex = index;
            enemy.Type = type;
            enemy.Position = position;
            enemy.Velocity = velocity;
            enemy.Radius = GameConstants.EnemyRadius;
            enemy.Color = Color.Red;

            if (type == EnemyType.Roamer)
            {
                enemy.RoamDirection = Vector2.UnitX;
                enemy.RoamChangeTimer = 0f;
            }
        }

        public void Update(float deltaTime)
        {
            Enemy[] items = _pool.Items;
            int activeEnemies = 0;

            for (int i = 0; i < items.Length; i++)
            {
                Enemy enemy = items[i];
                if (!enemy.Active)
                {
                    continue;
                }
                activeEnemies++;
                Console.WriteLine($"[EnemyManager] Enemigo activo en posición: {enemy.Position}");

                switch (enemy.Type)
                {
                    case Core.EnemyType.Swarmer:
                        UpdateSwarmer(enemy, deltaTime);
                        break;
                    case Core.EnemyType.Roamer:
                        UpdateRoamer(enemy, deltaTime);
                        break;
                    case Core.EnemyType.Turret:
                    case Core.EnemyType.Spawner:
                        // Aún no implementados — se agregan en etapas siguientes.
                        break;
                }
            }
            Console.WriteLine($"[EnemyManager] Enemigos activos: {activeEnemies}");
        }

        /// <summary>Actualiza el movimiento y ciclo de vida de un Swarmer.</summary>
        private void UpdateSwarmer(Enemy enemy, float deltaTime)
        {
            enemy.Position = PhysicsHelper.MoveWithCollision(
                enemy,
                enemy.Velocity * deltaTime,
                _levelManager);

            bool offWorld =
                enemy.Position.X < -enemy.Radius ||
                enemy.Position.X > GameConstants.WorldWidth + enemy.Radius ||
                enemy.Position.Y < -enemy.Radius ||
                enemy.Position.Y > GameConstants.WorldHeight + enemy.Radius;

            if (offWorld)
            {
                enemy.Active = false;
                _pool.Release(enemy.PoolIndex);
                Console.WriteLine($"[EnemyManager] Enemigo desactivado por salir del mundo.");
            }
        }

        /// <summary>Actualiza el movimiento errático y el rebote en paredes de un Roamer.</summary>
        private void UpdateRoamer(Enemy enemy, float deltaTime)
        {
            enemy.RoamChangeTimer -= deltaTime;
            if (enemy.RoamChangeTimer <= 0f)
            {
                float angle = (float)(_random.NextDouble() * MathHelper.TwoPi);
                enemy.RoamDirection = new Vector2(
                    (float)Math.Cos(angle),
                    (float)Math.Sin(angle));
                enemy.RoamChangeTimer = GameConstants.RoamerDirectionChangeMinSeconds +
                    (float)_random.NextDouble() *
                    (GameConstants.RoamerDirectionChangeMaxSeconds -
                     GameConstants.RoamerDirectionChangeMinSeconds);
            }

            Vector2 moveDelta = enemy.RoamDirection * GameConstants.RoamerSpeed * deltaTime;
            Vector2 newPosition = PhysicsHelper.MoveWithCollision(enemy, moveDelta, _levelManager);

            if (Math.Abs(newPosition.X - enemy.Position.X) < 0.01f)
            {
                enemy.RoamDirection = new Vector2(-enemy.RoamDirection.X, enemy.RoamDirection.Y);
            }
            if (Math.Abs(newPosition.Y - enemy.Position.Y) < 0.01f)
            {
                enemy.RoamDirection = new Vector2(enemy.RoamDirection.X, -enemy.RoamDirection.Y);
            }

            enemy.Position = newPosition;

            bool offWorld =
                enemy.Position.X < -enemy.Radius ||
                enemy.Position.X > GameConstants.WorldWidth + enemy.Radius ||
                enemy.Position.Y < -enemy.Radius ||
                enemy.Position.Y > GameConstants.WorldHeight + enemy.Radius;

            if (offWorld)
            {
                enemy.Active = false;
                _pool.Release(enemy.PoolIndex);
            }
        }
    }
}