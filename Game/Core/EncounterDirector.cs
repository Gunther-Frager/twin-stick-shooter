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

            List<EncounterSpawn> plan = new List<EncounterSpawn>();
            int remainingSlots = GameConstants.MaxEnemies;
            int maxGroupSize = Math.Max(1, settings.EncounterMaxGroupSize);
            int minGroupSize = Math.Min(Math.Max(1, settings.EncounterMinGroupSize), maxGroupSize);
            float clusterRadius = Math.Max(0f, settings.EncounterClusterRadius);
            float safeDistance = Math.Max(0f, settings.EncounterSpawnSafeDistance);
            double safeDistanceSquared = (double)safeDistance * safeDistance;
            double clusterRadiusSquared = (double)clusterRadius * clusterRadius;
            Random random = new Random(settings.Seed);
            int nextClusterId = 0;
            List<EncounterRegion> eligibleRegions = new List<EncounterRegion>();

            for (int roomIndex = 1; roomIndex < rooms.Count; roomIndex++)
            {
                Rectangle room = rooms[roomIndex];

                EncounterRegion roomRegion = new EncounterRegion
                {
                    RegionIndex = roomIndex,
                    Budget = Math.Max(0, settings.EncounterDifficultyBudget)
                };
                for (int x = room.Left + 1; x < room.Right - 1; x++)
                {
                    for (int y = room.Top + 1; y < room.Bottom - 1; y++)
                    {
                        if (x < 0 || x >= grid.GetLength(0) || y < 0 || y >= grid.GetLength(1) || grid[x, y] != 0)
                        {
                            continue;
                        }

                        long dx = (long)x - spawnPoint.X;
                        long dy = (long)y - spawnPoint.Y;
                        if (dx * dx + dy * dy < safeDistanceSquared)
                        {
                            continue;
                        }

                        roomRegion.Available.Add(new Point(x, y));
                    }
                }

                if (roomRegion.Available.Count > 0)
                {
                    eligibleRegions.Add(roomRegion);
                }
            }

            EncounterRegion corridorRegion = new EncounterRegion
            {
                RegionIndex = rooms.Count,
                Budget = Math.Max(0, settings.EncounterDifficultyBudget)
            };
            for (int x = 0; x < grid.GetLength(0); x++)
            {
                for (int y = 0; y < grid.GetLength(1); y++)
                {
                    if (grid[x, y] != 0 || IsCellInsideAnyRoom(x, y, rooms)) continue;
                    long dx = (long)x - spawnPoint.X;
                    long dy = (long)y - spawnPoint.Y;
                    if (dx * dx + dy * dy >= safeDistanceSquared)
                    {
                        corridorRegion.Available.Add(new Point(x, y));
                    }
                }
            }
            if (corridorRegion.Available.Count > 0) eligibleRegions.Add(corridorRegion);

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

        private static bool IsCellInsideAnyRoom(int x, int y, IList<Rectangle> rooms)
        {
            Point cell = new Point(x, y);
            for (int i = 0; i < rooms.Count; i++)
            {
                if (rooms[i].Contains(cell)) return true;
            }

            return false;
        }

        private static List<EnemyType> GetAffordableTypes(MapGenerationSettings settings, int budget)
        {
            List<EnemyType> types = new List<EnemyType>();
            if (Math.Max(1, settings.EncounterSwarmerCost) <= budget) types.Add(EnemyType.Swarmer);
            if (Math.Max(1, settings.EncounterRoamerCost) <= budget) types.Add(EnemyType.Roamer);
            if (Math.Max(1, settings.EncounterTurretCost) <= budget) types.Add(EnemyType.Turret);
            if (Math.Max(1, settings.EncounterSpawnerCost) <= budget) types.Add(EnemyType.Spawner);
            return types;
        }

        private static int GetMinimumCost(MapGenerationSettings settings)
        {
            return Math.Min(
                Math.Min(Math.Max(1, settings.EncounterSwarmerCost), Math.Max(1, settings.EncounterRoamerCost)),
                Math.Min(Math.Max(1, settings.EncounterTurretCost), Math.Max(1, settings.EncounterSpawnerCost)));
        }

        private static int GetCost(EnemyType type, MapGenerationSettings settings)
        {
            switch (type)
            {
                case EnemyType.Roamer:
                    return Math.Max(1, settings.EncounterRoamerCost);
                case EnemyType.Turret:
                    return Math.Max(1, settings.EncounterTurretCost);
                case EnemyType.Spawner:
                    return Math.Max(1, settings.EncounterSpawnerCost);
                default:
                    return Math.Max(1, settings.EncounterSwarmerCost);
            }
        }
    }
}