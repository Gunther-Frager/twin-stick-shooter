using System;
using Microsoft.Xna.Framework;
using TwinStickShooter.Core;
using TwinStickShooter.Entities;
using Xunit;

namespace TwinStickShooter.Tests;

public class ZeroAllocationTests
{
    private static void AssertNoAllocations(Action warmup, Action measured, int iterations = 100)
    {
        // 1. Warmup para dar tiempo al JIT y cualquier inicialización perezosa (lazy init)
        for (int i = 0; i < 5; i++)
        {
            warmup();
        }

        // 2. Limpieza de memoria previa
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        // 3. Medición
        long bytesBefore = GC.GetAllocatedBytesForCurrentThread();

        for (int i = 0; i < iterations; i++)
        {
            measured();
        }

        long bytesAfter = GC.GetAllocatedBytesForCurrentThread();
        long allocated = bytesAfter - bytesBefore;

        Assert.True(allocated == 0, $"Se esperaba 0 bytes de alocación pero se alocaron {allocated} bytes a lo largo de {iterations} iteraciones.");
    }

    private static LevelManager CreateArena()
    {
        var level = new LevelManager(GameConstants.GridWidth, GameConstants.GridHeight, 80);
        level.ConfigureCombatTestArena();
        return level;
    }

    [Fact]
    public void ParticleSystem_EmitAndUpdate_AllocatesZero()
    {
        var level = CreateArena();
        var psWithLevel = new ParticleSystem(100, level);
        var psWithoutLevel = new ParticleSystem(100, null);
        Vector2 pos = new Vector2(100f, 100f);
        Vector2 vel = new Vector2(10f, 0f);

        AssertNoAllocations(
            warmup: () =>
            {
                psWithLevel.Emit(pos, vel, 0.5f, 4f, Color.Orange);
                psWithLevel.Update(0.016f);
                psWithoutLevel.Emit(pos, vel, 0.5f, 4f, Color.White);
                psWithoutLevel.Update(0.016f);
            },
            measured: () =>
            {
                psWithLevel.Emit(pos, vel, 0.5f, 4f, Color.Orange);
                psWithLevel.Update(0.016f);
                psWithoutLevel.Emit(pos, vel, 0.5f, 4f, Color.White);
                psWithoutLevel.Update(0.016f);
            },
            iterations: 50
        );
    }

    [Fact]
    public void BulletManager_SpawnAndUpdate_AllocatesZero()
    {
        var level = CreateArena();
        var bm = new BulletManager(level);
        var enemyManager = new EnemyManager(level);
        var spawnerManager = new SpawnerManager(10, enemyManager);

        Vector2 origin = new Vector2(120f, 120f);

        AssertNoAllocations(
            warmup: () =>
            {
                bm.Spawn(origin, 0f, 0, Color.Yellow);
                bm.Update(0.016f, enemyManager, spawnerManager);
            },
            measured: () =>
            {
                bm.Spawn(origin, 0f, 0, Color.Yellow);
                bm.Update(0.016f, enemyManager, spawnerManager);
            },
            iterations: 50
        );
    }

    [Fact]
    public void EnemyBulletManager_SpawnAndUpdate_AllocatesZero()
    {
        var level = CreateArena();
        var ebm = new EnemyBulletManager(level);
        var player = new Player(0, new Vector2(200f, 200f)) { IsActive = true };
        var players = new[] { player };

        Vector2 origin = new Vector2(150f, 150f);

        AssertNoAllocations(
            warmup: () =>
            {
                ebm.Spawn(origin, 0f, Color.Red);
                ebm.Update(0.016f, players);
            },
            measured: () =>
            {
                ebm.Spawn(origin, 0f, Color.Red);
                ebm.Update(0.016f, players);
            },
            iterations: 50
        );
    }

    [Fact]
    public void EnemyManager_Update_ActiveEnemiesAndPlayer_AllocatesZero()
    {
        var level = CreateArena();
        var em = new EnemyManager(level);
        var bullets = new EnemyBulletManager(level);
        var player = new Player(0, new Vector2(300f, 300f)) { IsActive = true };
        var players = new[] { player };

        EnemyType[] types = new[]
        {
            EnemyType.Swarmer,
            EnemyType.Roamer,
            EnemyType.Rusher,
            EnemyType.StaticShooter,
            EnemyType.MobileGenerator
        };

        for (int i = 0; i < types.Length; i++)
        {
            em.Spawn(new Vector2(100f + i * 40f, 100f + i * 40f), Vector2.Zero, types[i], regionId: -1);
        }

        AssertNoAllocations(
            warmup: () =>
            {
                em.Update(0.016f, players, bullets);
            },
            measured: () =>
            {
                em.Update(0.016f, players, bullets);
            },
            iterations: 50
        );
    }

    [Fact]
    public void SpawnerManager_Update_AllocatesZero()
    {
        var level = CreateArena();
        var em = new EnemyManager(level);
        var sm = new SpawnerManager(10, em);

        sm.Register(new Vector2(250f, 250f), regionId: -1);

        AssertNoAllocations(
            warmup: () =>
            {
                sm.Update(0.016f);
            },
            measured: () =>
            {
                sm.Update(0.016f);
            },
            iterations: 50
        );
    }

    [Fact]
    public void ObjectPool_AcquireAndRelease_AllocatesZero()
    {
        var pool = new ObjectPool<Bullet>(16);

        AssertNoAllocations(
            warmup: () =>
            {
                if (pool.TryAcquire(out int idx, out _))
                {
                    pool.Release(idx);
                }
            },
            measured: () =>
            {
                if (pool.TryAcquire(out int idx, out _))
                {
                    pool.Release(idx);
                }
            },
            iterations: 100
        );
    }
}
