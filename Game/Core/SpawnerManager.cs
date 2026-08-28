using Microsoft.Xna.Framework;
using System;
using TwinStickShooter.Entities;

namespace TwinStickShooter.Core
{
    /// <summary>
    /// Administra generadores anclados al mapa. No usa el pool de enemigos.
    /// </summary>
    public class SpawnerManager
    {
        public struct SpawnerData
        {
            public Vector2 Position;
            public float Timer;
            public float Health;
            public bool Active;
        }

        private readonly SpawnerData[] _spawners;
        private readonly EnemyManager _enemyManager;

        public SpawnerManager(int capacity, EnemyManager enemyManager)
        {
            _spawners = new SpawnerData[capacity];
            _enemyManager = enemyManager;
        }

        public SpawnerData[] Spawners => _spawners;

        public float GetHealth(int index)
        {
            return index >= 0 && index < _spawners.Length ? _spawners[index].Health : 0f;
        }

        public int ActiveCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < _spawners.Length; i++)
                {
                    if (_spawners[i].Active)
                    {
                        count++;
                    }
                }
                return count;
            }
        }

        public bool Register(Vector2 position)
        {
            for (int i = 0; i < _spawners.Length; i++)
            {
                if (!_spawners[i].Active)
                {
                    _spawners[i] = new SpawnerData
                    {
                        Position = position,
                        Timer = GameConstants.SpawnerInterval,
                        Health = GameConstants.SpawnerHealth,
                        Active = true
                    };
                    return true;
                }
            }

            return false;
        }

        public void Reset()
        {
            for (int i = 0; i < _spawners.Length; i++)
            {
                _spawners[i].Active = false;
                _spawners[i].Health = 0f;
                _spawners[i].Timer = 0f;
            }
        }

        public void Update(float deltaTime)
        {
            for (int i = 0; i < _spawners.Length; i++)
            {
                if (!_spawners[i].Active)
                {
                    continue;
                }

                _spawners[i].Timer -= deltaTime;
                if (_spawners[i].Timer > 0f)
                {
                    continue;
                }

                _spawners[i].Timer = GameConstants.SpawnerInterval;
                if (_enemyManager.ActiveCount >= GameConstants.MaxEnemies ||
                    _enemyManager.CountNear(_spawners[i].Position, GameConstants.SpawnerChildRadius) >=
                    GameConstants.SpawnerMaxConcurrentChildren)
                {
                    continue;
                }

                int childCount = _enemyManager.CountNear(
                    _spawners[i].Position, GameConstants.SpawnerChildRadius);
                float angle = childCount * MathHelper.TwoPi / GameConstants.SpawnerMaxConcurrentChildren;
                Vector2 offset = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) *
                    (GameConstants.SpawnerRadius + GameConstants.EnemyRadius + 8f);
                _enemyManager.Spawn(_spawners[i].Position + offset, Vector2.Zero, EnemyType.Swarmer);
            }
        }

        public bool ApplyDamage(int index, float amount)
        {
            if (index < 0 || index >= _spawners.Length ||
                !_spawners[index].Active || amount <= 0f)
            {
                return false;
            }

            _spawners[index].Health -= amount;
            if (_spawners[index].Health <= 0f)
            {
                _spawners[index].Health = 0f;
                _spawners[index].Active = false;
            }

            return true;
        }

        public int FindHit(Vector2 position, float radius)
        {
            for (int i = 0; i < _spawners.Length; i++)
            {
                if (_spawners[i].Active &&
                    Vector2.Distance(position, _spawners[i].Position) < radius + GameConstants.SpawnerRadius)
                {
                    return i;
                }
            }

            return -1;
        }
    }
}