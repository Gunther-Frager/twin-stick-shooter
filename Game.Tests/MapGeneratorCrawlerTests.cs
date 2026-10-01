using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using TwinStickShooter.Core;
using Xunit;

namespace TwinStickShooter.Tests
{
    public class MapGeneratorCrawlerTests
    {
        [Fact]
        public void DefaultConfiguration_PlansEncountersWithinStaticRegionBudgets()
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
            Assert.Equal(1024, settings.EnemyPoolCapacity);
            Assert.Equal(3, settings.EncounterMinGroupSize);
            Assert.Equal(12, settings.EncounterMaxGroupSize);
            Assert.Equal(3f, settings.EncounterClusterRadius);
            Assert.Equal(1, settings.EncounterSwarmerCost);
            Assert.Equal(1024, settings.EnemyPoolCapacity);
            Assert.Equal(1, settings.EncounterRusherCost);
            Assert.Equal(1, settings.EncounterRoamerCost);
            Assert.Equal(3, settings.EncounterTurretCost);
            Assert.Equal(3, settings.EncounterStaticShooterCost);
            Assert.Equal(10, settings.EncounterMobileGeneratorCost);
            Assert.Equal(6, settings.EncounterSpawnerCost);
            Assert.Equal(2, settings.EncounterBudgetPerDepth);
            Assert.Equal(6, settings.EncounterPocketDifficultyBudget);
            Assert.Equal(5, settings.ArenaObstacleHitPoints);
            Assert.Equal(6, settings.ArenaObstacleMinSeeds);
            Assert.Equal(9, settings.ArenaObstacleMaxSeeds);

            var generator = new MapGenerator(GameConstants.GridWidth, GameConstants.GridHeight, settings);
            var grid = generator.GenerateMap();
            IReadOnlyList<EncounterSpawn> plan = EncounterDirector.Plan(
                grid,
                generator.RegionIdMap,
                generator.Regions,
                generator.SpawnPoint,
                GameConstants.GridCellSize,
                settings);

            Assert.True(generator.SpawnExitPathLength >= settings.MinimumSpawnExitPathLength);
            Assert.Equal(1 + generator.ArenaRoomsPlaced + generator.PocketRoomsPlaced, generator.Rooms.Count);
            Assert.InRange(generator.CrawlerStepCount, 1, settings.CrawlerMaxSteps);
            Assert.Equal(2, generator.ArenaRoomsPlaced);
            Assert.True(generator.PrefabPlacementAttempts >= generator.ArenaRoomsPlaced);
            Assert.NotEmpty(plan);
            Assert.InRange(plan.Count, 1, GameConstants.MaxEnemies);
            Assert.All(plan.GroupBy(spawn => spawn.RegionIndex), regionPlan =>
            {
                MapRegionDefinition region = generator.Regions[regionPlan.Key];
                Assert.InRange(regionPlan.Sum(spawn => GetEncounterCost(spawn.Type, settings)), 1, region.Budget);
            });
            Assert.All(plan, spawn =>
            {
                Point cell = new Point(spawn.Position.X / GameConstants.GridCellSize, spawn.Position.Y / GameConstants.GridCellSize);
                Assert.Equal(spawn.RegionIndex, generator.GetRegionId(cell));
            });
        }

        [Fact]
        public void GeneratedArenaObstacleClusterHasSolidCoreAndQuarterTileDestructibleCapsules()
        {
            var settings = new MapGenerationSettings
            {
                Seed = 514,
                CrawlerCount = 4,
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
                PocketChance = 0f,
                MaxPocketCount = 0,
                EncounterDifficultyBudget = 0,
            };
            var generator = new MapGenerator(GameConstants.GridWidth, GameConstants.GridHeight, settings);
            int[,] grid = generator.GenerateMap();

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

        [Fact]
        public void GenerateMap_AssignsArenaOwnershipAndDepthFromSpawnGraph()
        {
            var settings = new MapGenerationSettings
            {
                Seed = 6421,
                CrawlerCount = 4,
                CrawlerMaxSteps = 400,
                CrawlerRoomInterval = 5,
                MinArenaCount = 3,
                MaxArenaCount = 3,
                MinimumSpawnExitPathLength = 20,
                ArenaRadiusMin = 3,
                ArenaRadiusMax = 3,
                ArenaObstacleMinSeeds = 0,
                ArenaObstacleMaxSeeds = 0,
                PocketChance = 1f,
                MaxPocketCount = 2,
                EncounterDifficultyBudget = 0,
            };
            var first = new MapGenerator(50, 50, settings);
            var second = new MapGenerator(50, 50, new MapGenerationSettings
            {
                Seed = settings.Seed,
                CrawlerCount = settings.CrawlerCount,
                CrawlerMaxSteps = settings.CrawlerMaxSteps,
                CrawlerRoomInterval = settings.CrawlerRoomInterval,
                MinArenaCount = settings.MinArenaCount,
                MaxArenaCount = settings.MaxArenaCount,
                MinimumSpawnExitPathLength = settings.MinimumSpawnExitPathLength,
                ArenaRadiusMin = settings.ArenaRadiusMin,
                ArenaRadiusMax = settings.ArenaRadiusMax,
                ArenaObstacleMinSeeds = 0,
                ArenaObstacleMaxSeeds = 0,
                PocketChance = settings.PocketChance,
                MaxPocketCount = settings.MaxPocketCount,
                EncounterDifficultyBudget = 0,
            });

            int[,] map = first.GenerateMap();
            second.GenerateMap();

            Assert.Equal(first.Rooms.Count, first.Regions.Count);
            Assert.Equal(3, first.Regions.Count(region => region.Kind == MapRegionKind.Arena));
            Assert.Equal(first.PocketRoomsPlaced, first.Regions.Count(region => region.Kind == MapRegionKind.Pocket));
            Assert.True(first.PocketRoomsPlaced > 0,
                $"No pockets placed; attempts={first.PocketPlacementAttempts}, arenaCount={first.ArenaRoomsPlaced}.");
            Assert.Equal(first.Regions.Select(region => $"{region.Id}:{region.Kind}:{region.Bounds}:{region.Depth}"),
                second.Regions.Select(region => $"{region.Id}:{region.Kind}:{region.Bounds}:{region.Depth}"));
            int[] expectedDepths = GetRoomDepths(first.Rooms.Count, first.RoomConnections);
            Assert.Equal(0, first.GetRegionId(first.SpawnPoint));
            Assert.Equal(-1, first.GetRegionId(new Point(-1, -1)));

            for (int regionId = 0; regionId < first.Regions.Count; regionId++)
            {
                MapRegionDefinition region = first.Regions[regionId];
                Assert.Equal(regionId, region.Id);
                Assert.True(regionId == 0
                    ? region.Kind == MapRegionKind.Spawn
                    : region.Kind == MapRegionKind.Arena || region.Kind == MapRegionKind.Pocket);
                Assert.Equal(expectedDepths[regionId], region.Depth);
                Assert.True(regionId == 0 ? region.Depth == 0 : region.Depth > 0);
                int expectedBudget = region.Kind == MapRegionKind.Spawn
                    ? 0
                    : region.Kind == MapRegionKind.Pocket
                        ? settings.EncounterPocketDifficultyBudget + region.Depth * settings.EncounterBudgetPerDepth
                        : settings.EncounterDifficultyBudget + region.Depth * settings.EncounterBudgetPerDepth;
                Assert.Equal(expectedBudget, region.Budget);
                bool foundOwnedFloor = false;
                for (int x = region.Bounds.Left; x < region.Bounds.Right; x++)
                {
                    for (int y = region.Bounds.Top; y < region.Bounds.Bottom; y++)
                    {
                        if (map[x, y] != 0) continue;
                        Assert.Equal(regionId, first.RegionIdMap[x, y]);
                        foundOwnedFloor = true;
                    }
                }

                Assert.True(foundOwnedFloor);
            }

            for (int x = 0; x < first.RegionIdMap.GetLength(0); x++)
            {
                for (int y = 0; y < first.RegionIdMap.GetLength(1); y++)
                {
                    if (first.ZoneMap[x, y] == MapZoneType.ChokePoint && map[x, y] == 0)
                        Assert.InRange(first.RegionIdMap[x, y], 1, first.Regions.Count - 1);
                }
            }
        }

        [Fact]
        public void GenerateMap_ScalesRegionalBudgetsToPoolWhileKeepingMinimumGroups()
        {
            var settings = new MapGenerationSettings
            {
                Seed = 723,
                CrawlerCount = 4,
                CrawlerMaxSteps = 400,
                CrawlerRoomInterval = 8,
                MinArenaCount = 3,
                MaxArenaCount = 3,
                MinimumSpawnExitPathLength = 20,
                PocketChance = 1f,
                MaxPocketCount = 2,
                EncounterBudgetPerDepth = 4,
                EncounterPocketDifficultyBudget = 20,
                EncounterDifficultyBudget = 40,
                EncounterMinGroupSize = 3,
                EnemyPoolCapacity = 24,
                ArenaObstacleMinSeeds = 0,
                ArenaObstacleMaxSeeds = 0,
            };
            var generator = new MapGenerator(50, 50, settings);
            generator.GenerateMap();

            MapRegionDefinition[] activeRegions = generator.Regions
                .Where(region => region.Kind != MapRegionKind.Spawn)
                .ToArray();

            Assert.True(activeRegions.Length >= 3);
            Assert.True(activeRegions.Sum(region => region.Budget) <= settings.EnemyPoolCapacity);
            Assert.All(activeRegions, region => Assert.True(region.Budget >= settings.EncounterMinGroupSize));
            Assert.Equal(activeRegions.Length, activeRegions.Select(region => region.Id).Distinct().Count());
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
                PocketChance = 0f,
                MaxPocketCount = 0,
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
            Assert.Equal(1 + first.ArenaRoomsPlaced + first.PocketRoomsPlaced, first.Rooms.Count);
            Assert.Equal(first.Rooms.Count - 1, first.RoomConnections.Count);
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
            Assert.Equal(1 + generator.ArenaRoomsPlaced + generator.PocketRoomsPlaced, generator.Rooms.Count);
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

        private static int GetEncounterCost(EnemyType type, MapGenerationSettings settings)
        {
            switch (type)
            {
                case EnemyType.Rusher: return settings.EncounterRusherCost;
                case EnemyType.Roamer: return settings.EncounterRoamerCost;
                case EnemyType.Turret: return settings.EncounterTurretCost;
                case EnemyType.StaticShooter: return settings.EncounterStaticShooterCost;
                case EnemyType.MobileGenerator: return settings.EncounterMobileGeneratorCost;
                case EnemyType.Spawner: return settings.EncounterSpawnerCost;
                default: return settings.EncounterSwarmerCost;
            }
        }

        private static int[] GetRoomDepths(int roomCount, List<(int From, int To)> connections)
        {
            int[] depths = Enumerable.Repeat(-1, roomCount).ToArray();
            var pending = new Queue<int>();
            depths[0] = 0;
            pending.Enqueue(0);

            while (pending.Count > 0)
            {
                int current = pending.Dequeue();
                foreach ((int from, int to) in connections)
                {
                    int neighbor = from == current ? to : to == current ? from : -1;
                    if (neighbor < 0 || depths[neighbor] >= 0) continue;
                    depths[neighbor] = depths[current] + 1;
                    pending.Enqueue(neighbor);
                }
            }

            return depths;
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