using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Microsoft.Xna.Framework.Content;

namespace TwinStickShooter.Core
{
    /// <summary>
    /// Carga plantillas de sala desde archivos JSON en la carpeta Content/RoomTemplates/.
    /// Las plantillas se usan para insertar patrones pre-diseñados en los mapas generados proceduralmente.
    /// </summary>
    public static class RoomTemplateLoader
    {
        /// <summary>
        /// Carga todas las plantillas de sala disponibles en Content/RoomTemplates/.
        /// </summary>
        /// <param name="content">ContentManager de MonoGame (no se usa directamente, pero se incluye para consistencia con otros loaders).</param>
        /// <returns>Lista de plantillas cargadas correctamente. Si la carpeta no existe o está vacía, devuelve una lista vacía.</returns>
        public static List<RoomTemplateData> LoadAll(ContentManager content)
        {
            List<RoomTemplateData> templates = new List<RoomTemplateData>();
            string templatesPath = Path.Combine(AppContext.BaseDirectory, "Content", "RoomTemplates");
            
            if (!Directory.Exists(templatesPath))
            {
                Console.WriteLine($"[RoomTemplateLoader] ADVERTENCIA: La carpeta {templatesPath} no existe.");
                return templates;
            }
            
            string[] jsonFiles = Directory.GetFiles(templatesPath, "*.json");
            if (jsonFiles.Length == 0)
            {
                Console.WriteLine($"[RoomTemplateLoader] ADVERTENCIA: No se encontraron archivos JSON en {templatesPath}.");
                return templates;
            }
            
            foreach (string file in jsonFiles)
            {
                try
                {
                    string json = File.ReadAllText(file);
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                    };
                    
                    // Deserializar a una clase intermedia para manejar el mapeo de enemySpawns
                    var templateData = JsonSerializer.Deserialize<RoomTemplateDataIntermediate>(json, options);
                    if (templateData == null)
                    {
                        Console.WriteLine($"[RoomTemplateLoader] ERROR en {file}: No se pudo deserializar el JSON.");
                        continue;
                    }
                    
                    // Mapear a la clase final
                    var roomTemplate = new RoomTemplateData
                    {
                        Id = templateData.Id,
                        MinSize = templateData.MinSize,
                        MaxSize = templateData.MaxSize,
                        Grid = templateData.Grid,
                    };
                    roomTemplate.MapEnemySpawns(templateData.EnemySpawns);
                    
                    templates.Add(roomTemplate);
                    Console.WriteLine($"[RoomTemplateLoader] Plantilla cargada: {templateData.Id} desde {file}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[RoomTemplateLoader] ERROR en {file}: {ex.Message}");
                }
            }
            
            return templates;
        }
        
        /// <summary>
        /// Clase intermedia para deserializar el JSON y manejar el mapeo de enemySpawns.
        /// </summary>
        private class RoomTemplateDataIntermediate
        {
            public string Id { get; set; }
            public int MinSize { get; set; }
            public int MaxSize { get; set; }
            public string[] Grid { get; set; }
            public List<RoomTemplateData.EnemySpawnData> EnemySpawns { get; set; }
        }
    }
}