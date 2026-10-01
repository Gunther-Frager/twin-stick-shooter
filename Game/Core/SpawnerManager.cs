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
            /// <summary>El slot está ocupado, pero su región todavía no se ha activado.</summary>
            public bool Dormant;
            /// <summary>Los descendientes heredan este ID y cuentan para la limpieza regional.</summary>
            public int RegionId;
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

        /// <summary>Incluye spawners Dormant: la región no está limpia mientras quede alguno vivo.</summary>
        public int GetAliveCountInRegion(int regionId)
        {
            int count = 0;
            for (int i = 0; i < _spawners.Length; i++)
            {
                if (_spawners[i].Active && _spawners[i].RegionId == regionId) count++;
            }

            return count;
        }

        public bool Register(Vector2 position, int regionId = -1)
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
                        Active = true,
                        Dormant = regionId > 0,
                        RegionId = regionId,
                    };
                    return true;
                }
            }

            return false;
        }

        /// <summary>Despierta de una vez los spawners pertenecientes a una región.</summary>
        public void ActivateRegion(int regionId)
        {
            for (int i = 0; i < _spawners.Length; i++)
            {
                if (_spawners[i].Active && _spawners[i].RegionId == regionId)
                    _spawners[i].Dormant = false;
            }
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
                if (_spawners[i].Dormant) continue;

                _spawners[i].Timer -= deltaTime;
                if (_spawners[i].Timer > 0f)
                {
                    continue;
                }

                _spawners[i].Timer = GameConstants.SpawnerInterval;
                TrySpawnChild(i);
            }
        }

        /// <summary>Busca una posición libre; si el pool o la sala están llenos, el timer reintentará luego.</summary>
        private void TrySpawnChild(int spawnerIndex)
        {
            SpawnerData spawner = _spawners[spawnerIndex];
            if (_enemyManager.ActiveCount >= _enemyManager.Capacity) return;

            int childCount = _enemyManager.CountNear(spawner.Position, GameConstants.SpawnerChildRadius);
            float startAngle = (childCount % 8) * MathHelper.TwoPi / 8f;
            for (int ring = 0; ring < 4; ring++)
            {
                float distance = GameConstants.SpawnerRadius + GameConstants.EnemyRadius + 8f + ring *
                    (GameConstants.EnemyRadius * 2f + 8f);
                for (int slot = 0; slot < 8; slot++)
                {
                    float angle = startAngle + slot * MathHelper.TwoPi / 8f;
                    Vector2 offset = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * distance;
                    if (_enemyManager.Spawn(spawner.Position + offset, Vector2.Zero, EnemyType.Swarmer, spawner.RegionId))
                        return;
                }
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