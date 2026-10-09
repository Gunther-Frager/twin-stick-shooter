using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Xna.Framework;
using TwinStickShooter.Core;
using Xunit;

namespace TwinStickShooter.Tests;

/// <summary>
/// Golden master: captura el output determinista completo de MapGenerator para un conjunto
/// fijo de seeds. Si el snapshot no existe, lo crea y falla pidiendo revisión.
/// Si existe, compara exacto. Tres ejecuciones seguidas deben dar siempre el mismo resultado.
/// </summary>
public class GoldenMasterTests
{
    // 12 seeds: las 7 fijadas + 5 adicionales
    private static readonly int[] Seeds = new[]
    {
        1355728287, 1731, 514, 7319, 6421, 117, 9991,
        42, 2024, 777777, 31415, 100
    };

    /// <summary>Resuelve la ruta de snapshots subiendo desde AppContext.BaseDirectory hasta hallar Game.Tests.csproj.</summary>
    private static string GetSnapshotDir()
    {
        string dir = AppContext.BaseDirectory;
        while (dir != null)
        {
            string candidate = Path.Combine(dir, "Game.Tests.csproj");
            if (File.Exists(candidate))
            {
                return Path.Combine(dir, "Snapshots");
            }
            dir = Path.GetDirectoryName(dir);
        }
        // Fallback: junto al dll de salida
        return Path.Combine(AppContext.BaseDirectory, "Snapshots");
    }

    [Theory]
    [InlineData(1355728287)]
    [InlineData(1731)]
    [InlineData(514)]
    [InlineData(7319)]
    [InlineData(6421)]
    [InlineData(117)]
    [InlineData(9991)]
    [InlineData(42)]
    [InlineData(2024)]
    [InlineData(777777)]
    [InlineData(31415)]
    [InlineData(100)]
    public void GenerateMap_GoldenMaster(int seed)
    {
        string snapshotDir = GetSnapshotDir();
        string snapshotPath = Path.Combine(snapshotDir, $"golden_{seed}.txt");

        string actual = BuildSnapshot(seed);

        if (!File.Exists(snapshotPath))
        {
            Directory.CreateDirectory(snapshotDir);
            File.WriteAllText(snapshotPath, actual, Encoding.UTF8);
            Assert.Fail($"Snapshot creado: {snapshotPath}. Revísalo, súbelo al repo y vuelve a ejecutar.");
        }

        string expected = File.ReadAllText(snapshotPath, Encoding.UTF8);

        if (expected != actual)
        {
            // Muestra las primeras líneas que difieren
            string[] expLines = expected.Split('\n');
            string[] actLines = actual.Split('\n');
            int maxLines = Math.Max(expLines.Length, actLines.Length);
            var diff = new StringBuilder();
            int diffCount = 0;
            for (int i = 0; i < maxLines && diffCount < 20; i++)
            {
                string e = i < expLines.Length ? expLines[i] : "<missing>";
                string a = i < actLines.Length ? actLines[i] : "<missing>";
                if (e != a)
                {
                    diff.AppendLine($"Línea {i + 1}:");
                    diff.AppendLine($"  esperado: {e}");
                    diff.AppendLine($"  actual:   {a}");
                    diffCount++;
                }
            }
            Assert.Fail($"Snapshot difiere para seed {seed}. Primeras diferencias:\n{diff}");
        }
    }

    private static string BuildSnapshot(int seed)
    {
        var sb = new StringBuilder();
        var settings = new MapGenerationSettings { Seed = seed };
        var generator = new MapGenerator(GameConstants.GridWidth, GameConstants.GridHeight, settings);

        sb.AppendLine($"seed: {seed}");

        int[,] grid;

        try
        {
            grid = generator.GenerateMap();
        }
        catch (InvalidOperationException ex)
        {
            sb.AppendLine($"FAILURE: {ex.Message}");
            return sb.ToString();
        }

        // --- Resumen básico ---
        sb.AppendLine($"SpawnPoint: {generator.SpawnPoint.X},{generator.SpawnPoint.Y}");
        sb.AppendLine($"ExitPoint: {generator.ExitPoint.X},{generator.ExitPoint.Y}");
        sb.AppendLine($"SpawnExitPathLength: {generator.SpawnExitPathLength}");

        // --- Rooms ---
        sb.AppendLine($"Rooms.Count: {generator.Rooms.Count}");
        for (int i = 0; i < generator.Rooms.Count; i++)
        {
            Rectangle r = generator.Rooms[i];
            sb.AppendLine($"  Room[{i}]: X={r.X} Y={r.Y} W={r.Width} H={r.Height}");
        }

        // --- RoomConnections ---
        sb.AppendLine($"RoomConnections.Count: {generator.RoomConnections.Count}");
        foreach (var (From, To) in generator.RoomConnections)
        {
            sb.AppendLine($"  Connection: {From}->{To}");
        }

        // --- Regions ---
        sb.AppendLine($"Regions.Count: {generator.Regions.Count}");
        foreach (MapRegionDefinition region in generator.Regions)
        {
            Rectangle b = region.Bounds;
            sb.AppendLine($"  Region id={region.Id} kind={region.Kind} bounds=({b.X},{b.Y},{b.Width},{b.Height}) depth={region.Depth} budget={region.Budget}");
        }

        // --- Hashes estables (SHA256) de grid, ZoneMap, RegionIdMap ---
        sb.AppendLine($"GridHash: {ComputeGridHash(grid, GameConstants.GridWidth, GameConstants.GridHeight)}");
        sb.AppendLine($"ZoneMapHash: {ComputeZoneMapHash(generator.ZoneMap, GameConstants.GridWidth, GameConstants.GridHeight)}");
        sb.AppendLine($"RegionIdMapHash: {ComputeRegionIdMapHash(generator.RegionIdMap, GameConstants.GridWidth, GameConstants.GridHeight)}");

        // --- ArenaObstacles ---
        sb.AppendLine($"ArenaObstacles.Count: {generator.ArenaObstacles.Count}");
        foreach (ArenaObstacleDefinition obs in generator.ArenaObstacles)
        {
            MapCapsule c = obs.CapsuleInTiles;
            sb.AppendLine(
                $"  Obstacle: start=({c.Start.X.ToString("F4", CultureInfo.InvariantCulture)},{c.Start.Y.ToString("F4", CultureInfo.InvariantCulture)})" +
                $" end=({c.End.X.ToString("F4", CultureInfo.InvariantCulture)},{c.End.Y.ToString("F4", CultureInfo.InvariantCulture)})" +
                $" radius={c.Radius.ToString("F4", CultureInfo.InvariantCulture)}" +
                $" destructible={obs.IsDestructible}" +
                $" hp={obs.HitPoints}");
        }

        // --- EncounterDirector.Plan ---
        IReadOnlyList<EncounterSpawn> plan = EncounterDirector.Plan(
            grid,
            generator.RegionIdMap,
            generator.Regions,
            generator.SpawnPoint,
            GameConstants.GridCellSize,
            settings);

        sb.AppendLine($"EncounterPlan.Count: {plan.Count}");
        foreach (EncounterSpawn spawn in plan)
        {
            sb.AppendLine($"  Spawn: region={spawn.RegionIndex} cluster={spawn.ClusterId} pos=({spawn.Position.X},{spawn.Position.Y}) type={spawn.Type}");
        }
        sb.AppendLine($"EncounterPlanHash: {ComputeEncounterPlanHash(plan)}");

        return sb.ToString();
    }

    private static string ComputeGridHash(int[,] grid, int width, int height)
    {
        // Serializa el grid como un buffer de bytes little-endian (1 byte por celda, 0 o 1)
        byte[] buf = new byte[width * height];
        int idx = 0;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                buf[idx++] = (byte)(grid[x, y] & 0xFF);
            }
        }
        return HashBytesToHex(buf);
    }

    private static string ComputeZoneMapHash(MapZoneType[,] zoneMap, int width, int height)
    {
        byte[] buf = new byte[width * height];
        int idx = 0;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                buf[idx++] = (byte)zoneMap[x, y];
            }
        }
        return HashBytesToHex(buf);
    }

    private static string ComputeRegionIdMapHash(int[,] regionIdMap, int width, int height)
    {
        // 4 bytes por celda (int, little-endian)
        byte[] buf = new byte[width * height * 4];
        int idx = 0;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int v = regionIdMap[x, y];
                buf[idx++] = (byte)(v & 0xFF);
                buf[idx++] = (byte)((v >> 8) & 0xFF);
                buf[idx++] = (byte)((v >> 16) & 0xFF);
                buf[idx++] = (byte)((v >> 24) & 0xFF);
            }
        }
        return HashBytesToHex(buf);
    }

    private static string ComputeEncounterPlanHash(IReadOnlyList<EncounterSpawn> plan)
    {
        // Cada spawn: 4 bytes regionIndex + 4 bytes clusterId + 4 bytes posX + 4 bytes posY + 1 byte type
        byte[] buf = new byte[plan.Count * 17];
        int idx = 0;
        foreach (EncounterSpawn s in plan)
        {
            WriteInt(buf, ref idx, s.RegionIndex);
            WriteInt(buf, ref idx, s.ClusterId);
            WriteInt(buf, ref idx, s.Position.X);
            WriteInt(buf, ref idx, s.Position.Y);
            buf[idx++] = (byte)s.Type;
        }
        return HashBytesToHex(buf);
    }

    private static void WriteInt(byte[] buf, ref int idx, int v)
    {
        buf[idx++] = (byte)(v & 0xFF);
        buf[idx++] = (byte)((v >> 8) & 0xFF);
        buf[idx++] = (byte)((v >> 16) & 0xFF);
        buf[idx++] = (byte)((v >> 24) & 0xFF);
    }

    private static string HashBytesToHex(byte[] data)
    {
        byte[] hash = SHA256.HashData(data);
        var sb = new StringBuilder(hash.Length * 2);
        foreach (byte b in hash) sb.Append(b.ToString("x2"));
        return sb.ToString();
    }
}
