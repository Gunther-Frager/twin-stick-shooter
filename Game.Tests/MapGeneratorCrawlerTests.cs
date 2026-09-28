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
        public void GenerateMap_WithCrawlerRooms_IsDeterministicAndConnectsRooms()
        {
            var settings = new MapGenerationSettings
            {
                Seed = 7319,
                CrawlerRoomProbability = 1.0,
            };
            var first = new MapGenerator(40, 40, 1, settings);
            var second = new MapGenerator(40, 40, 1, new MapGenerationSettings
            {
                Seed = 7319,
                CrawlerRoomProbability = 1.0,
            });

            int[,] firstMap = first.GenerateMap();
            int[,] secondMap = second.GenerateMap();

            AssertMapsEqual(firstMap, secondMap);
            Assert.Equal(first.RoomConnections, second.RoomConnections);
            Assert.True(first.RoomConnections.Count >= first.Rooms.Count - 1);

            Point[] centers = first.Rooms.Select(room => room.Center).ToArray();
            foreach (Point center in centers)
            {
                Assert.Equal(0, firstMap[center.X, center.Y]);
                Assert.True(first.IsMapTraversable(firstMap, first.SpawnPoint, center));
            }

            AssertRoomGraphIsConnected(first.Rooms.Count, first.RoomConnections);
        }

        [Fact]
        public void GenerateMap_WithZeroCrawlerProbability_LeavesOrdinaryRoomsOpen()
        {
            var noCrawler = new MapGenerator(40, 40, 1, new MapGenerationSettings
            {
                Seed = 514,
                CrawlerRoomProbability = 0.0,
            });
            var crawler = new MapGenerator(40, 40, 1, new MapGenerationSettings
            {
                Seed = 514,
                CrawlerRoomProbability = 1.0,
                CrawlerStepsPerRoomCell = 0,
            });

            int[,] openMap = noCrawler.GenerateMap();
            int[,] closedMap = crawler.GenerateMap();

            int roomIndex = 1;
            Rectangle room = noCrawler.Rooms[roomIndex];
            int openCells = CountWalkableCells(openMap, room);
            int crawlerCells = CountWalkableCells(closedMap, crawler.Rooms[roomIndex]);

            Assert.True(openCells > crawlerCells);
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

        private static int CountWalkableCells(int[,] map, Rectangle room)
        {
            int count = 0;
            for (int x = room.X; x < room.X + room.Width; x++)
            {
                for (int y = room.Y; y < room.Y + room.Height; y++)
                {
                    if (map[x, y] == 0)
                    {
                        count++;
                    }
                }
            }

            return count;
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