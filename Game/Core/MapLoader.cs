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
            }
            catch (Exception ex)
            {
                Console.WriteLine("[MapLoader] ERROR al procesar JSON: " + ex.Message);
            }
        }

        private class MapData
        {
            public int width { get; set; }
            public int height { get; set; }
            public int[] grid { get; set; }
        }
    }
}