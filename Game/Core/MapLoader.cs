using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using System;
using System.IO;
using System.Text.Json;

namespace TwinStickShooter.Core
{
    /// <summary>
    /// Carga mapas desde archivos JSON. Formato esperado:
    /// {
    ///   "width": 30,
    ///   "height": 30,
    ///   "grid": [0, 0, 1, 0, ...]
    /// }
    /// Donde 0 = vacío, 1 = pared.
    /// </summary>
    public static class MapLoader
    {
        public static void LoadMap(LevelManager levelManager, ContentManager content, string mapPath)
        {
            // Cargar el archivo JSON usando la ruta relativa al ejecutable
            string fullPath = Path.Combine(AppContext.BaseDirectory, "Content", mapPath + ".json");
            Console.WriteLine($"[MapLoader] AppContext.BaseDirectory: {AppContext.BaseDirectory}");
            Console.WriteLine($"[MapLoader] Intentando cargar: {fullPath}");
            
            if (!File.Exists(fullPath))
            {
                Console.WriteLine("[MapLoader] ERROR: Archivo no encontrado en " + fullPath);
                return;
            }
            
            try 
            {
                string json = File.ReadAllText(fullPath);
                var mapData = JsonSerializer.Deserialize<MapData>(json);
                Console.WriteLine($"[MapLoader] Mapa cargado: {mapData.width}x{mapData.height}");

                for (int y = 0; y < mapData.height; y++)
                {
                    for (int x = 0; x < mapData.width; x++)
                    {
                        int index = y * mapData.width + x;
                        if (index < mapData.grid.Length)
                        {
                            levelManager.SetCollision(x, y, mapData.grid[index] == 1);
                        }
                    }
                }

                // Almacenar los marcadores de spawn y salida en el LevelManager
                if (mapData.spawn != null)
                {
                    Point spawnPoint = ValidateSpawnPoint(levelManager, mapData.spawn.x, mapData.spawn.y);
                    levelManager.SetSpawnPosition(spawnPoint.X, spawnPoint.Y);
                }
                if (mapData.exit != null)
                {
                    Point exitPoint = ValidateSpawnPoint(levelManager, mapData.exit.x, mapData.exit.y);
                    levelManager.SetExitPosition(exitPoint.X, exitPoint.Y);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("[MapLoader] ERROR al procesar JSON: " + ex.Message);
            }
        }

        public static void GenerateProceduralMap(LevelManager levelManager)
        {
            levelManager.GenerateProceduralMap();
        }

        private class MapData
        {
            public int width { get; set; }
            public int height { get; set; }
            public int[] grid { get; set; }
            public SpawnData spawn { get; set; }
            public ExitData exit { get; set; }
        }

        private class SpawnData
        {
            public int x { get; set; }
            public int y { get; set; }
        }

        private class ExitData
        {
            public int x { get; set; }
            public int y { get; set; }
        }

        /// <summary>
        /// Valida que la posición de spawn o salida no esté en una celda con colisión.
        /// Si lo está, busca la celda vacía más cercana.
        /// </summary>
        private static Point ValidateSpawnPoint(LevelManager levelManager, int x, int y)
        {
            // Verificar si la posición inicial es válida
            if (x >= 0 && x < GameConstants.GridWidth && y >= 0 && y < GameConstants.GridHeight)
            {
                Vector2 testPosition = levelManager.GridToWorld(new Point(x, y));
                if (!levelManager.CheckCollision(testPosition, 1f))
                {
                    return new Point(x, y);
                }
            }

            // Si la posición no es válida, buscar la celda vacía más cercana
            for (int radius = 1; radius < Math.Max(GameConstants.GridWidth, GameConstants.GridHeight); radius++)
            {
                for (int i = -radius; i <= radius; i++)
                {
                    for (int j = -radius; j <= radius; j++)
                    {
                        int newX = x + i;
                        int newY = y + j;
                        if (newX >= 0 && newX < GameConstants.GridWidth && newY >= 0 && newY < GameConstants.GridHeight)
                        {
                            Vector2 testPosition = levelManager.GridToWorld(new Point(newX, newY));
                            if (!levelManager.CheckCollision(testPosition, 1f))
                            {
                                return new Point(newX, newY);
                            }
                        }
                    }
                }
            }

            // Si no se encuentra ninguna celda vacía, devolver la posición original (aunque sea inválida)
            Console.WriteLine("[MapLoader] ADVERTENCIA: No se encontró una celda vacía para spawn/salida. Usando posición original.");
            return new Point(x, y);
        }
    }
}