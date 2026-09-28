using System;
using Microsoft.Xna.Framework;
using TwinStickShooter.Core;
using Xunit;

namespace TwinStickShooter.Tests
{
    public class MapGeneratorTests
    {
        [Fact]
        public void FindPathDistance_ReturnsShortestWalkableCellPath()
        {
            var generator = new MapGenerator(5, 5, 1);
            int[,] grid =
            {
                { 0, 1, 0, 0, 0 },
                { 0, 1, 0, 1, 0 },
                { 0, 0, 0, 1, 0 },
                { 1, 1, 0, 0, 0 },
                { 0, 0, 0, 1, 0 },
            };

            Assert.Equal(8, generator.FindPathDistance(grid, new Point(0, 0), new Point(4, 4)));
        }

        [Fact]
        public void GenerateMap_WithSameSeed_ProducesSameMapAndValidExitDistance()
        {
            var first = new MapGenerator(30, 30, 80, new MapGenerationSettings { Seed = 1731 });
            var second = new MapGenerator(30, 30, 80, new MapGenerationSettings { Seed = 1731 });

            int[,] firstMap = first.GenerateMap();
            int[,] secondMap = second.GenerateMap();

            AssertMapsEqual(firstMap, secondMap);
            Assert.True(first.IsMapTraversable(firstMap, first.SpawnPoint, first.ExitPoint));
            Assert.True(first.SpawnExitPathLength >= first.Settings.MinimumSpawnExitPathLength);
            Assert.Equal(first.SpawnExitPathLength, first.FindPathDistance(firstMap, first.SpawnPoint, first.ExitPoint));
        }

        [Fact]
        public void GenerateMap_WhenMinimumDistanceIsImpossible_ReportsFailure()
        {
            var settings = new MapGenerationSettings
            {
                Seed = 9,
                MaxGenerationAttempts = 2,
            };
            var generator = new MapGenerator(30, 30, 80, settings);
            int[,] previousMap = generator.GenerateMap();
            Point previousSpawn = generator.SpawnPoint;
            Point previousExit = generator.ExitPoint;
            int previousPathLength = generator.SpawnExitPathLength;
            Rectangle[] previousRooms = generator.Rooms.ToArray();
            (int From, int To)[] previousConnections = generator.RoomConnections.ToArray();
            settings.MinimumSpawnExitPathLength = 1000;

            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => generator.GenerateMap());

            Assert.Contains("Distancia mínima requerida", exception.Message);
            Assert.Equal(exception.Message, generator.LastGenerationFailure);
            Assert.Equal(previousSpawn, generator.SpawnPoint);
            Assert.Equal(previousExit, generator.ExitPoint);
            Assert.Equal(previousPathLength, generator.SpawnExitPathLength);
            Assert.Equal(previousRooms, generator.Rooms);
            Assert.Equal(previousConnections, generator.RoomConnections);
            Assert.True(generator.IsMapTraversable(previousMap, generator.SpawnPoint, generator.ExitPoint));
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