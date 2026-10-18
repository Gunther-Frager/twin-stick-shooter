using Microsoft.Xna.Framework;
using System;
using System.IO;
using System.Text.Json;

namespace TwinStickShooter.Core
{
    /// <summary>
    /// Permite cargar niveles 100% prefabricados desde archivos o cadenas JSON, sin ejecutar generación procedural.
    /// 
    /// Formato esperado:
    /// {
    ///   "width": 30,
    ///   "height": 30,
    ///   "grid": [0, 0, 1, 0, ...],
    ///   "spawn": { "x": 2, "y": 1 },
    ///   "exit": { "x": 25, "y": 28 }
    /// }
    /// Donde en "grid": 0 = libre/suelo, 1 = pared sólida/colisión.
    /// Requiere que "grid" contenga exactamente width * height elementos y que las dimensiones coincidan con el LevelManager.
    /// </summary>
    public static class MapLoader
    {
        /// <summary>
        /// Resuelve la ruta del archivo de mapa prefabricado y delega la deserialización a <see cref="LoadMapFromJson"/>.
        /// </summary>
        /// <param name="levelManager">Instancia del nivel destino a configurar.</param>
        /// <param name="mapPath">Ruta al archivo .json (absoluta o relativa a Content).</param>
        /// <returns>True si el mapa fue cargado correctamente; de lo contrario, false.</returns>
        public static bool LoadMap(LevelManager levelManager, string mapPath)
        {
            string fullPath = mapPath;
            if (!Path.IsPathRooted(fullPath))
            {
                if (!fullPath.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                {
                    fullPath += ".json";
                }
                fullPath = Path.Combine(AppContext.BaseDirectory, "Content", fullPath);
            }

            Console.WriteLine($"[MapLoader] AppContext.BaseDirectory: {AppContext.BaseDirectory}");
            Console.WriteLine($"[MapLoader] Intentando cargar: {fullPath}");

            if (!File.Exists(fullPath))
            {
                Console.WriteLine("[MapLoader] ERROR: Archivo no encontrado en " + fullPath);
                return false;
            }

            try
            {
                string json = File.ReadAllText(fullPath);
                return LoadMapFromJson(levelManager, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine("[MapLoader] ERROR al leer archivo de mapa: " + ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Carga y valida la estructura de un nivel prefabricado a partir de una cadena JSON, configurando colisiones,
        /// malla de primitivas, spawn y exit en el <see cref="LevelManager"/> sin invocar generación procedural.
        /// </summary>
        /// <param name="levelManager">Nivel donde volcar las celdas y configuración.</param>
        /// <param name="json">Cadena con el JSON a deserializar.</param>
        /// <returns>True si el formato y dimensiones son válidos; false ante cualquier inconsistencia o error.</returns>
        internal static bool LoadMapFromJson(LevelManager levelManager, string json)
        {
            if (levelManager == null || string.IsNullOrWhiteSpace(json))
            {
                Console.WriteLine("[MapLoader] ERROR: Parámetros inválidos (levelManager nulo o json vacío).");
                return false;
            }

            MapData mapData;
            try
            {
                mapData = JsonSerializer.Deserialize<MapData>(json);
            }
            catch (Exception ex)
            {
                Console.WriteLine("[MapLoader] ERROR al procesar JSON: " + ex.Message);
                return false;
            }

            if (mapData == null)
            {
                Console.WriteLine("[MapLoader] ERROR: El JSON deserializado es nulo.");
                return false;
            }

            if (mapData.grid == null)
            {
                Console.WriteLine("[MapLoader] ERROR: El mapa no contiene un array 'grid'.");
                return false;
            }

            if (mapData.width != levelManager.GridWidth || mapData.height != levelManager.GridHeight)
            {
                Console.WriteLine($"[MapLoader] ERROR: Dimensiones inconsistentes. Esperado: {levelManager.GridWidth}x{levelManager.GridHeight}, obtenido: {mapData.width}x{mapData.height}.");
                return false;
            }

            if (mapData.grid.Length != mapData.width * mapData.height)
            {
                Console.WriteLine($"[MapLoader] ERROR: El tamaño del grid ({mapData.grid.Length}) no coincide con width*height ({mapData.width * mapData.height}).");
                return false;
            }

            Console.WriteLine($"[MapLoader] Mapa cargado: {mapData.width}x{mapData.height}");

            for (int y = 0; y < mapData.height; y++)
            {
                for (int x = 0; x < mapData.width; x++)
                {
                    int index = y * mapData.width + x;
                    levelManager.SetCollision(x, y, mapData.grid[index] == 1);
                }
            }

            levelManager.RebuildPrimitiveMapFromGrid();

            if (mapData.spawn != null)
            {
                Point spawnPoint = ValidateSpawnPoint(levelManager, mapData.spawn.x, mapData.spawn.y);
                levelManager.SetSpawnPosition(spawnPoint.X, spawnPoint.Y);
            }
            else
            {
                Console.WriteLine("[MapLoader] ERROR: El mapa no define un punto de 'spawn'.");
            }

            if (mapData.exit != null)
            {
                Point exitPoint = ValidateSpawnPoint(levelManager, mapData.exit.x, mapData.exit.y);
                levelManager.SetExitPosition(exitPoint.X, exitPoint.Y);
            }
            else
            {
                Console.WriteLine("[MapLoader] ERROR: El mapa no define un punto de 'exit'.");
            }

            return true;
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