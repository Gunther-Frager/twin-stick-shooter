using Microsoft.Xna.Framework;
using TwinStickShooter.Core;
using TwinStickShooter.Entities;
using Xunit;

namespace TwinStickShooter.Tests;

public class CollisionRegressionTests
{
    [Fact]
    public void TrySweepCircle_ShouldDetectWallBetweenEndpoints()
    {
        LevelManager level = CreateLevelWithWall();

        bool hit = level.TrySweepCircle(
            new Vector2(30f, 80f),
            new Vector2(110f, 80f),
            GameConstants.BulletRadius,
            out Vector2 hitPosition,
            out Vector2 hitNormal);

        Assert.True(hit);
        Assert.InRange(hitPosition.X, 55f, 62f);
        Assert.True(hitNormal.X < -0.9f, $"Normal calculada: {hitNormal}");
    }

    [Fact]
    public void Bullet_ShouldStopAtWallWithoutDamagingTargetBehindIt()
    {
        LevelManager level = CreateLevelWithWall();
        EnemyManager enemies = new EnemyManager(level);
        SpawnerManager spawners = new SpawnerManager(1, enemies);
        Assert.True(enemies.Spawn(new Vector2(90f, 80f), Vector2.Zero));

        BulletManager bullets = new BulletManager(level);
        bullets.Spawn(new Vector2(30f, 80f), 0f, 0, Color.White);
        bullets.Update(0.25f, enemies, spawners);

        Assert.Equal(GameConstants.SwarmerHealth, enemies.Enemies[0].Health);
        Assert.False(bullets.Bullets[0].Active);
    }

    [Fact]
    public void EnemySpawn_ShouldRejectPositionInsideWall()
    {
        LevelManager level = CreateLevelWithWall();
        EnemyManager enemies = new EnemyManager(level);

        Assert.False(enemies.Spawn(new Vector2(70f, 80f), Vector2.Zero));
        Assert.Equal(0, enemies.ActiveCount);
    }

    [Fact]
    public void Particle_ShouldBeDiscardedWhenItReachesWall()
    {
        LevelManager level = CreateLevelWithWall();
        ParticleSystem particles = new ParticleSystem(1, level);

        particles.Emit(new Vector2(30f, 80f), new Vector2(400f, 0f), 1f, 6f, Color.White);
        particles.Update(0.2f);

        Assert.False(particles.Particles[0].Active);
    }

    [Fact]
    public void Bullet_WithOneBounce_ShouldReflectAndRemainActive()
    {
        LevelManager level = CreateLevelWithWall();
        EnemyManager enemies = new EnemyManager(level);
        SpawnerManager spawners = new SpawnerManager(1, enemies);
        BulletManager bullets = new BulletManager(level);

        bullets.Spawn(new Vector2(30f, 80f), 0f, 0, Color.White, 1);
        bullets.Update(0.25f, enemies, spawners);

        Assert.True(bullets.Bullets[0].Active);
        Assert.True(bullets.Bullets[0].Velocity.X < 0f);
        Assert.Equal(0, bullets.Bullets[0].RemainingBounces);
    }

    private static LevelManager CreateLevelWithWall()
    {
        LevelManager level = new LevelManager(6, 6, 20)
        {
            UseRoundedContours = false
        };
        for (int y = 1; y < 5; y++)
        {
            if (y != 2)
            {
                level.SetCollision(3, y, true);
            }
        }

        level.SetSpawnPosition(1, 2);
        level.RebuildPrimitiveMapFromGrid();
        return level;
    }
}