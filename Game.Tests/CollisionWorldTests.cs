using Microsoft.Xna.Framework;
using TwinStickShooter.Core;
using Xunit;

namespace TwinStickShooter.Tests;

public class CollisionWorldTests
{
    [Fact]
    public void CircleCollision_ShouldDetectOverlap()
    {
        var a = new MapCircle { Center = new Vector2(0f, 0f), Radius = 10f };
        var b = new MapCircle { Center = new Vector2(15f, 0f), Radius = 10f };

        Assert.True(MapCollisionResolver.CircleIntersectsCircle(a, b));
    }

    [Fact]
    public void CircleCollision_ShouldNotDetectSeparation()
    {
        var a = new MapCircle { Center = new Vector2(0f, 0f), Radius = 10f };
        var b = new MapCircle { Center = new Vector2(40f, 0f), Radius = 10f };

        Assert.False(MapCollisionResolver.CircleIntersectsCircle(a, b));
    }

    [Fact]
    public void CircleVsCapsule_ShouldDetectOverlap()
    {
        var circle = new MapCircle { Center = new Vector2(0f, 0f), Radius = 10f };
        var capsule = new MapCapsule
        {
            Start = new Vector2(-30f, 0f),
            End = new Vector2(30f, 0f),
            Radius = 8f
        };

        Assert.True(MapCollisionResolver.CircleIntersectsCapsule(circle, capsule));
    }

    [Fact]
    public void PrimitiveGrid_ShouldCreateCapsulesForConnectedRuns()
    {
        var level = new LevelManager(5, 1, 16);
        level.SetCollision(0, 0, true);
        level.SetCollision(1, 0, true);
        level.SetCollision(2, 0, true);
        level.SetCollision(4, 0, true);

        level.RebuildPrimitiveMapFromGrid();

        Assert.Contains(level.PrimitiveCapsules, c => c.Start.X < c.End.X);
        Assert.Contains(level.PrimitiveCircles, c => c.Center.X > 60f && c.Center.X < 90f);
    }
}
