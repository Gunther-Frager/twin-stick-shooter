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
    public void PrimitiveGrid_ShouldCreateRoundedPolyForConnectedRunAndCircleForIsolatedCell()
    {
        var level = new LevelManager(5, 1, 16);
        level.SetCollision(0, 0, true);
        level.SetCollision(1, 0, true);
        level.SetCollision(2, 0, true);
        level.SetCollision(4, 0, true);

        level.RebuildPrimitiveMapFromGrid();

        Assert.Single(level.PrimitiveRoundedPolys);
        Assert.Empty(level.PrimitiveCapsules);
        Assert.Contains(level.PrimitiveCircles, c => c.Center.X > 60f && c.Center.X < 90f);
    }

    [Fact]
    public void GeneratedArenaCover_BreaksAfterConfiguredProjectileHits()
    {
        var level = new LevelManager(30, 30, 80);
        MapGenerationSettings settings = level.MapGenerator.Settings;
        settings.Seed = 301;
        settings.CrawlerCount = 1;
        settings.CrawlerMaxSteps = 120;
        settings.CrawlerRoomInterval = 8;
        settings.MinArenaCount = 1;
        settings.MaxArenaCount = 1;
        settings.MinimumSpawnExitPathLength = 8;
        settings.ArenaRadiusMin = 3;
        settings.ArenaRadiusMax = 3;
        settings.ArenaObstacleMinSeeds = 1;
        settings.ArenaObstacleMaxSeeds = 1;
        settings.ArenaSolidCoreClusterChance = 0f;
        settings.ArenaSolidObstacleChance = 0f;
        settings.ArenaObstacleHitPoints = 5;

        level.GenerateProceduralMap();

        ArenaObstacle obstacle = Assert.Single(level.ArenaObstacles);
        Vector2 impactPosition = (obstacle.Capsule.Start + obstacle.Capsule.End) * 0.5f;
        Assert.True(obstacle.IsDestructible);
        Assert.Equal(10f, obstacle.Capsule.Radius);
        Assert.Equal(5, obstacle.HitPoints);
        Assert.True(level.CheckCollision(impactPosition, 1f));

        Color intactColor = obstacle.OutlineColor;
        Assert.True(level.TryDamageDestructibleAt(impactPosition, Vector2.Zero, 1));
        Assert.True(level.TryDamageDestructibleAt(impactPosition, Vector2.Zero, 1));
        Assert.Equal(3, obstacle.HitPoints);
        Assert.NotEqual(intactColor, obstacle.OutlineColor);
        Assert.True(level.CheckCollision(impactPosition, 1f));

        Assert.True(level.TryDamageDestructibleAt(impactPosition, Vector2.Zero, 3));
        Assert.Empty(level.ArenaObstacles);
        Assert.False(level.CheckCollision(impactPosition, 1f));
    }

    [Fact]
    public void LineOfSight_UsesTheSameWallGeometryAsCollision()
    {
        var level = new LevelManager(20, 20, 20)
        {
            UseRoundedContours = false,
        };
        level.SetCollision(10, 8, true);
        level.SetCollision(10, 9, true);
        level.SetCollision(10, 10, true);
        level.SetCollision(10, 11, true);
        level.SetCollision(10, 12, true);
        level.RebuildPrimitiveMapFromGrid();

        Assert.False(level.HasLineOfSight(new Vector2(100f, 200f), new Vector2(300f, 200f)));
        Assert.True(level.HasLineOfSight(new Vector2(100f, 100f), new Vector2(300f, 100f)));
    }
}
