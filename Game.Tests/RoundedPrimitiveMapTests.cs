using Microsoft.Xna.Framework;
using TwinStickShooter.Core;
using Xunit;

namespace TwinStickShooter.Tests;

public class RoundedPrimitiveMapTests
{
    [Fact]
    public void RebuildPrimitiveMap_RoundedThreeByThreeRegion_ProducesOnePoly()
    {
        LevelManager level = CreateRoundedRegion();

        Assert.Single(level.PrimitiveRoundedPolys);
        Assert.Empty(level.PrimitiveCapsules);
    }

    [Fact]
    public void RebuildPrimitiveMap_RoundedIsolatedCell_StillProducesCircle()
    {
        var level = new LevelManager(7, 7, 20)
        {
            UseRoundedContours = true
        };
        level.SetCollision(3, 3, true);

        level.RebuildPrimitiveMapFromGrid();

        Assert.Single(level.PrimitiveCircles);
        Assert.Empty(level.PrimitiveRoundedPolys);
    }

    [Fact]
    public void CheckCollision_RoundedRegion_DetectsNearContourButNotBeyondRadius()
    {
        LevelManager level = CreateRoundedRegion();

        Assert.True(level.CheckCollision(new Vector2(41f, 70f), 2f));
        Assert.False(level.CheckCollision(new Vector2(37f, 70f), 2f));
    }

    [Fact]
    public void TryGetPenetration_RoundedRegion_PushesCenterOutOfCollision()
    {
        LevelManager level = CreateRoundedRegion();
        Vector2 center = new Vector2(39f, 70f);

        Assert.True(level.TryGetPenetration(center, 2f, out Vector2 pushOut));
        Assert.NotEqual(Vector2.Zero, pushOut);
        Assert.False(level.CheckCollision(center + pushOut, 2f));
    }

    private static LevelManager CreateRoundedRegion()
    {
        var level = new LevelManager(7, 7, 20)
        {
            UseRoundedContours = true
        };
        for (int x = 2; x <= 4; x++)
        {
            for (int y = 2; y <= 4; y++)
                level.SetCollision(x, y, true);
        }

        level.RebuildPrimitiveMapFromGrid();
        return level;
    }
}