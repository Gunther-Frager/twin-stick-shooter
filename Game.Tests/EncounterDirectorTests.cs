using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using TwinStickShooter.Core;
using Xunit;

namespace TwinStickShooter.Tests
{
    public class EncounterDirectorTests
    {
        [Fact]
        public void Plan_RespectsDifficultyBudgetAndProducesUniquePositions()
        {
            var settings = new MapGenerationSettings
            {
                Seed = 71,
                EncounterDifficultyBudget = 17,
                EncounterSwarmerCost = 1,
                EncounterRoamerCost = 3,
                EncounterTurretCost = 5,
                EncounterMaxGroupSize = 4,
                EncounterClusterRadius = 3,
                EncounterSpawnSafeDistance = 0,
            };

            IReadOnlyList<EncounterSpawn> plan = EncounterDirector.Plan(
                CreateFloorGrid(64, 64), CreateRooms(), new Point(5, 5), 10, settings);

            Assert.NotEmpty(plan);
            Assert.All(plan.GroupBy(spawn => spawn.RegionIndex), roomPlan =>
                Assert.InRange(roomPlan.Sum(spawn => GetCost(spawn.Type, settings)), 1, settings.EncounterDifficultyBudget));
            Assert.Equal(plan.Count, plan.Select(spawn => spawn.Position).Distinct().Count());
            Assert.Contains(plan, spawn => spawn.Type == EnemyType.Spawner);
            Assert.All(plan, spawn => Assert.Equal(0, CreateFloorGrid(64, 64)[spawn.Position.X / 10, spawn.Position.Y / 10]));
        }

        [Fact]
        public void Plan_ProtectsSpawnRoomAndSafeRadius()
        {
            var rooms = CreateRooms();
            var settings = new MapGenerationSettings
            {
                Seed = 12,
                EncounterDifficultyBudget = 100,
                EncounterSpawnSafeDistance = 12,
                EncounterClusterRadius = 2,
                EncounterMinGroupSize = 1,
            };

            IReadOnlyList<EncounterSpawn> plan = EncounterDirector.Plan(
                CreateFloorGrid(64, 64), rooms, new Point(5, 5), 10, settings);

            Assert.NotEmpty(plan);
            Assert.All(plan, spawn =>
            {
                Assert.NotEqual(0, spawn.RegionIndex);
                int cellX = spawn.Position.X / 10;
                int cellY = spawn.Position.Y / 10;
                int dx = cellX - 5;
                int dy = cellY - 5;
                Assert.True(dx * dx + dy * dy >= settings.EncounterSpawnSafeDistance * settings.EncounterSpawnSafeDistance);
            });
        }

        [Fact]
        public void Plan_WithSameSeedProducesSameEncounterPlan()
        {
            var settings = new MapGenerationSettings { Seed = 991, EncounterSpawnSafeDistance = 0 };

            IReadOnlyList<EncounterSpawn> first = EncounterDirector.Plan(CreateFloorGrid(64, 64), CreateRooms(), new Point(5, 5), 10, settings);
            IReadOnlyList<EncounterSpawn> second = EncounterDirector.Plan(CreateFloorGrid(64, 64), CreateRooms(), new Point(5, 5), 10, settings);

            Assert.Equal(first.Select(ToValue).ToArray(), second.Select(ToValue).ToArray());
        }

        [Fact]
        public void Plan_WhenBudgetCannotPayForAnyEnemyReturnsEmptyPlan()
        {
            var settings = new MapGenerationSettings
            {
                EncounterDifficultyBudget = 2,
                EncounterSwarmerCost = 3,
                EncounterRoamerCost = 4,
                EncounterTurretCost = 5,
            };

            IReadOnlyList<EncounterSpawn> plan = EncounterDirector.Plan(
                CreateFloorGrid(64, 64), CreateRooms(), new Point(5, 5), 10, settings);

            Assert.Empty(plan);
        }

        [Fact]
        public void Plan_CanIncludeSpawnerWhenItsCostFitsTheRegionBudget()
        {
            var settings = new MapGenerationSettings
            {
                Seed = 18,
                EncounterDifficultyBudget = 100,
                EncounterMinGroupSize = 1,
                EncounterMaxGroupSize = 1,
                EncounterSwarmerCost = 1,
                EncounterRoamerCost = 1,
                EncounterTurretCost = 1,
                EncounterSpawnerCost = 1,
                EncounterClusterRadius = 12f,
                EncounterSpawnSafeDistance = 0f,
            };

            IReadOnlyList<EncounterSpawn> plan = EncounterDirector.Plan(
                CreateFloorGrid(64, 64), CreateRooms(), new Point(5, 5), 10, settings);

            Assert.Contains(plan, spawn => spawn.Type == EnemyType.Spawner);
        }

        [Fact]
        public void Plan_HonorsMinimumAndMaximumGroupSizesWhenSpaceAndBudgetAllow()
        {
            var settings = new MapGenerationSettings
            {
                Seed = 234,
                EncounterDifficultyBudget = 12,
                EncounterSwarmerCost = 1,
                EncounterRoamerCost = 1,
                EncounterTurretCost = 1,
                EncounterMinGroupSize = 4,
                EncounterMaxGroupSize = 6,
                EncounterClusterRadius = 10,
                EncounterSpawnSafeDistance = 0,
            };

            IReadOnlyList<EncounterSpawn> plan = EncounterDirector.Plan(
                CreateFloorGrid(64, 64), CreateRooms(), new Point(5, 5), 10, settings);

            Assert.NotEmpty(plan);
            Assert.All(plan.GroupBy(spawn => spawn.ClusterId), cluster =>
                Assert.InRange(cluster.Count(), settings.EncounterMinGroupSize, settings.EncounterMaxGroupSize));
        }

        [Fact]
        public void Plan_DoesNotCreateUndersizedGroupsWhenClusterRadiusIsTooSmall()
        {
            var settings = new MapGenerationSettings
            {
                EncounterDifficultyBudget = 100,
                EncounterMinGroupSize = 3,
                EncounterMaxGroupSize = 6,
                EncounterClusterRadius = 0,
                EncounterSpawnSafeDistance = 0,
            };

            IReadOnlyList<EncounterSpawn> plan = EncounterDirector.Plan(
                CreateFloorGrid(64, 64), CreateRooms(), new Point(5, 5), 10, settings);

            Assert.Empty(plan);
        }

        [Fact]
        public void Plan_BudgetChangesSpawnCountWithoutKnowingTerrainSource()
        {
            var lowBudgetSettings = new MapGenerationSettings
            {
                Seed = 182,
                EncounterDifficultyBudget = 1,
                EncounterMinGroupSize = 1,
                EncounterMaxGroupSize = 1,
                EncounterSwarmerCost = 1,
                EncounterRoamerCost = 1,
                EncounterTurretCost = 1,
                EncounterClusterRadius = 12,
                EncounterSpawnSafeDistance = 0,
            };
            var highBudgetSettings = new MapGenerationSettings
            {
                Seed = lowBudgetSettings.Seed,
                EncounterDifficultyBudget = 8,
                EncounterMinGroupSize = 1,
                EncounterMaxGroupSize = 1,
                EncounterSwarmerCost = 1,
                EncounterRoamerCost = 1,
                EncounterTurretCost = 1,
                EncounterClusterRadius = 12,
                EncounterSpawnSafeDistance = 0,
            };

            IReadOnlyList<EncounterSpawn> lowBudgetPlan = EncounterDirector.Plan(
                CreateFloorGrid(64, 64), CreateRooms(), new Point(5, 5), 10, lowBudgetSettings);
            IReadOnlyList<EncounterSpawn> highBudgetPlan = EncounterDirector.Plan(
                CreateFloorGrid(64, 64), CreateRooms(), new Point(5, 5), 10, highBudgetSettings);

            Assert.True(highBudgetPlan.Count > lowBudgetPlan.Count);
        }

        [Fact]
        public void Plan_SpawnsBudgetedGroupsInCrawlerCorridorsWithoutPrefabRooms()
        {
            int[,] grid = CreateWallGrid(40, 40);
            Rectangle spawnRoom = new Rectangle(2, 2, 4, 4);
            for (int x = spawnRoom.Left; x < spawnRoom.Right; x++)
            {
                for (int y = spawnRoom.Top; y < spawnRoom.Bottom; y++) grid[x, y] = 0;
            }
            for (int x = 5; x <= 15; x++) grid[x, 4] = 0;
            for (int y = 4; y <= 15; y++) grid[15, y] = 0;
            for (int x = 14; x <= 17; x++)
            {
                for (int y = 13; y <= 17; y++) grid[x, y] = 0;
            }

            var settings = new MapGenerationSettings
            {
                Seed = 25,
                EncounterDifficultyBudget = 8,
                EncounterSwarmerCost = 1,
                EncounterRoamerCost = 1,
                EncounterTurretCost = 1,
                EncounterMinGroupSize = 3,
                EncounterMaxGroupSize = 4,
                EncounterClusterRadius = 3,
                EncounterSpawnSafeDistance = 0,
            };

            IReadOnlyList<EncounterSpawn> plan = EncounterDirector.Plan(
                grid, new[] { spawnRoom }, new Point(3, 3), 10, settings);

            Assert.NotEmpty(plan);
            Assert.All(plan, spawn => Assert.Equal(1, spawn.RegionIndex));
            Assert.All(plan.GroupBy(spawn => spawn.ClusterId), group => Assert.InRange(group.Count(), 3, 4));
        }

        private static Rectangle[] CreateRooms()
        {
            return new[]
            {
                new Rectangle(2, 2, 8, 8),
                new Rectangle(20, 20, 12, 12),
                new Rectangle(30, 30, 12, 12),
                new Rectangle(42, 20, 12, 12),
                new Rectangle(10, 3, 6, 6),
            };
        }

        private static int[,] CreateFloorGrid(int width, int height)
        {
            return new int[width, height];
        }

        private static int[,] CreateWallGrid(int width, int height)
        {
            int[,] grid = new int[width, height];
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++) grid[x, y] = 1;
            }

            return grid;
        }

        private static int GetCost(EnemyType type, MapGenerationSettings settings)
        {
            switch (type)
            {
                case EnemyType.Roamer:
                    return settings.EncounterRoamerCost;
                case EnemyType.Turret:
                    return settings.EncounterTurretCost;
                case EnemyType.Spawner:
                    return settings.EncounterSpawnerCost;
                default:
                    return settings.EncounterSwarmerCost;
            }
        }

        private static string ToValue(EncounterSpawn spawn)
        {
            return $"{spawn.RegionIndex}:{spawn.Position.X}:{spawn.Position.Y}:{spawn.Type}";
        }
    }
}