using System;
using Microsoft.Xna.Framework;
using TwinStickShooter.Core;
using TwinStickShooter.Entities;
using Xunit;

namespace TwinStickShooter.Tests;

public class FindHitEquivalenceTests
{
    private static int FindHitReference(EnemyManager enemyManager, Vector2 position, float radius)
    {
        for (int i = 0; i < enemyManager.Enemies.Length; i++)
        {
            Enemy enemy = enemyManager.Enemies[i];
            if (enemy.Active &&
                Vector2.Distance(position, enemy.Position) < radius + enemy.Radius)
            {
                return i;
            }
        }
        return -1;
    }

    private static int FindHitReference(SpawnerManager spawnerManager, Vector2 position, float radius)
    {
        for (int i = 0; i < spawnerManager.Spawners.Length; i++)
        {
            if (spawnerManager.Spawners[i].Active &&
                Vector2.Distance(position, spawnerManager.Spawners[i].Position) < radius + GameConstants.SpawnerRadius)
            {
                return i;
            }
        }
        return -1;
    }

    [Fact]
    public void EnemyManager_FindHit_MatchesReferenceImplementation()
    {
        var level = new LevelManager(GameConstants.GridWidth, GameConstants.GridHeight, GameConstants.GridCellSize);
        level.GenerateProceduralMap();
        var enemyManager = new EnemyManager(level);

        var rnd = new Random(42);
        Vector2 spawnBase = level.GetSpawnPosition();
        EnemyType[] types = (EnemyType[])Enum.GetValues(typeof(EnemyType));

        // Spawnear varios enemigos con distintas posiciones y radios
        for (int i = 0; i < 150; i++)
        {
            float x = spawnBase.X + (float)(rnd.NextDouble() * 300.0 - 150.0);
            float y = spawnBase.Y + (float)(rnd.NextDouble() * 300.0 - 150.0);
            EnemyType type = types[rnd.Next(types.Length)];
            enemyManager.Spawn(new Vector2(x, y), Vector2.Zero, type);
        }

        // Ejecutar 5000 consultas con posiciones y radios aleatorios
        for (int q = 0; q < 5000; q++)
        {
            Vector2 queryPos = spawnBase + new Vector2(
                (float)(rnd.NextDouble() * 350.0 - 175.0),
                (float)(rnd.NextDouble() * 350.0 - 175.0));
            float queryRadius = (float)(rnd.NextDouble() * 20.0 + 5.0);

            // Descartar si cae exactamente en el umbral ULP (diferencia menor a 0.001)
            bool isAmbiguousBoundary = false;
            for (int i = 0; i < enemyManager.Enemies.Length; i++)
            {
                if (!enemyManager.Enemies[i].Active) continue;
                float d = Vector2.Distance(queryPos, enemyManager.Enemies[i].Position);
                float r = queryRadius + enemyManager.Enemies[i].Radius;
                if (Math.Abs(d - r) < 0.002f)
                {
                    isAmbiguousBoundary = true;
                    break;
                }
            }

            if (isAmbiguousBoundary) continue;

            int refResult = FindHitReference(enemyManager, queryPos, queryRadius);
            int optResult = enemyManager.FindHit(queryPos, queryRadius);

            Assert.Equal(refResult, optResult);
        }
    }

    [Fact]
    public void SpawnerManager_FindHit_MatchesReferenceImplementation()
    {
        var level = new LevelManager(GameConstants.GridWidth, GameConstants.GridHeight, GameConstants.GridCellSize);
        level.GenerateProceduralMap();
        var enemyManager = new EnemyManager(level);
        var spawnerManager = new SpawnerManager(128, enemyManager);

        var rnd = new Random(42);
        Vector2 spawnBase = level.GetSpawnPosition();

        for (int i = 0; i < 40; i++)
        {
            float x = spawnBase.X + (float)(rnd.NextDouble() * 400.0 - 200.0);
            float y = spawnBase.Y + (float)(rnd.NextDouble() * 400.0 - 200.0);
            spawnerManager.Register(new Vector2(x, y));
        }

        for (int q = 0; q < 5000; q++)
        {
            Vector2 queryPos = spawnBase + new Vector2(
                (float)(rnd.NextDouble() * 450.0 - 225.0),
                (float)(rnd.NextDouble() * 450.0 - 225.0));
            float queryRadius = (float)(rnd.NextDouble() * 25.0 + 5.0);

            bool isAmbiguousBoundary = false;
            for (int i = 0; i < spawnerManager.Spawners.Length; i++)
            {
                if (!spawnerManager.Spawners[i].Active) continue;
                float d = Vector2.Distance(queryPos, spawnerManager.Spawners[i].Position);
                float r = queryRadius + GameConstants.SpawnerRadius;
                if (Math.Abs(d - r) < 0.002f)
                {
                    isAmbiguousBoundary = true;
                    break;
                }
            }

            if (isAmbiguousBoundary) continue;

            int refResult = FindHitReference(spawnerManager, queryPos, queryRadius);
            int optResult = spawnerManager.FindHit(queryPos, queryRadius);

            Assert.Equal(refResult, optResult);
        }
    }
}
