using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace TwinStickShooter.Core
{
    /// <summary>
    /// Representa una plantilla de sala pre-diseñada que puede ser insertada en un mapa.
    /// </summary>
    public class RoomTemplateData
    {
        public string Id { get; set; }
        public int MinSize { get; set; }
        public int MaxSize { get; set; }
        public string[] Grid { get; set; }
        public List<Point> EnemySpawns { get; set; }
        public List<EnemySpawn> TypedEnemySpawns { get; set; }

        public readonly struct EnemySpawn
        {
            public EnemySpawn(Point position, EnemyType type)
            {
                Position = position;
                Type = type;
            }

            public Point Position { get; }
            public EnemyType Type { get; }
        }

        /// <summary>
        /// Intenta obtener si una celda específica de la grilla es una pared.
        /// </summary>
        /// <param name="localX">Coordenada X relativa al inicio de la sala.</param>
        /// <param name="localY">Coordenada Y relativa al inicio de la sala.</param>
        /// <param name="isWall">Devuelve true si es una pared ('1'), false si es piso ('0').</param>
        /// <returns>True si las coordenadas están dentro del rango de la grilla, false si están fuera.</returns>
        public bool TryGetCell(int localX, int localY, out bool isWall)
        {
            isWall = false;

            if (Grid == null || localY < 0 || localY >= Grid.Length)
            {
                return false;
            }

            string row = Grid[localY];
            if (localX < 0 || localX >= row.Length)
            {
                return false;
            }

            isWall = row[localX] == '1';
            return true;
        }

        /// <summary>
        /// Clase auxiliar para la deserialización de los puntos de spawn de enemigos desde JSON.
        /// </summary>
        internal class EnemySpawnData
        {
            public int x { get; set; }
            public int y { get; set; }
            public string type { get; set; }
        }

        /// <summary>
        /// Método interno para mapear los datos de deserialización a la lista de Points.
        /// </summary>
        internal void MapEnemySpawns(List<EnemySpawnData> data)
        {
            EnemySpawns = new List<Point>();
            TypedEnemySpawns = new List<EnemySpawn>();
            if (data == null) return;

            foreach (var spawn in data)
            {
                Point position = new Point(spawn.x, spawn.y);
                EnemyType type = ParseEnemyType(spawn.type);
                EnemySpawns.Add(position);
                TypedEnemySpawns.Add(new EnemySpawn(position, type));
            }
        }

        private static EnemyType ParseEnemyType(string value)
        {
            if (Enum.TryParse(value, true, out EnemyType type) &&
                Enum.IsDefined(typeof(EnemyType), type))
            {
                return type;
            }

            return EnemyType.Swarmer;
        }
    }
}
