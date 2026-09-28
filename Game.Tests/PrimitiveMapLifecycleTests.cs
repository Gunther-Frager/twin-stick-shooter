using Microsoft.Xna.Framework;
using TwinStickShooter.Core;
using Xunit;

namespace TwinStickShooter.Tests;

public class PrimitiveMapLifecycleTests
{
    [Fact]
    public void ConfigureCombatTestArena_ShouldUseWorldBoundsWithoutInteriorBorderPrimitives()
    {
        var level = new LevelManager(6, 6, 16);

        level.ConfigureCombatTestArena();

        Assert.True(level.HasPrimitiveMapData());
        Assert.Empty(level.PrimitiveCapsules);
        Assert.Empty(level.PrimitiveCircles);
        Assert.True(level.CheckCollision(new Vector2(8f, 8f), 8f));
        Assert.False(level.CheckCollision(new Vector2(24f, 24f), 1f));
        Assert.False(level.CheckCollision(new Vector2(48f, 48f), 0f));
    }

    [Fact]
    public void CheckCollision_ShouldFailClearlyBeforePrimitiveMapInitialization()
    {
        var level = new LevelManager(6, 6, 16);

        var exception = Assert.Throws<System.InvalidOperationException>(
            () => level.CheckCollision(Vector2.Zero, 1f));

        Assert.Contains("RebuildPrimitiveMapFromGrid", exception.Message);
    }
}