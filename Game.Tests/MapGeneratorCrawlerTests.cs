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
        public void GenerateMap_CrawlerPeriodicallyInsertsConnectedPrefabRoomsDeterministically()
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
            first.SetRoomTemplates(CreateTemplates());
            var second = new MapGenerator(40, 40, new MapGenerationSettings
            {
                Seed = 7319,
                CrawlerMaxSteps = 180,
                CrawlerRoomInterval = 8,
                MinPrefabRooms = 2,
                MaxPrefabRooms = 4,
                MinimumSpawnExitPathLength = 18,
            });
            second.SetRoomTemplates(CreateTemplates());

            int[,] firstMap = first.GenerateMap();
            int[,] secondMap = second.GenerateMap();

            AssertMapsEqual(firstMap, secondMap);
            Assert.Equal(first.RoomConnections, second.RoomConnections);
            Assert.True(first.PrefabRoomsPlaced > 0);
            Assert.Equal(first.PrefabRoomsPlaced + 1, first.Rooms.Count);
            Assert.Equal(first.PrefabRoomsPlaced, first.RoomConnections.Count);
            Assert.True(first.PrefabPlacementAttempts >= first.PrefabRoomsPlaced);

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

            foreach (Rectangle room in first.Rooms.Skip(1))
            {
                int openBoundaryCells = 0;
                for (int x = room.Left; x < room.Right; x++)
                {
                    if (firstMap[x, room.Top] == 0) openBoundaryCells++;
                    if (firstMap[x, room.Bottom - 1] == 0) openBoundaryCells++;
                }
                for (int y = room.Top + 1; y < room.Bottom - 1; y++)
                {
                    if (firstMap[room.Left, y] == 0) openBoundaryCells++;
                    if (firstMap[room.Right - 1, y] == 0) openBoundaryCells++;
                }

                Assert.Equal(1, openBoundaryCells);
            }

            AssertRoomGraphIsConnected(first.Rooms.Count, first.RoomConnections);
        }

        [Fact]
        public void GenerateMap_WhenPrefabCannotFit_CrawlerContinuesAndProducesTraversableMap()
        {
            var generator = new MapGenerator(20, 20, new MapGenerationSettings
            {
                Seed = 514,
                CrawlerMaxSteps = 80,
                CrawlerRoomInterval = 5,
                MinPrefabRooms = 0,
                MaxPrefabRooms = 4,
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

            Assert.True(generator.PrefabPlacementAttempts > 0);
            Assert.Equal(0, generator.PrefabRoomsPlaced);
            Assert.Single(generator.Rooms);
            Assert.True(generator.SpawnExitPathLength >= generator.Settings.MinimumSpawnExitPathLength);
            Assert.True(generator.IsMapTraversable(map, generator.SpawnPoint, generator.ExitPoint));
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
            generator.SetRoomTemplates(CreateTemplates());

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
            generator.SetRoomTemplates(CreateTemplates());

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