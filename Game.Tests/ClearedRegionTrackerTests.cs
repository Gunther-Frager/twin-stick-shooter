using System.Collections.Generic;
using Microsoft.Xna.Framework;
using TwinStickShooter.Core;
using Xunit;

namespace TwinStickShooter.Tests;

public class ClearedRegionTrackerTests
{
    private static MapRegionDefinition CreateRegion(int id, MapRegionKind kind = MapRegionKind.Arena)
    {
        return new MapRegionDefinition(id, kind, new Rectangle(0, 0, 10, 10), depth: 1, budget: 100);
    }

    [Fact]
    public void ProcessRegions_EmptyRegionFromStart_IsMarkedCleanOnFirstCheck()
    {
        var tracker = new ClearedRegionTracker();
        var regions = new List<MapRegionDefinition>
        {
            CreateRegion(1),
            CreateRegion(2)
        };

        var clearedEvents = new List<int>();

        // Primer chequeo con 0 enemigos y 0 spawners
        int newlyCleared = tracker.ProcessRegions(
            regions,
            getEnemyAliveCount: r => 0,
            getSpawnerAliveCount: r => 0,
            enemyDeathVersion: 0,
            spawnerDeathVersion: 0,
            onRegionCleared: r => clearedEvents.Add(r.Id));

        Assert.Equal(2, newlyCleared);
        Assert.Equal(new[] { 1, 2 }, clearedEvents);
        Assert.True(tracker.IsCleared(1));
        Assert.True(tracker.IsCleared(2));
    }

    [Fact]
    public void ProcessRegions_RegionWithEnemiesOrSpawners_NotMarkedUntilAllDie()
    {
        var tracker = new ClearedRegionTracker();
        var regions = new List<MapRegionDefinition> { CreateRegion(1) };
        var clearedEvents = new List<int>();

        int enemies = 2;
        int spawners = 1;
        int enemyDeathVersion = 0;
        int spawnerDeathVersion = 0;

        // Frame 1: enemigos y spawners vivos
        int count1 = tracker.ProcessRegions(
            regions,
            r => enemies,
            r => spawners,
            enemyDeathVersion,
            spawnerDeathVersion,
            r => clearedEvents.Add(r.Id));

        Assert.Equal(0, count1);
        Assert.False(tracker.IsCleared(1));

        // Frame 2: muere 1 enemigo, pero queda 1 enemigo y 1 spawner
        enemies--;
        enemyDeathVersion++;
        int count2 = tracker.ProcessRegions(
            regions,
            r => enemies,
            r => spawners,
            enemyDeathVersion,
            spawnerDeathVersion,
            r => clearedEvents.Add(r.Id));

        Assert.Equal(0, count2);
        Assert.False(tracker.IsCleared(1));

        // Frame 3: muere el segundo enemigo, pero queda el spawner
        enemies--;
        enemyDeathVersion++;
        int count3 = tracker.ProcessRegions(
            regions,
            r => enemies,
            r => spawners,
            enemyDeathVersion,
            spawnerDeathVersion,
            r => clearedEvents.Add(r.Id));

        Assert.Equal(0, count3);
        Assert.False(tracker.IsCleared(1));

        // Frame 4: muere el spawner -> ahora la región está totalmente limpia
        spawners--;
        spawnerDeathVersion++;
        int count4 = tracker.ProcessRegions(
            regions,
            r => enemies,
            r => spawners,
            enemyDeathVersion,
            spawnerDeathVersion,
            r => clearedEvents.Add(r.Id));

        Assert.Equal(1, count4);
        Assert.True(tracker.IsCleared(1));
        Assert.Equal(new[] { 1 }, clearedEvents);
    }

    [Fact]
    public void ProcessRegions_AfterMarkedCleared_DoesNotEmitAgainEvenWhenVersionsChange()
    {
        var tracker = new ClearedRegionTracker();
        var regions = new List<MapRegionDefinition> { CreateRegion(1) };
        var clearedEvents = new List<int>();

        // Primer chequeo: limpia
        tracker.ProcessRegions(
            regions,
            r => 0,
            r => 0,
            enemyDeathVersion: 0,
            spawnerDeathVersion: 0,
            r => clearedEvents.Add(r.Id));

        Assert.Single(clearedEvents);

        // Chequeos posteriores con nuevas muertes en otras regiones
        for (int v = 1; v <= 5; v++)
        {
            int newlyCleared = tracker.ProcessRegions(
                regions,
                r => 0,
                r => 0,
                enemyDeathVersion: v,
                spawnerDeathVersion: v,
                r => clearedEvents.Add(r.Id));

            Assert.Equal(0, newlyCleared);
        }

        Assert.Single(clearedEvents);
    }

    [Fact]
    public void ShouldCheck_WithoutDeathVersionChanges_ReturnsFalse()
    {
        var tracker = new ClearedRegionTracker();

        // Inicialmente dirty = true
        Assert.True(tracker.ShouldCheck(0, 0));

        tracker.RecordCheck(0, 0);

        // Sin cambios en versiones
        Assert.False(tracker.ShouldCheck(0, 0));

        // Con cambio en enemy death version
        Assert.True(tracker.ShouldCheck(1, 0));

        // Con cambio en spawner death version
        Assert.True(tracker.ShouldCheck(0, 1));
    }
}
