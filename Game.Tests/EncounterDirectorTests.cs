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
                CreateFloorGrid(64, 64), CreateRooms(), new Point(5, 5), 10, null, settings);

            Assert.NotEmpty(plan);
            Assert.All(plan.GroupBy(spawn => spawn.RoomIndex), roomPlan =>
                Assert.InRange(roomPlan.Sum(spawn => GetCost(spawn.Type, settings)), 1, settings.EncounterDifficultyBudget));
            Assert.Equal(plan.Count, plan.Select(spawn => spawn.Position).Distinct().Count());
            Assert.DoesNotContain(plan, spawn => spawn.Type == EnemyType.Spawner);
            Assert.All(plan, spawn => Assert.Equal(0, CreateFloorGrid(64, 64)[spawn.Position.X / 10, spawn.Position.Y / 10]));
        }

        [Fact]
        public void Plan_ProtectsSpawnRoomSafeRadiusAndRoomsWithTemplateSpawns()
        {
            var rooms = CreateRooms();
            var templateSpawns = new List<RoomTemplateData.EnemySpawn>
            {
                new RoomTemplateData.EnemySpawn(new Point(305, 305), EnemyType.Turret),
            };
            var settings = new MapGenerationSettings
            {
                Seed = 12,
                EncounterDifficultyBudget = 100,
                EncounterSpawnSafeDistance = 12,
                EncounterClusterRadius = 2,
            };

            IReadOnlyList<EncounterSpawn> plan = EncounterDirector.Plan(
                CreateFloorGrid(64, 64), rooms, new Point(5, 5), 10, templateSpawns, settings);

            Assert.NotEmpty(plan);
            Assert.All(plan, spawn =>
            {
                Assert.NotEqual(0, spawn.RoomIndex);
                Assert.NotEqual(2, spawn.RoomIndex);
                Assert.NotEqual(4, spawn.RoomIndex);
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

            IReadOnlyList<EncounterSpawn> first = EncounterDirector.Plan(CreateFloorGrid(64, 64), CreateRooms(), new Point(5, 5), 10, null, settings);
            IReadOnlyList<EncounterSpawn> second = EncounterDirector.Plan(CreateFloorGrid(64, 64), CreateRooms(), new Point(5, 5), 10, null, settings);

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
                CreateFloorGrid(64, 64), CreateRooms(), new Point(5, 5), 10, null, settings);

            Assert.Empty(plan);
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
                CreateFloorGrid(64, 64), CreateRooms(), new Point(5, 5), 10, null, settings);

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
                CreateFloorGrid(64, 64), CreateRooms(), new Point(5, 5), 10, null, settings);

            Assert.Empty(plan);
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

        private static int GetCost(EnemyType type, MapGenerationSettings settings)
        {
            switch (type)
            {
                case EnemyType.Roamer:
                    return settings.EncounterRoamerCost;
                case EnemyType.Turret:
                    return settings.EncounterTurretCost;
                default:
                    return settings.EncounterSwarmerCost;
            }
        }

        private static string ToValue(EncounterSpawn spawn)
        {
            return $"{spawn.RoomIndex}:{spawn.Position.X}:{spawn.Position.Y}:{spawn.Type}";
        }
    }
}