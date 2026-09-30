using Microsoft.Xna.Framework;

namespace TwinStickShooter.Core
{
    /// <summary>
    /// Representa una plantilla de sala pre-diseñada que puede ser insertada en un mapa.
    /// </summary>
    public class RoomTemplateData
    {
        public string Id { get; set; }
        public string[] Grid { get; set; }

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
    }
}
