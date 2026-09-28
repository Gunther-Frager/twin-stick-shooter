using Microsoft.Xna.Framework;
using TwinStickShooter.Core;
using Xunit;

namespace TwinStickShooter.Tests;

public class PrimitiveMapLifecycleTests
{
    [Fact]
    public void ConfigureCombatTestArena_ShouldInitializePrimitiveMapAndPreserveBorderCollision()
    {
        var level = new LevelManager(6, 6, 16);

        level.ConfigureCombatTestArena();

        Assert.True(level.HasPrimitiveMapData());
        Assert.Equal(4, level.PrimitiveCapsules.Count);
        Assert.Equal(4, level.PrimitiveCircles.Count);
        Assert.True(level.CheckCollision(new Vector2(8f, 8f), 0f));
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