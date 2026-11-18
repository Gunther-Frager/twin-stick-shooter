using System;
using Microsoft.Xna.Framework;
using TwinStickShooter.Core;
using TwinStickShooter.Entities;
using Xunit;

namespace TwinStickShooter.Tests;

public class ManagerCountersTests
{
    private static int BruteForceActiveCount(EnemyManager enemyManager)
    {
        int count = 0;
        Enemy[] enemies = enemyManager.Enemies;
        for (int i = 0; i < enemies.Length; i++)
        {
            if (enemies[i].Active) count++;
        }
        return count;
    }

    private static int BruteForceActiveCount(SpawnerManager spawnerManager)
    {
        int count = 0;
        SpawnerManager.SpawnerData[] spawners = spawnerManager.Spawners;
        for (int i = 0; i < spawners.Length; i++)
        {
            if (spawners[i].Active) count++;
        }
        return count;
    }

    [Fact]
    public void EnemyManager_RandomOperationsSequence_ActiveCountMatchesBruteForce()
    {
        var level = new LevelManager(GameConstants.GridWidth, GameConstants.GridHeight, GameConstants.GridCellSize);
        level.GenerateProceduralMap();
        var enemyManager = new EnemyManager(level);
        var enemyBulletManager = new EnemyBulletManager(level);
        var players = new Player[] { new Player(0, level.GetSpawnPosition()) };
        players[0].IsActive = true;

        var rnd = new Random(123);
        EnemyType[] types = (EnemyType[])Enum.GetValues(typeof(EnemyType));
        Vector2 spawnBase = level.GetSpawnPosition();

        int previousDeathVersion = enemyManager.DeathVersion;

        for (int step = 0; step < 5000; step++)
        {
            int action = rnd.Next(100);

            if (action < 45)
            {
                // Spawn
                EnemyType type = types[rnd.Next(types.Length)];
                float offsetX = (float)(rnd.NextDouble() * 80.0 - 40.0);
                float offsetY = (float)(rnd.NextDouble() * 80.0 - 40.0);
                Vector2 pos = spawnBase + new Vector2(offsetX, offsetY);
                enemyManager.Spawn(pos, Vector2.UnitX, type, regionId: rnd.Next(-1, 5));
            }
            else if (action < 75)
            {
                // ApplyDamage
                int targetIndex = rnd.Next(enemyManager.Capacity);
                float damage = (float)(rnd.NextDouble() * 50.0);
                enemyManager.ApplyDamage(targetIndex, damage);
            }
            else if (action < 95)
            {
                // Update (incluye hijos de MobileGenerator y muerte por out-of-world)
                enemyManager.Update(0.016f, players, enemyBulletManager, null);
            }
            else
            {
                // Clear
                enemyManager.Clear();
            }

            int expected = BruteForceActiveCount(enemyManager);
            Assert.Equal(expected, enemyManager.ActiveCount);

            // La versión de muerte nunca debe decrecer
            Assert.True(enemyManager.DeathVersion >= previousDeathVersion);
            previousDeathVersion = enemyManager.DeathVersion;
        }
    }

    [Fact]
    public void SpawnerManager_RandomOperationsSequence_ActiveCountMatchesBruteForce()
    {
        var level = new LevelManager(GameConstants.GridWidth, GameConstants.GridHeight, GameConstants.GridCellSize);
        level.GenerateProceduralMap();
        var enemyManager = new EnemyManager(level);
        var spawnerManager = new SpawnerManager(128, enemyManager);

        var rnd = new Random(123);
        Vector2 spawnBase = level.GetSpawnPosition();

        int previousDeathVersion = spawnerManager.DeathVersion;

        for (int step = 0; step < 5000; step++)
        {
            int action = rnd.Next(100);

            if (action < 45)
            {
                // Register
                float offsetX = (float)(rnd.NextDouble() * 100.0 - 50.0);
                float offsetY = (float)(rnd.NextDouble() * 100.0 - 50.0);
                spawnerManager.Register(spawnBase + new Vector2(offsetX, offsetY), regionId: rnd.Next(1, 10));
            }
            else if (action < 80)
            {
                // ApplyDamage
                int targetIndex = rnd.Next(spawnerManager.Spawners.Length);
                float damage = (float)(rnd.NextDouble() * 40.0);
                spawnerManager.ApplyDamage(targetIndex, damage);
            }
            else if (action < 95)
            {
                // Update (puede generar hijos en EnemyManager)
                spawnerManager.Update(GameConstants.SpawnerInterval + 0.1f);
            }
            else
            {
                // Reset
                spawnerManager.Reset();
            }

            int expected = BruteForceActiveCount(spawnerManager);
            Assert.Equal(expected, spawnerManager.ActiveCount);

            // La versión de muerte nunca debe decrecer
            Assert.True(spawnerManager.DeathVersion >= previousDeathVersion);
            previousDeathVersion = spawnerManager.DeathVersion;
        }
    }
}
