using TwinStickShooter.Core;
using Xunit;

namespace TwinStickShooter.Tests;

public class PrimitiveDecompositionTests
{
    [Fact]
    public void PrimitiveGrid_ShouldPartitionTShapeIntoTwoRectangles()
    {
        var level = new LevelManager(6, 6, 16);
        level.SetCollision(2, 1, true);
        level.SetCollision(1, 2, true);
        level.SetCollision(2, 2, true);
        level.SetCollision(3, 2, true);

        level.RebuildPrimitiveMapFromGrid();

        Assert.Single(level.PrimitiveCircles);
        var capsule = Assert.Single(level.PrimitiveCapsules);
        Assert.True(capsule.Start.X < capsule.End.X);
    }
}