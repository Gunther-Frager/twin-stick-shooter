using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using TwinStickShooter.Core;
using Xunit;

namespace TwinStickShooter.Tests;

public class MapLoaderTests
{
    [Fact]
    public void LoadMapFromJson_WithValidMinimumLevel_SetsCollisionSpawnExitAndRebuildsPrimitives()
    {
        // Nivel 8x8 con borde perimetral de paredes y suelo interior
        int width = 8;
        int height = 8;
        int[] grid = new int[width * height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (x == 0 || y == 0 || x == width - 1 || y == height - 1)
                {
                    grid[y * width + x] = 1;
                }
                else
                {
                    grid[y * width + x] = 0;
                }
            }
        }

        string json = $@"{{
            ""width"": {width},
            ""height"": {height},
            ""grid"": [{string.Join(",", grid)}],
            ""spawn"": {{ ""x"": 2, ""y"": 2 }},
            ""exit"": {{ ""x"": 5, ""y"": 5 }}
        }}";

        // Incluimos un obstáculo interior en (3,3) para probar primitivas interiores
        grid[3 * width + 3] = 1;
        json = $@"{{
            ""width"": {width},
            ""height"": {height},
            ""grid"": [{string.Join(",", grid)}],
            ""spawn"": {{ ""x"": 2, ""y"": 2 }},
            ""exit"": {{ ""x"": 5, ""y"": 5 }}
        }}";

        var levelManager = new LevelManager(width, height, 40);
        int initialRevision = levelManager.PrimitiveMapRevision;

        bool success = MapLoader.LoadMapFromJson(levelManager, json);

        Assert.True(success);
        Assert.True(levelManager.HasPrimitiveMapData());
        Assert.True(levelManager.PrimitiveMapRevision > initialRevision);

        // Validar spawn y exit
        Vector2 expectedSpawn = levelManager.GridToWorld(new Point(2, 2));
        Vector2 expectedExit = levelManager.GridToWorld(new Point(5, 5));
        Assert.Equal(expectedSpawn, levelManager.GetSpawnPosition());
        Assert.Equal(expectedExit, levelManager.GetExitPosition());

        // Comprobar colisiones: celda interior en (3,3) colisiona, spawn libre en (2,2)
        Vector2 wallInside = levelManager.GridToWorld(new Point(3, 3));
        Assert.True(levelManager.CheckCollision(wallInside, 1f));
        Assert.False(levelManager.CheckCollision(expectedSpawn, 1f));
    }

    [Fact]
    public void LoadMapFromJson_WithMismatchedDimensions_ReturnsFalseWithoutThrowing()
    {
        var levelManager = new LevelManager(10, 10, 40);
        string json = @"{
            ""width"": 8,
            ""height"": 8,
            ""grid"": [0],
            ""spawn"": { ""x"": 1, ""y"": 1 },
            ""exit"": { ""x"": 2, ""y"": 2 }
        }";

        bool success = MapLoader.LoadMapFromJson(levelManager, json);

        Assert.False(success);
    }

    [Fact]
    public void LoadMapFromJson_WithShortGrid_ReturnsFalseWithoutThrowing()
    {
        var levelManager = new LevelManager(8, 8, 40);
        // Declaramos 8x8 (64 elementos esperados), pero enviamos solo 10
        string json = @"{
            ""width"": 8,
            ""height"": 8,
            ""grid"": [0, 1, 0, 1, 0, 1, 0, 1, 0, 1],
            ""spawn"": { ""x"": 1, ""y"": 1 },
            ""exit"": { ""x"": 2, ""y"": 2 }
        }";

        bool success = MapLoader.LoadMapFromJson(levelManager, json);

        Assert.False(success);
    }

    [Fact]
    public void LoadMapFromJson_WithMalformedJson_ReturnsFalseWithoutThrowing()
    {
        var levelManager = new LevelManager(8, 8, 40);
        string brokenJson = "{ \"width\": 8, \"height\": 8, \"grid\": [0, 1, broken json ";

        bool success = MapLoader.LoadMapFromJson(levelManager, brokenJson);

        Assert.False(success);
    }

    [Fact]
    public void LoadMap_WithRealExamplePrefabFile_LoadsSuccessfullyAndSpawnCanReachExit()
    {
        var levelManager = new LevelManager(GameConstants.GridWidth, GameConstants.GridHeight, GameConstants.GridCellSize);

        // Cargamos el archivo example_prefab.json desde la ruta de salida de Content
        string prefabPath = Path.Combine(AppContext.BaseDirectory, "Content", "Maps", "example_prefab.json");
        Assert.True(File.Exists(prefabPath), $"El archivo de prefab real no se encontró en: {prefabPath}");

        bool success = MapLoader.LoadMap(levelManager, "Maps/example_prefab");
        Assert.True(success);

        // Validar alcanzabilidad entre spawn y exit mediante BFS sobre la grilla
        Vector2 spawnWorld = levelManager.GetSpawnPosition();
        Vector2 exitWorld = levelManager.GetExitPosition();

        Point start = levelManager.WorldToGrid(spawnWorld);
        Point target = levelManager.WorldToGrid(exitWorld);

        bool reachable = IsReachable(levelManager, start, target);
        Assert.True(reachable, $"Se esperaba que el spawn ({start.X},{start.Y}) pudiera alcanzar la salida ({target.X},{target.Y})");
    }

    private static bool IsReachable(LevelManager levelManager, Point start, Point target)
    {
        int width = levelManager.GridWidth;
        int height = levelManager.GridHeight;
        bool[,] visited = new bool[width, height];
        var queue = new Queue<Point>();

        queue.Enqueue(start);
        visited[start.X, start.Y] = true;

        Point[] neighbors =
        {
            new Point(0, -1),
            new Point(0, 1),
            new Point(-1, 0),
            new Point(1, 0)
        };

        while (queue.Count > 0)
        {
            Point current = queue.Dequeue();
            if (current == target)
            {
                return true;
            }

            foreach (Point dir in neighbors)
            {
                int nx = current.X + dir.X;
                int ny = current.Y + dir.Y;

                if (nx >= 0 && nx < width && ny >= 0 && ny < height && !visited[nx, ny])
                {
                    visited[nx, ny] = true;
                    Vector2 worldPos = levelManager.GridToWorld(new Point(nx, ny));
                    if (!levelManager.CheckCollision(worldPos, 1f))
                    {
                        queue.Enqueue(new Point(nx, ny));
                    }
                }
            }
        }

        return false;
    }
}

