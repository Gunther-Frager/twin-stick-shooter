using TwinStickShooter.Core;
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
    }
}