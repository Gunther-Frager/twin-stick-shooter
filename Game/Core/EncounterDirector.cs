using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace TwinStickShooter.Core
{
    public sealed class EncounterSpawn
    {
        public EncounterSpawn(int regionIndex, int clusterId, Point position, EnemyType type)
        {
            RegionIndex = regionIndex;
            ClusterId = clusterId;
            Position = position;
            Type = type;
        }

        public int RegionIndex { get; }
        public int ClusterId { get; }
        public Point Position { get; }
        public EnemyType Type { get; }
    }

    /// <summary>
    /// Creates deterministic room encounter plans without creating game entities.
    /// </summary>
    public static class EncounterDirector
    {
        private sealed class EncounterRegion
        {
            public int RegionIndex;
            public int Budget;
            public List<Point> Available = new List<Point>();
        }

        public static IReadOnlyList<EncounterSpawn> Plan(
            int[,] grid,
            IList<Rectangle> rooms,
            Point spawnPoint,
            int cellSize,
            MapGenerationSettings settings)
        {
            if (grid == null) throw new ArgumentNullException(nameof(grid));
            if (rooms == null) throw new ArgumentNullException(nameof(rooms));
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            if (cellSize <= 0) throw new ArgumentOutOfRangeException(nameof(cellSize));

            int[,] regionIdMap = new int[grid.GetLength(0), grid.GetLength(1)];
            for (int x = 0; x < regionIdMap.GetLength(0); x++)
            {
                for (int y = 0; y < regionIdMap.GetLength(1); y++) regionIdMap[x, y] = -1;
            }

            List<MapRegionDefinition> regions = new List<MapRegionDefinition>();
            for (int regionId = 0; regionId < rooms.Count; regionId++)
            {
                Rectangle bounds = rooms[regionId];
                MapRegionKind kind = regionId == 0 ? MapRegionKind.Spawn : MapRegionKind.Arena;
                regions.Add(new MapRegionDefinition(
                    regionId,
                    kind,
                    bounds,
                    0,
                    kind == MapRegionKind.Spawn ? 0 : Math.Max(0, settings.EncounterDifficultyBudget)));

                if (kind == MapRegionKind.Spawn) continue;
                for (int x = bounds.Left; x < bounds.Right; x++)
                {
                    for (int y = bounds.Top; y < bounds.Bottom; y++)
                    {
                        if (x >= 0 && x < grid.GetLength(0) && y >= 0 && y < grid.GetLength(1) && grid[x, y] == 0)
                            regionIdMap[x, y] = regionId;
                    }
                }
            }

            return Plan(grid, regionIdMap, regions, spawnPoint, cellSize, settings);
        }

        /// <summary>Planifica budgets fijos solo en celdas de arenas y pockets con ownership regional.</summary>
        public static IReadOnlyList<EncounterSpawn> Plan(
            int[,] grid,
            int[,] regionIdMap,
            IReadOnlyList<MapRegionDefinition> regions,
            Point spawnPoint,
            int cellSize,
            MapGenerationSettings settings)
        {
            if (grid == null) throw new ArgumentNullException(nameof(grid));
            if (regionIdMap == null) throw new ArgumentNullException(nameof(regionIdMap));
            if (regions == null) throw new ArgumentNullException(nameof(regions));
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            if (cellSize <= 0) throw new ArgumentOutOfRangeException(nameof(cellSize));
            if (grid.GetLength(0) != regionIdMap.GetLength(0) || grid.GetLength(1) != regionIdMap.GetLength(1))
                throw new ArgumentException("El ownership regional debe tener las mismas dimensiones que el mapa.", nameof(regionIdMap));

            List<EncounterSpawn> plan = new List<EncounterSpawn>();
            int remainingSlots = Math.Max(0, settings.EnemyPoolCapacity);
            int maxGroupSize = Math.Max(1, settings.EncounterMaxGroupSize);
            int minGroupSize = Math.Min(Math.Max(1, settings.EncounterMinGroupSize), maxGroupSize);
            float clusterRadius = Math.Max(0f, settings.EncounterClusterRadius);
            float safeDistance = Math.Max(0f, settings.EncounterSpawnSafeDistance);
            double safeDistanceSquared = (double)safeDistance * safeDistance;
            double clusterRadiusSquared = (double)clusterRadius * clusterRadius;
            Random random = new Random(settings.Seed);
            int nextClusterId = 0;
            List<EncounterRegion> eligibleRegions = new List<EncounterRegion>();

            for (int regionIndex = 0; regionIndex < regions.Count; regionIndex++)
            {
                MapRegionDefinition region = regions[regionIndex];
                if (region.Kind == MapRegionKind.Spawn || region.Budget <= 0) continue;

                EncounterRegion encounterRegion = new EncounterRegion
                {
                    RegionIndex = region.Id,
                    Budget = region.Budget,
                };
                for (int x = region.Bounds.Left; x < region.Bounds.Right; x++)
                {
                    for (int y = region.Bounds.Top; y < region.Bounds.Bottom; y++)
                    {
                        if (x < 0 || x >= grid.GetLength(0) || y < 0 || y >= grid.GetLength(1) ||
                            grid[x, y] != 0 || regionIdMap[x, y] != region.Id)
                        {
                            continue;
                        }

                        long dx = (long)x - spawnPoint.X;
                        long dy = (long)y - spawnPoint.Y;
                        if (dx * dx + dy * dy < safeDistanceSquared)
                        {
                            continue;
                        }

                        encounterRegion.Available.Add(new Point(x, y));
                    }
                }

                if (encounterRegion.Available.Count > 0) eligibleRegions.Add(encounterRegion);
            }

            while (remainingSlots > 0 && eligibleRegions.Count > 0)
            {
                EncounterRegion region = eligibleRegions[random.Next(eligibleRegions.Count)];
                List<EnemyType> affordableForRoom = GetAffordableTypes(settings, region.Budget);
                if (affordableForRoom.Count == 0)
                {
                    eligibleRegions.Remove(region);
                    continue;
                }

                Point anchor = region.Available[random.Next(region.Available.Count)];
                List<Point> cluster = new List<Point>();
                for (int i = 0; i < region.Available.Count; i++)
                {
                    Point candidate = region.Available[i];
                    long dx = (long)candidate.X - anchor.X;
                    long dy = (long)candidate.Y - anchor.Y;
                    if (dx * dx + dy * dy <= clusterRadiusSquared)
                    {
                        cluster.Add(candidate);
                    }
                }

                int minimumEnemyCost = GetMinimumCost(settings);
                int affordableGroupSize = region.Budget / minimumEnemyCost;
                int resourceLimitedGroupSize = Math.Min(remainingSlots, affordableGroupSize);
                if (resourceLimitedGroupSize < minGroupSize)
                {
                    eligibleRegions.Remove(region);
                    continue;
                }

                int maxAvailableGroupSize = Math.Min(
                    Math.Min(maxGroupSize, cluster.Count),
                    resourceLimitedGroupSize);
                if (maxAvailableGroupSize < minGroupSize)
                {
                    region.Available.Remove(anchor);
                    if (region.Available.Count == 0) eligibleRegions.Remove(region);
                    continue;
                }
                int groupSize = random.Next(minGroupSize, maxAvailableGroupSize + 1);
                int clusterId = nextClusterId++;
                for (int i = 0; i < groupSize; i++)
                {
                    int reservedBudget = minimumEnemyCost * (groupSize - i - 1);
                    List<EnemyType> affordableTypes = GetAffordableTypes(settings, region.Budget - reservedBudget);
                    int clusterIndex = random.Next(cluster.Count);
                    Point cell = cluster[clusterIndex];
                    cluster.RemoveAt(clusterIndex);
                    for (int regionIndex = 0; regionIndex < eligibleRegions.Count; regionIndex++)
                    {
                        eligibleRegions[regionIndex].Available.Remove(cell);
                    }

                    EnemyType type = affordableTypes[random.Next(affordableTypes.Count)];
                    region.Budget -= GetCost(type, settings);
                    remainingSlots--;
                    Point worldPosition = new Point(
                        cell.X * cellSize + cellSize / 2,
                        cell.Y * cellSize + cellSize / 2);
                    plan.Add(new EncounterSpawn(region.RegionIndex, clusterId, worldPosition, type));
                }

                if (region.Budget > 0 && region.Available.Count > 0 && GetAffordableTypes(settings, region.Budget).Count > 0)
                {
                    continue;
                }

                for (int i = eligibleRegions.Count - 1; i >= 0; i--)
                {
                    if (eligibleRegions[i].Available.Count == 0 || eligibleRegions[i] == region)
                    {
                        eligibleRegions.RemoveAt(i);
                    }
                }
            }

            return plan.AsReadOnly();
        }

        private static List<EnemyType> GetAffordableTypes(MapGenerationSettings settings, int budget)
        {
            List<EnemyType> types = new List<EnemyType>();
            if (Math.Max(1, settings.EncounterSwarmerCost) <= budget) types.Add(EnemyType.Swarmer);
            if (Math.Max(1, settings.EncounterRusherCost) <= budget) types.Add(EnemyType.Rusher);
            if (Math.Max(1, settings.EncounterRoamerCost) <= budget) types.Add(EnemyType.Roamer);
            if (Math.Max(1, settings.EncounterTurretCost) <= budget) types.Add(EnemyType.Turret);
            if (Math.Max(1, settings.EncounterStaticShooterCost) <= budget) types.Add(EnemyType.StaticShooter);
            if (Math.Max(1, settings.EncounterMobileGeneratorCost) <= budget) types.Add(EnemyType.MobileGenerator);
            if (Math.Max(1, settings.EncounterSpawnerCost) <= budget) types.Add(EnemyType.Spawner);
            return types;
        }

        private static int GetMinimumCost(MapGenerationSettings settings)
        {
            return Math.Min(
                Math.Min(Math.Min(Math.Max(1, settings.EncounterSwarmerCost), Math.Max(1, settings.EncounterRusherCost)),
                    Math.Min(Math.Max(1, settings.EncounterRoamerCost), Math.Max(1, settings.EncounterTurretCost))),
                Math.Min(Math.Min(Math.Max(1, settings.EncounterStaticShooterCost), Math.Max(1, settings.EncounterMobileGeneratorCost)),
                    Math.Max(1, settings.EncounterSpawnerCost)));
        }

        private static int GetCost(EnemyType type, MapGenerationSettings settings)
        {
            switch (type)
            {
                case EnemyType.Roamer:
                    return Math.Max(1, settings.EncounterRoamerCost);
                case EnemyType.Rusher:
                    return Math.Max(1, settings.EncounterRusherCost);
                case EnemyType.Turret:
                    return Math.Max(1, settings.EncounterTurretCost);
                case EnemyType.StaticShooter:
                    return Math.Max(1, settings.EncounterStaticShooterCost);
                case EnemyType.MobileGenerator:
                    return Math.Max(1, settings.EncounterMobileGeneratorCost);
                case EnemyType.Spawner:
                    return Math.Max(1, settings.EncounterSpawnerCost);
                default:
                    return Math.Max(1, settings.EncounterSwarmerCost);
            }
        }
    }
}