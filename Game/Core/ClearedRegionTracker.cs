using System;
using System.Collections.Generic;

namespace TwinStickShooter.Core
{
    /// <summary>
    /// Rastrea la limpieza de regiones del mapa ejecutando comprobaciones costosas
    /// únicamente cuando ocurre una muerte/desactivación o tras la carga inicial.
    /// </summary>
    public class ClearedRegionTracker
    {
        private readonly HashSet<int> _clearedRegions = new HashSet<int>();
        private int _lastEnemyDeathVersion = -1;
        private int _lastSpawnerDeathVersion = -1;
        private bool _isDirty = true;

        public IReadOnlyCollection<int> ClearedRegions => _clearedRegions;

        public void Reset()
        {
            _clearedRegions.Clear();
            _lastEnemyDeathVersion = -1;
            _lastSpawnerDeathVersion = -1;
            _isDirty = true;
        }

        public void MarkDirty()
        {
            _isDirty = true;
        }

        public bool ShouldCheck(int enemyDeathVersion, int spawnerDeathVersion)
        {
            return _isDirty ||
                   enemyDeathVersion != _lastEnemyDeathVersion ||
                   spawnerDeathVersion != _lastSpawnerDeathVersion;
        }

        public void RecordCheck(int enemyDeathVersion, int spawnerDeathVersion)
        {
            _isDirty = false;
            _lastEnemyDeathVersion = enemyDeathVersion;
            _lastSpawnerDeathVersion = spawnerDeathVersion;
        }

        public bool TryMarkCleared(int regionId)
        {
            if (regionId <= 0) return false;
            return _clearedRegions.Add(regionId);
        }

        public bool IsCleared(int regionId)
        {
            return _clearedRegions.Contains(regionId);
        }

        public int ProcessRegions(
            IReadOnlyList<MapRegionDefinition> regions,
            Func<int, int> getEnemyAliveCount,
            Func<int, int> getSpawnerAliveCount,
            int enemyDeathVersion,
            int spawnerDeathVersion,
            Action<MapRegionDefinition> onRegionCleared = null)
        {
            if (!ShouldCheck(enemyDeathVersion, spawnerDeathVersion))
            {
                return 0;
            }

            RecordCheck(enemyDeathVersion, spawnerDeathVersion);

            if (regions == null || regions.Count == 0) return 0;

            int newlyCleared = 0;
            for (int i = 0; i < regions.Count; i++)
            {
                MapRegionDefinition region = regions[i];
                if (region.Id <= 0 || IsCleared(region.Id)) continue;
                if (getEnemyAliveCount(region.Id) > 0 || getSpawnerAliveCount(region.Id) > 0) continue;

                if (TryMarkCleared(region.Id))
                {
                    newlyCleared++;
                    onRegionCleared?.Invoke(region);
                }
            }

            return newlyCleared;
        }
    }
}
