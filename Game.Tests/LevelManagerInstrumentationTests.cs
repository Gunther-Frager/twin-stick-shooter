using Microsoft.Xna.Framework;
using TwinStickShooter.Core;
using Xunit;

namespace TwinStickShooter.Tests;

public class LevelManagerInstrumentationTests
{
#if DEBUG
    [Fact]
    public void QueryCounters_IncrementAndReset_WorksAsExpected()
    {
        var level = new LevelManager(GameConstants.GridWidth, GameConstants.GridHeight, 80);
        level.ConfigureCombatTestArena();

        Assert.Equal(0, level.CollisionQueryCount);
        Assert.Equal(0, level.PenetrationQueryCount);
        Assert.Equal(0, level.SweepQueryCount);

        Vector2 center = new Vector2(level.WorldWidth / 2f, level.WorldHeight / 2f);

        level.CheckCollision(center, 10f);
        Assert.Equal(1, level.CollisionQueryCount);

        level.TryGetPenetration(center, 10f, out _);
        Assert.Equal(1, level.PenetrationQueryCount);

        level.TrySweepCircle(center, center + new Vector2(10, 0), 10f, out _, out _);
        Assert.True(level.SweepQueryCount >= 1);

        level.ResetQueryCounters();

        Assert.Equal(0, level.CollisionQueryCount);
        Assert.Equal(0, level.PenetrationQueryCount);
        Assert.Equal(0, level.SweepQueryCount);
    }
#endif
}

