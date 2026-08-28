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

        public void Clear()
        {
            for (int i = 0; i < _pool.Items.Length; i++)
            {
                if (_pool.Items[i].Active)
                {
                    _pool.Items[i].Active = false;
                    _pool.Release(_pool.Items[i].PoolIndex);
                }
            }
        }

        public bool Spawn(Vector2 position, Vector2 velocity, EnemyType type = EnemyType.Swarmer)
        {
            if (!_pool.TryAcquire(out int index, out Enemy enemy))
            {
            return false; // pool lleno: se descarta el spawn en vez de alocar de más
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
            else if (type == EnemyType.Turret)
            {
                enemy.Health = enemy.MaxHealth = GameConstants.TurretHealth;
                enemy.DetectionRange = GameConstants.TurretDetectionRange;
                enemy.ShootCooldown = GameConstants.TurretShootCooldown;
                enemy.ShootTimer = 0f;
            }

            return true;
        }

        public int CountNear(Vector2 position, float radius)
        {
            float radiusSquared = radius * radius;
            int count = 0;
            for (int i = 0; i < _pool.Items.Length; i++)
            {
                Enemy enemy = _pool.Items[i];
                if (enemy.Active && Vector2.DistanceSquared(position, enemy.Position) <= radiusSquared)
                {
                    count++;
                }
            }
            return count;
        }

        public bool ApplyDamage(int index, float amount)
        {
            if (index < 0 || index >= _pool.Items.Length || amount <= 0f)
            {
                return false;
            }

            Enemy enemy = _pool.Items[index];
            if (!enemy.Active)
            {
                return false;
            }

            enemy.Health -= amount;
            if (enemy.Health <= 0f)
            {
                enemy.Active = false;
                _pool.Release(enemy.PoolIndex);
            }

            return true;
        }

        public int FindHit(Vector2 position, float radius)
        {
            for (int i = 0; i < _pool.Items.Length; i++)
            {
                Enemy enemy = _pool.Items[i];
                if (enemy.Active &&
                    Vector2.Distance(position, enemy.Position) < radius + enemy.Radius)
                {
                    return i;
                }
            }

            return -1;
        }

        public void Update(float deltaTime, Player[] players, EnemyBulletManager enemyBullets)
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

                switch (enemy.Type)
                {
                    case Core.EnemyType.Swarmer:
                        UpdateSwarmer(enemy, deltaTime);
                        break;
                    case Core.EnemyType.Roamer:
                        UpdateRoamer(enemy, deltaTime);
                        break;
                    case Core.EnemyType.Turret:
                        UpdateTurret(enemy, deltaTime, players, enemyBullets);
                        break;
                    case Core.EnemyType.Spawner:
                        // Aún no implementados — se agregan en etapas siguientes.
                        break;
                }
            }
        }

        private Player FindNearestActivePlayer(Vector2 position, Player[] players)
        {
            Player nearest = null;
            float bestDistSq = float.MaxValue;

            for (int i = 0; i < players.Length; i++)
            {
                if (!players[i].IsActive)
                {
                    continue;
                }

                float distSq = Vector2.DistanceSquared(position, players[i].Position);
                if (distSq < bestDistSq)
                {
                    bestDistSq = distSq;
                    nearest = players[i];
                }
            }

            return nearest;
        }

        private void UpdateTurret(Enemy enemy, float deltaTime, Player[] players, EnemyBulletManager enemyBullets)
        {
            Player target = FindNearestActivePlayer(enemy.Position, players);
            if (target == null)
            {
                return;
            }

            float distance = Vector2.Distance(enemy.Position, target.Position);
            if (distance > enemy.DetectionRange)
            {
                return;
            }

            enemy.ShootTimer -= deltaTime;
            if (enemy.ShootTimer > 0f)
            {
                return;
            }

            enemy.ShootTimer = enemy.ShootCooldown;
            Vector2 direction = target.Position - enemy.Position;
            if (direction != Vector2.Zero)
            {
                direction.Normalize();
            }

            float angle = (float)Math.Atan2(direction.Y, direction.X);
            enemyBullets.Spawn(enemy.Position, angle, enemy.Color);
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