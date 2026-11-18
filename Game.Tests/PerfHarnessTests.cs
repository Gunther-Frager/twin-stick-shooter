using System;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using TwinStickShooter.Core;
using TwinStickShooter.Entities;
using Xunit;
using Xunit.Abstractions;

namespace TwinStickShooter.Tests;

public class PerfHarnessTests
{
    private readonly ITestOutputHelper _output;

    public PerfHarnessTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    [Trait("Category", "Perf")]
    public void Benchmark_EnemyManager_ActiveCountAndFindHit()
    {
        var level = new LevelManager(GameConstants.GridWidth, GameConstants.GridHeight, GameConstants.GridCellSize);
        level.GenerateProceduralMap();
        var enemyManager = new EnemyManager(level);

        // Llenar el pool con 1024 enemigos activos en posición caminable garantizada
        Vector2 spawnPos = level.GetSpawnPosition();
        for (int i = 0; i < GameConstants.MaxEnemies; i++)
        {
            float offsetX = ((i % 16) - 8) * 0.5f;
            float offsetY = ((i / 16) - 32) * 0.5f;
            bool spawned = enemyManager.Spawn(spawnPos + new Vector2(offsetX, offsetY), Vector2.Zero, EnemyType.Swarmer);
            Assert.True(spawned, $"No se pudo spawnear el enemigo {i}");
        }

        Assert.Equal(GameConstants.MaxEnemies, enemyManager.ActiveCount);

        // Medir ActiveCount
        const int activeCountIterations = 50_000;
        int dummyCount = 0;
        var sw = Stopwatch.StartNew();
        for (int i = 0; i < activeCountIterations; i++)
        {
            dummyCount += enemyManager.ActiveCount;
        }
        sw.Stop();
        double activeCountMs = sw.Elapsed.TotalMilliseconds;
        double activeCountPerOpNs = (activeCountMs * 1_000_000.0) / activeCountIterations;

        // Medir FindHit
        const int findHitIterations = 20_000;
        var rnd = new Random(1);
        int hitCount = 0;
        sw.Restart();
        for (int i = 0; i < findHitIterations; i++)
        {
            Vector2 p = new Vector2(rnd.Next(50, 750), rnd.Next(50, 750));
            if (enemyManager.FindHit(p, 16f) >= 0)
            {
                hitCount++;
            }
        }
        sw.Stop();
        double findHitMs = sw.Elapsed.TotalMilliseconds;
        double findHitPerOpNs = (findHitMs * 1_000_000.0) / findHitIterations;

        _output.WriteLine($"[Benchmark] EnemyManager.ActiveCount ({activeCountIterations} iters): {activeCountMs:0.000} ms total ({activeCountPerOpNs:0.0} ns/op) [checksum={dummyCount}]");
        _output.WriteLine($"[Benchmark] EnemyManager.FindHit ({findHitIterations} iters con 1024 enemigos): {findHitMs:0.000} ms total ({findHitPerOpNs:0.0} ns/op) [hits={hitCount}]");
    }

    [Fact]
    [Trait("Category", "Perf")]
    public void Benchmark_SpawnerManager_GetAliveCountInRegion()
    {
        var level = new LevelManager(GameConstants.GridWidth, GameConstants.GridHeight, GameConstants.GridCellSize);
        level.GenerateProceduralMap();
        var enemyManager = new EnemyManager(level);
        var spawnerManager = new SpawnerManager(1024, enemyManager);

        // Registrar spawners en 40 regiones distintas
        for (int r = 1; r <= 40; r++)
        {
            spawnerManager.Register(new Vector2(r * 20f, r * 20f), regionId: r);
        }

        const int iterations = 10_000;
        int aliveSum = 0;
        var sw = Stopwatch.StartNew();
        for (int i = 0; i < iterations; i++)
        {
            for (int r = 1; r <= 40; r++)
            {
                aliveSum += spawnerManager.GetAliveCountInRegion(r);
            }
        }
        sw.Stop();
        double totalMs = sw.Elapsed.TotalMilliseconds;
        double perRegionCheckNs = (totalMs * 1_000_000.0) / (iterations * 40);

        _output.WriteLine($"[Benchmark] SpawnerManager.GetAliveCountInRegion (40 regiones x {iterations} frames): {totalMs:0.000} ms total ({perRegionCheckNs:0.0} ns/query) [checksum={aliveSum}]");
    }

    [Fact]
    [Trait("Category", "Perf")]
    public void Benchmark_RoundedContour_ClosestPoint()
    {
        var level = new LevelManager(GameConstants.GridWidth, GameConstants.GridHeight, GameConstants.GridCellSize);
        level.GenerateProceduralMap();
        var polys = level.PrimitiveRoundedPolys;

        Assert.NotEmpty(polys);

        const int queryCount = 100_000;
        var rnd = new Random(1);
        var testPoints = new Vector2[queryCount];
        for (int i = 0; i < queryCount; i++)
        {
            testPoints[i] = new Vector2(
                (float)(rnd.NextDouble() * level.WorldWidth),
                (float)(rnd.NextDouble() * level.WorldHeight));
        }

        float dummyAccum = 0f;
        var sw = Stopwatch.StartNew();
        for (int i = 0; i < queryCount; i++)
        {
            MapRoundedPoly poly = polys[i % polys.Count];
            RoundedContour.ClosestPoint(poly, testPoints[i], out Vector2 closest, out Vector2 normal);
            dummyAccum += closest.X + normal.Y;
        }
        sw.Stop();
        double totalMs = sw.Elapsed.TotalMilliseconds;
        double perQueryNs = (totalMs * 1_000_000.0) / queryCount;

        _output.WriteLine($"[Benchmark] RoundedContour.ClosestPoint ({queryCount} queries sobre {polys.Count} polys): {totalMs:0.000} ms total ({perQueryNs:0.0} ns/query) [checksum={dummyAccum:0.0}]");
    }
}
