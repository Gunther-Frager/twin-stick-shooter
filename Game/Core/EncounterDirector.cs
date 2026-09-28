using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace TwinStickShooter.Core
{
    public sealed class EncounterSpawn
    {
        public EncounterSpawn(int roomIndex, int clusterId, Point position, EnemyType type)
        {
            RoomIndex = roomIndex;
            ClusterId = clusterId;
            Position = position;
            Type = type;
        }

        public int RoomIndex { get; }
        public int ClusterId { get; }
        public Point Position { get; }
        public EnemyType Type { get; }
    }

    /// <summary>
    /// Creates deterministic room encounter plans without creating game entities.
    /// </summary>
    public static class EncounterDirector
    {
        private sealed class RoomCells
        {
            public int RoomIndex;
            public int Budget;
            public List<Point> Available = new List<Point>();
        }

        public static IReadOnlyList<EncounterSpawn> Plan(
            int[,] grid,
            IList<Rectangle> rooms,
            Point spawnPoint,
            int cellSize,
            IList<RoomTemplateData.EnemySpawn> templateSpawns,
            MapGenerationSettings settings)
        {
            if (grid == null) throw new ArgumentNullException(nameof(grid));
            if (rooms == null) throw new ArgumentNullException(nameof(rooms));
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            if (cellSize <= 0) throw new ArgumentOutOfRangeException(nameof(cellSize));

            List<EncounterSpawn> plan = new List<EncounterSpawn>();
            int remainingSlots = GameConstants.MaxEnemies;
            if (templateSpawns != null)
            {
                for (int i = 0; i < templateSpawns.Count; i++)
                {
                    if (templateSpawns[i].Type != EnemyType.Spawner) remainingSlots--;
                }
            }
            int maxGroupSize = Math.Max(1, settings.EncounterMaxGroupSize);
            int minGroupSize = Math.Min(Math.Max(1, settings.EncounterMinGroupSize), maxGroupSize);
            int clusterRadius = Math.Max(0, settings.EncounterClusterRadius);
            int safeDistance = Math.Max(0, settings.EncounterSpawnSafeDistance);
            long safeDistanceSquared = (long)safeDistance * safeDistance;
            long clusterRadiusSquared = (long)clusterRadius * clusterRadius;
            Random random = new Random(settings.Seed);
            int nextClusterId = 0;
            List<RoomCells> eligibleRooms = new List<RoomCells>();

            for (int roomIndex = 1; roomIndex < rooms.Count; roomIndex++)
            {
                Rectangle room = rooms[roomIndex];
                if (HasTemplateSpawn(room, templateSpawns, cellSize)) continue;

                RoomCells roomCells = new RoomCells
                {
                    RoomIndex = roomIndex,
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
                        if (dx * dx + dy * dy >= safeDistanceSquared)
                        {
                            roomCells.Available.Add(new Point(x, y));
                        }
                    }
                }

                if (roomCells.Available.Count > 0)
                {
                    eligibleRooms.Add(roomCells);
                }
            }

            while (remainingSlots > 0 && eligibleRooms.Count > 0)
            {
                RoomCells room = eligibleRooms[random.Next(eligibleRooms.Count)];
                List<EnemyType> affordableForRoom = GetAffordableTypes(settings, room.Budget);
                if (affordableForRoom.Count == 0)
                {
                    eligibleRooms.Remove(room);
                    continue;
                }

                Point anchor = room.Available[random.Next(room.Available.Count)];
                List<Point> cluster = new List<Point>();
                for (int i = 0; i < room.Available.Count; i++)
                {
                    Point candidate = room.Available[i];
                    long dx = (long)candidate.X - anchor.X;
                    long dy = (long)candidate.Y - anchor.Y;
                    if (dx * dx + dy * dy <= clusterRadiusSquared)
                    {
                        cluster.Add(candidate);
                    }
                }

                int minimumEnemyCost = GetMinimumCost(settings);
                int affordableGroupSize = room.Budget / minimumEnemyCost;
                int resourceLimitedGroupSize = Math.Min(remainingSlots, affordableGroupSize);
                if (resourceLimitedGroupSize < minGroupSize)
                {
                    eligibleRooms.Remove(room);
                    continue;
                }

                int maxAvailableGroupSize = Math.Min(
                    Math.Min(maxGroupSize, cluster.Count),
                    resourceLimitedGroupSize);
                if (maxAvailableGroupSize < minGroupSize)
                {
                    room.Available.Remove(anchor);
                    if (room.Available.Count == 0) eligibleRooms.Remove(room);
                    continue;
                }
                int groupSize = random.Next(minGroupSize, maxAvailableGroupSize + 1);
                int clusterId = nextClusterId++;
                for (int i = 0; i < groupSize; i++)
                {
                    int reservedBudget = minimumEnemyCost * (groupSize - i - 1);
                    List<EnemyType> affordableTypes = GetAffordableTypes(settings, room.Budget - reservedBudget);
                    int clusterIndex = random.Next(cluster.Count);
                    Point cell = cluster[clusterIndex];
                    cluster.RemoveAt(clusterIndex);
                    for (int roomIndex = 0; roomIndex < eligibleRooms.Count; roomIndex++)
                    {
                        eligibleRooms[roomIndex].Available.Remove(cell);
                    }

                    EnemyType type = affordableTypes[random.Next(affordableTypes.Count)];
                    room.Budget -= GetCost(type, settings);
                    remainingSlots--;
                    Point worldPosition = new Point(
                        cell.X * cellSize + cellSize / 2,
                        cell.Y * cellSize + cellSize / 2);
                    plan.Add(new EncounterSpawn(room.RoomIndex, clusterId, worldPosition, type));
                }

                if (room.Budget > 0 && room.Available.Count > 0 && GetAffordableTypes(settings, room.Budget).Count > 0)
                {
                    continue;
                }

                for (int i = eligibleRooms.Count - 1; i >= 0; i--)
                {
                    if (eligibleRooms[i].Available.Count == 0 || eligibleRooms[i] == room)
                    {
                        eligibleRooms.RemoveAt(i);
                    }
                }
            }

            return plan.AsReadOnly();
        }

        private static bool HasTemplateSpawn(
            Rectangle room,
            IList<RoomTemplateData.EnemySpawn> templateSpawns,
            int cellSize)
        {
            if (templateSpawns == null) return false;

            for (int i = 0; i < templateSpawns.Count; i++)
            {
                Point worldPosition = templateSpawns[i].Position;
                Point cell = new Point(
                    (int)Math.Floor((double)worldPosition.X / cellSize),
                    (int)Math.Floor((double)worldPosition.Y / cellSize));
                if (room.Contains(cell)) return true;
            }

            return false;
        }

        private static List<EnemyType> GetAffordableTypes(MapGenerationSettings settings, int budget)
        {
            List<EnemyType> types = new List<EnemyType>();
            if (Math.Max(1, settings.EncounterSwarmerCost) <= budget) types.Add(EnemyType.Swarmer);
            if (Math.Max(1, settings.EncounterRoamerCost) <= budget) types.Add(EnemyType.Roamer);
            if (Math.Max(1, settings.EncounterTurretCost) <= budget) types.Add(EnemyType.Turret);
            return types;
        }

        private static int GetMinimumCost(MapGenerationSettings settings)
        {
            return Math.Min(
                Math.Max(1, settings.EncounterSwarmerCost),
                Math.Min(Math.Max(1, settings.EncounterRoamerCost), Math.Max(1, settings.EncounterTurretCost)));
        }

        private static int GetCost(EnemyType type, MapGenerationSettings settings)
        {
            switch (type)
            {
                case EnemyType.Roamer:
                    return Math.Max(1, settings.EncounterRoamerCost);
                case EnemyType.Turret:
                    return Math.Max(1, settings.EncounterTurretCost);
                default:
                    return Math.Max(1, settings.EncounterSwarmerCost);
            }
        }
    }
}