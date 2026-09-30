using TwinStickShooter.Core;
using System.Text.Json;
using Xunit;

namespace TwinStickShooter.Tests
{
    public class RoomTemplateDataTests
    {
        [Fact]
        public void TryGetCellReturnsWallAndFloorValues()
        {
            RoomTemplateData template = new RoomTemplateData
            {
                Grid = new[]
                {
                    "101",
                    "000",
                    "111",
                },
            };

            Assert.True(template.TryGetCell(0, 0, out bool firstCellIsWall));
            Assert.True(firstCellIsWall);

            Assert.True(template.TryGetCell(1, 0, out bool secondCellIsWall));
            Assert.False(secondCellIsWall);
        }

        [Theory]
        [InlineData(-1, 0)]
        [InlineData(0, -1)]
        [InlineData(3, 0)]
        [InlineData(0, 3)]
        public void TryGetCellRejectsCoordinatesOutsideGrid(int localX, int localY)
        {
            RoomTemplateData template = new RoomTemplateData
            {
                Grid = new[] { "101", "000", "111" },
            };

            Assert.False(template.TryGetCell(localX, localY, out bool isWall));
            Assert.False(isWall);
        }

        [Fact]
        public void TryGetCellRejectsMissingGrid()
        {
            RoomTemplateData template = new RoomTemplateData();

            Assert.False(template.TryGetCell(0, 0, out bool isWall));
            Assert.False(isWall);
        }

        [Fact]
        public void JsonWithLegacyEnemySpawnsLoadsOnlyTerrainData()
        {
            const string json = "{\"id\":\"legacy\",\"grid\":[\"111\",\"101\",\"111\"],\"enemySpawns\":[{\"x\":1,\"y\":1,\"type\":\"Turret\"}]}";
            var template = JsonSerializer.Deserialize<RoomTemplateData>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
            });

            Assert.Equal("legacy", template.Id);
            Assert.True(template.TryGetCell(1, 1, out bool isWall));
            Assert.False(isWall);
        }
    }
}