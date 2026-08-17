using Microsoft.Xna.Framework;
using System;
using TwinStickShooter.Entities;
using TwinStickShooter.Input;

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
        /// <param name="players">Array de todos los jugadores.</param>
        /// <param name="inputManager">Manager de input para verificar conexión.</param>
        public void Update(Player[] players, InputManager inputManager)
        {
            if (players == null || inputManager == null)
                return;

            // 1. Calcular centro de masa y límites (min/max) en un solo bucle sin allocations
            Vector2 centerOfMass = Vector2.Zero;
            int activeCount = 0;

            float minX = float.MaxValue;
            float maxX = float.MinValue;
            float minY = float.MaxValue;
            float maxY = float.MinValue;

            for (int i = 0; i < players.Length; i++)
            {
                Player player = players[i];
                if (player != null && player.IsActive && inputManager.GetState(i).IsConnected)
                {
                    Vector2 pos = player.Position;
                    centerOfMass += pos;
                    activeCount++;

                    if (pos.X < minX) minX = pos.X;
                    if (pos.X > maxX) maxX = pos.X;
                    if (pos.Y < minY) minY = pos.Y;
                    if (pos.Y > maxY) maxY = pos.Y;
                }
            }

            if (activeCount == 0)
                return;

            centerOfMass /= activeCount;

            // 2. Calcular zoom necesario para que todos los jugadores entren en pantalla con padding
            // Evitar división por cero si todos los jugadores están en la misma posición
            float requiredWidth = (maxX - minX) + (2 * GameConstants.CameraPadding);
            float requiredHeight = (maxY - minY) + (2 * GameConstants.CameraPadding);
            
            if (requiredWidth <= 0) requiredWidth = GameConstants.ScreenWidth;
            if (requiredHeight <= 0) requiredHeight = GameConstants.ScreenHeight;

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