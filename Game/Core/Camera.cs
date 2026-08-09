using Microsoft.Xna.Framework;
using System;
using System.Linq;
using TwinStickShooter.Entities;

namespace TwinStickShooter.Core
{
    /// <summary>
    /// Cámara dinámica que sigue el centro de masa de los jugadores y ajusta el zoom
    /// para que todos entren en pantalla con un margen (padding).
    /// </summary>
    public class Camera
    {
        private Vector2 _position;
        private float _zoom = 1f;

        public Matrix ViewMatrix { get; private set; }

        /// <summary>
        /// Actualiza la posición y el zoom de la cámara en base a los jugadores activos.
        /// </summary>
        /// <param name="players">Lista de jugadores activos.</param>
        public void Update(Player[] players)
        {
            if (players == null || players.Length == 0)
                return;

            // 1. Calcular centro de masa (promedio de posiciones)
            Vector2 centerOfMass = Vector2.Zero;
            int activeCount = 0;
            foreach (var player in players)
            {
                if (player != null)
                {
                    centerOfMass += player.Position;
                    activeCount++;
                }
            }
            if (activeCount == 0)
                return;
            centerOfMass /= activeCount;

            // 2. Calcular zoom necesario para que todos los jugadores entren en pantalla con padding
            float minX = players.Where(p => p != null).Min(p => p.Position.X);
            float maxX = players.Where(p => p != null).Max(p => p.Position.X);
            float minY = players.Where(p => p != null).Min(p => p.Position.Y);
            float maxY = players.Where(p => p != null).Max(p => p.Position.Y);

            float requiredWidth = (maxX - minX) + (2 * GameConstants.CameraPadding);
            float requiredHeight = (maxY - minY) + (2 * GameConstants.CameraPadding);

            float zoomX = GameConstants.ScreenWidth / requiredWidth;
            float zoomY = GameConstants.ScreenHeight / requiredHeight;
            float targetZoom = MathHelper.Clamp(Math.Min(zoomX, zoomY), GameConstants.CameraZoomMin, GameConstants.CameraZoomMax);

            // 3. Interpolación suave (Lerp) para posición y zoom
            _position = Vector2.Lerp(_position, centerOfMass, GameConstants.CameraLerpFactor);
            _zoom = MathHelper.Lerp(_zoom, targetZoom, GameConstants.CameraLerpFactor);

            // 4. Construir matriz de vista
            ViewMatrix = Matrix.CreateTranslation(-_position.X, -_position.Y, 0) * 
                         Matrix.CreateScale(_zoom, _zoom, 1) *
                         Matrix.CreateTranslation(GameConstants.ScreenWidth / 2f, GameConstants.ScreenHeight / 2f, 0);
        }
    }
}