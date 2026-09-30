using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using TwinStickShooter.Core;
using TwinStickShooter.Entities;
using Xunit;

namespace TwinStickShooter.Tests
{
    public class MapGeneratorCrawlerTests
    {
        [Fact]
        public void DefaultConfiguration_UsesRequestedValuesAndGeneratesSampleLayout()
        {
            var settings = new MapGenerationSettings();
            Assert.Equal(1355728287, settings.Seed);
            Assert.Equal(23, settings.MinimumSpawnExitPathLength);
            Assert.Equal(400, settings.CrawlerMaxSteps);
            Assert.Equal(25, settings.CrawlerRoomInterval);
            Assert.Equal(40, settings.MaxArenaCount);
            Assert.Equal(2, settings.MinArenaCount);
            Assert.Equal(8f, settings.EncounterSpawnSafeDistance);
            Assert.Equal(18, settings.EncounterDifficultyBudget);
            Assert.Equal(3, settings.EncounterMinGroupSize);
            Assert.Equal(12, settings.EncounterMaxGroupSize);
            Assert.Equal(3f, settings.EncounterClusterRadius);
            Assert.Equal(1, settings.EncounterSwarmerCost);
            Assert.Equal(1, settings.EncounterRusherCost);
            Assert.Equal(1, settings.EncounterRoamerCost);
            Assert.Equal(3, settings.EncounterTurretCost);
            Assert.Equal(3, settings.EncounterStaticShooterCost);
            Assert.Equal(10, settings.EncounterMobileGeneratorCost);
            Assert.Equal(6, settings.EncounterSpawnerCost);
            Assert.Equal(27, settings.EncounterMaxInitialSpawnCount);
            Assert.Equal(5, settings.ArenaObstacleHitPoints);
            Assert.Equal(6, settings.ArenaObstacleMinSeeds);
            Assert.Equal(9, settings.ArenaObstacleMaxSeeds);

            var level = new LevelManager(GameConstants.GridWidth, GameConstants.GridHeight, GameConstants.GridCellSize);
            level.MapGenerator.Settings.Seed = settings.Seed;
            level.MapGenerator.Settings.MinimumSpawnExitPathLength = settings.MinimumSpawnExitPathLength;
            level.MapGenerator.Settings.CrawlerMaxSteps = settings.CrawlerMaxSteps;
            level.MapGenerator.Settings.CrawlerRoomInterval = settings.CrawlerRoomInterval;
            level.MapGenerator.Settings.MinArenaCount = settings.MinArenaCount;
            level.MapGenerator.Settings.MaxArenaCount = settings.MaxArenaCount;
            level.MapGenerator.Settings.EncounterDifficultyBudget = settings.EncounterDifficultyBudget;
            level.MapGenerator.Settings.EncounterMinGroupSize = settings.EncounterMinGroupSize;
            level.MapGenerator.Settings.EncounterMaxGroupSize = settings.EncounterMaxGroupSize;
            level.MapGenerator.Settings.EncounterSpawnSafeDistance = settings.EncounterSpawnSafeDistance;
            level.MapGenerator.Settings.EncounterClusterRadius = settings.EncounterClusterRadius;
            level.MapGenerator.Settings.EncounterSwarmerCost = settings.EncounterSwarmerCost;
            level.MapGenerator.Settings.EncounterRoamerCost = settings.EncounterRoamerCost;
            level.MapGenerator.Settings.EncounterTurretCost = settings.EncounterTurretCost;
            level.MapGenerator.Settings.EncounterSpawnerCost = settings.EncounterSpawnerCost;
            level.GenerateProceduralMap();

            MapGenerator generator = level.MapGenerator;
            int[,] grid = level.GetCollisionGridSnapshot();
            IReadOnlyList<EncounterSpawn> plan = EncounterDirector.Plan(
                grid,
                generator.Rooms,
                generator.SpawnPoint,
                GameConstants.GridCellSize,
                settings);
            var enemyManager = new EnemyManager(level);
            var spawnerManager = new SpawnerManager(GameConstants.MaxSpawners, enemyManager);
            int placedCount = 0;
            HashSet<int> placedClusters = new HashSet<int>();
            List<string> rejectedSpawns = new List<string>();
            foreach (EncounterSpawn spawn in plan)
            {
                Vector2 position = new Vector2(spawn.Position.X, spawn.Position.Y);
                bool placed = spawn.Type == EnemyType.Spawner
                    ? level.IsPlayableAndWalkable(position, GameConstants.SpawnerRadius) && spawnerManager.Register(position)
                    : enemyManager.Spawn(position, Vector2.Zero, spawn.Type);
                if (!placed)
                {
                    float radius = spawn.Type == EnemyType.Spawner ? GameConstants.SpawnerRadius : GameConstants.EnemyRadius;
                    rejectedSpawns.Add($"{spawn.Type}@{position}: playable={level.IsInPlayableArea(position)}, collision={level.CheckCollision(position, radius)}");
                    continue;
                }
                placedCount++;
                placedClusters.Add(spawn.ClusterId);
            }

            Assert.Equal(28, generator.SpawnExitPathLength);
            Assert.Equal(3, generator.Rooms.Count);
            Assert.Equal(398, generator.CrawlerStepCount);
            Assert.Equal(2, generator.ArenaRoomsPlaced);
            Assert.Equal(15, generator.PrefabPlacementAttempts);
            Assert.Equal(27, plan.Count);
            Assert.True(placedCount == 27, $"Placed {placedCount} of {plan.Count}; rejected: {string.Join(", ", rejectedSpawns)}");
            Assert.Equal(4, placedClusters.Count);
        }

        [Fact]
        public void GeneratedArenaObstacleClusterHasSolidCoreAndQuarterTileDestructibleCapsules()
        {
            var settings = new MapGenerationSettings
            {
                Seed = 514,
                CrawlerCount = 1,
                CrawlerMaxSteps = 120,
                CrawlerRoomInterval = 8,
                MinArenaCount = 1,
                MaxArenaCount = 1,
                MinimumSpawnExitPathLength = 8,
                ArenaRadiusMin = 3,
                ArenaRadiusMax = 3,
                ArenaObstacleMinSeeds = 5,
                ArenaObstacleMaxSeeds = 5,
                ArenaSolidCoreClusterChance = 1f,
                ArenaSolidObstacleChance = 0f,
                ArenaObstacleHitPoints = 5,
                EncounterDifficultyBudget = 0,
            };
            var generator = new MapGenerator(30, 30, settings);
            generator.GenerateMap();

            Assert.Equal(5, generator.ArenaObstacles.Count);
            Assert.Single(generator.ArenaObstacles, obstacle => !obstacle.IsDestructible);
            Assert.Equal(4, generator.ArenaObstacles.Count(obstacle => obstacle.IsDestructible));
            Assert.All(generator.ArenaObstacles, obstacle => Assert.Equal(0.125f, obstacle.CapsuleInTiles.Radius));
            Assert.All(generator.ArenaObstacles.Where(obstacle => obstacle.IsDestructible),
                obstacle => Assert.Equal(5, obstacle.HitPoints));

            Rectangle arena = generator.Rooms[1];
            Assert.All(generator.ArenaObstacles, obstacle =>
            {
                MapCapsule capsule = obstacle.CapsuleInTiles;
                Assert.True(capsule.Start.X - capsule.Radius >= arena.Left + 1f);
                Assert.True(capsule.Start.Y - capsule.Radius >= arena.Top + 1f);
                Assert.True(capsule.End.X + capsule.Radius <= arena.Right - 1f);
                Assert.True(capsule.End.Y + capsule.Radius <= arena.Bottom - 1f);
            });
        }

        [Theory]
        [InlineData(3)]
        [InlineData(4)]
        public void GenerateMap_MultipleCrawlersLeaveSpawnInDistinctDirections(int crawlerCount)
        {
            var generator = new MapGenerator(30, 30, new MapGenerationSettings
            {
                Seed = 813,
                CrawlerCount = crawlerCount,
                CrawlerMaxSteps = crawlerCount * 2,
                CrawlerRoomInterval = 50,
                MinPrefabRooms = 0,
                MaxPrefabRooms = 0,
                MinimumSpawnExitPathLength = 0,
            });

            int[,] map = generator.GenerateMap();
            Rectangle spawnRoom = generator.Rooms[0];
            Point center = spawnRoom.Center;
            int exitsOpened = 0;

            if (map[center.X, spawnRoom.Top - 1] == 0) exitsOpened++;
            if (map[spawnRoom.Right, center.Y] == 0) exitsOpened++;
            if (map[center.X, spawnRoom.Bottom] == 0) exitsOpened++;
            if (map[spawnRoom.Left - 1, center.Y] == 0) exitsOpened++;

            Assert.Equal(crawlerCount, exitsOpened);
            Assert.Equal(crawlerCount * 2, generator.CrawlerStepCount);
        }

        [Fact]
        public void GenerateMap_CrawlersCarveOneTileWideCorridorsByDefault()
        {
            var generator = new MapGenerator(30, 30, new MapGenerationSettings
            {
                Seed = 813,
                CrawlerCount = 4,
                CrawlerMaxSteps = 8,
                MaxPrefabRooms = 0,
                MinimumSpawnExitPathLength = 0,
            });

            int[,] map = generator.GenerateMap();
            Rectangle spawnRoom = generator.Rooms[0];
            int corridorFloorCells = 0;
            for (int x = 0; x < map.GetLength(0); x++)
            {
                for (int y = 0; y < map.GetLength(1); y++)
                {
                    if (map[x, y] == 0 && !spawnRoom.Contains(x, y)) corridorFloorCells++;
                }
            }

            Assert.Equal(1, generator.Settings.CrawlerCorridorWidth);
            Assert.Equal(6, corridorFloorCells);
        }

        [Fact]
        public void GenerateMap_WithTwoArenaLimitCreatesLargeGeneratedArenas()
        {
            var generator = new MapGenerator(30, 30, new MapGenerationSettings
            {
                Seed = 117,
                CrawlerCount = 4,
                CrawlerMaxSteps = 650,
                CrawlerRoomInterval = 10,
                MinPrefabRooms = 2,
                MaxPrefabRooms = 2,
                MinimumSpawnExitPathLength = 0,
            });
            generator.GenerateMap();

            Assert.Equal(2, generator.ArenaRoomsPlaced);
            Assert.True(generator.CrawlerStepCount < generator.Settings.CrawlerMaxSteps);
            Assert.Contains(generator.Rooms.Skip(1), room => room.Width >= 9 && room.Height >= 9);
        }

        [Fact]
        public void GenerateMap_CrawlerPeriodicallyExpandsConnectedArenasDeterministically()
        {
            var settings = new MapGenerationSettings
            {
                Seed = 7319,
                CrawlerMaxSteps = 180,
                CrawlerRoomInterval = 8,
                MinPrefabRooms = 2,
                MaxPrefabRooms = 4,
                MinimumSpawnExitPathLength = 18,
            };
            var first = new MapGenerator(40, 40, settings);
            var second = new MapGenerator(40, 40, new MapGenerationSettings
            {
                Seed = 7319,
                CrawlerMaxSteps = 180,
                CrawlerRoomInterval = 8,
                MinPrefabRooms = 2,
                MaxPrefabRooms = 4,
                MinimumSpawnExitPathLength = 18,
            });

            int[,] firstMap = first.GenerateMap();
            int[,] secondMap = second.GenerateMap();

            AssertMapsEqual(firstMap, secondMap);
            Assert.Equal(first.RoomConnections, second.RoomConnections);
            Assert.True(first.ArenaRoomsPlaced > 0);
            Assert.Equal(first.ArenaRoomsPlaced + 1, first.Rooms.Count);
            Assert.Equal(first.ArenaRoomsPlaced, first.RoomConnections.Count);
            Assert.True(first.PrefabPlacementAttempts >= first.ArenaRoomsPlaced);

            foreach (Rectangle room in first.Rooms)
            {
                bool foundReachableFloor = false;
                for (int x = room.Left; x < room.Right && !foundReachableFloor; x++)
                {
                    for (int y = room.Top; y < room.Bottom; y++)
                    {
                        if (firstMap[x, y] == 0 && first.IsMapTraversable(firstMap, first.SpawnPoint, new Point(x, y)))
                        {
                            foundReachableFloor = true;
                            break;
                        }
                    }
                }

                Assert.True(foundReachableFloor);
            }

            Assert.True(CountZones(first.ZoneMap, MapZoneType.Arena) > 0);
            Assert.True(CountZones(first.ZoneMap, MapZoneType.ChokePoint) > 0);
            AssertRoomGraphIsConnected(first.Rooms.Count, first.RoomConnections);
        }

        [Fact]
        public void GenerateMap_ArenaExpansionDoesNotDependOnRoomTemplates()
        {
            var generator = new MapGenerator(30, 30, new MapGenerationSettings
            {
                Seed = 514,
                CrawlerMaxSteps = 80,
                CrawlerRoomInterval = 5,
                CrawlerCount = 1,
                MinArenaCount = 1,
                MaxArenaCount = 1,
                MinimumSpawnExitPathLength = 12,
            });
            generator.SetRoomTemplates(new List<RoomTemplateData>
            {
                new RoomTemplateData
                {
                    Id = "too_large",
                    Grid = Enumerable.Repeat(new string('0', 19), 19).ToArray(),
                }
            });
            int[,] map = generator.GenerateMap();

            Assert.Equal(1, generator.ArenaRoomsPlaced);
            Assert.Equal(2, generator.Rooms.Count);
            Assert.True(generator.SpawnExitPathLength >= generator.Settings.MinimumSpawnExitPathLength);
            Assert.True(generator.IsMapTraversable(map, generator.SpawnPoint, generator.ExitPoint));
        }

        [Fact]
        public void GenerateMap_EncounterDirectorPlacesEnemiesInsideGeneratedArena()
        {
            var settings = new MapGenerationSettings
            {
                Seed = 514,
                CrawlerCount = 1,
                CrawlerMaxSteps = 120,
                CrawlerRoomInterval = 8,
                MinArenaCount = 1,
                MaxArenaCount = 1,
                MinimumSpawnExitPathLength = 8,
                EncounterDifficultyBudget = 24,
                EncounterMinGroupSize = 1,
                EncounterMaxGroupSize = 4,
                EncounterSpawnSafeDistance = 0,
                EncounterClusterRadius = 8f,
                ArenaObstacleMinSeeds = 0,
                ArenaObstacleMaxSeeds = 0,
            };
            var generator = new MapGenerator(30, 30, settings);
            int[,] grid = generator.GenerateMap();

            IReadOnlyList<EncounterSpawn> plan = EncounterDirector.Plan(
                grid,
                generator.Rooms,
                generator.SpawnPoint,
                GameConstants.GridCellSize,
                settings);

            Assert.NotEmpty(plan);
            Assert.Contains(plan, spawn => spawn.RegionIndex == 1);
            Assert.All(plan, spawn =>
            {
                Point cell = new Point(spawn.Position.X / GameConstants.GridCellSize, spawn.Position.Y / GameConstants.GridCellSize);
                Assert.Equal(0, grid[cell.X, cell.Y]);
                if (spawn.RegionIndex == 1)
                    Assert.True(generator.Rooms[spawn.RegionIndex].Contains(cell));
            });
        }

        [Fact]
        public void GenerateMap_WithZeroPrefabMaximumProducesOnlySpawnRoomAndCrawlerTunnels()
        {
            var generator = new MapGenerator(30, 30, new MapGenerationSettings
            {
                Seed = 631,
                CrawlerMaxSteps = 650,
                MinPrefabRooms = 4,
                MaxPrefabRooms = 0,
                MinimumSpawnExitPathLength = 15,
            });
            int[,] map = generator.GenerateMap();

            Assert.Single(generator.Rooms);
            Assert.Equal(0, generator.PrefabRoomsPlaced);
            Assert.Equal(0, generator.PrefabPlacementAttempts);
            Assert.True(generator.IsMapTraversable(map, generator.SpawnPoint, generator.ExitPoint));
        }

        [Theory]
        [InlineData(17)]
        [InlineData(203)]
        [InlineData(4501)]
        [InlineData(9991)]
        public void GenerateMap_RetriesSeedsUntilMinimumPrefabCountIsMet(int seed)
        {
            var settings = new MapGenerationSettings
            {
                Seed = seed,
                CrawlerMaxSteps = 650,
                CrawlerRoomInterval = 35,
                MinPrefabRooms = 2,
                MaxPrefabRooms = 8,
            };
            var generator = new MapGenerator(30, 30, settings);

            int[,] map = generator.GenerateMap();

            Assert.True(generator.PrefabRoomsPlaced >= settings.MinPrefabRooms);
            Assert.True(generator.IsMapTraversable(map, generator.SpawnPoint, generator.ExitPoint));
        }

        private static List<RoomTemplateData> CreateTemplates()
        {
            return new List<RoomTemplateData>
            {
                new RoomTemplateData
                {
                    Id = "test_room",
                    Grid = new[]
                    {
                        "11111",
                        "10001",
                        "10001",
                        "10001",
                        "11111",
                    },
                },
            };
        }

        private static int CountZones(MapZoneType[,] zones, MapZoneType expected)
        {
            int count = 0;
            for (int x = 0; x < zones.GetLength(0); x++)
            {
                for (int y = 0; y < zones.GetLength(1); y++)
                {
                    if (zones[x, y] == expected) count++;
                }
            }

            return count;
        }

        private static void AssertRoomGraphIsConnected(int roomCount, List<(int From, int To)> connections)
        {
            var visited = new HashSet<int> { 0 };
            var pending = new Queue<int>();
            pending.Enqueue(0);

            while (pending.Count > 0)
            {
                int current = pending.Dequeue();
                foreach ((int from, int to) in connections)
                {
                    int neighbor = from == current ? to : to == current ? from : -1;
                    if (neighbor >= 0 && visited.Add(neighbor))
                    {
                        pending.Enqueue(neighbor);
                    }
                }
            }

            Assert.Equal(roomCount, visited.Count);
        }

        private static void AssertMapsEqual(int[,] expected, int[,] actual)
        {
            Assert.Equal(expected.GetLength(0), actual.GetLength(0));
            Assert.Equal(expected.GetLength(1), actual.GetLength(1));
            for (int x = 0; x < expected.GetLength(0); x++)
            {
                for (int y = 0; y < expected.GetLength(1); y++)
                {
                    Assert.Equal(expected[x, y], actual[x, y]);
                }
            }
        }
    }
}