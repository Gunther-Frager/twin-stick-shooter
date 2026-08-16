using Microsoft.Xna.Framework;
using TwinStickShooter.Core;
using TwinStickShooter.Input;

namespace TwinStickShooter.Entities
{
    /// <summary>
    /// Representa a un jugador en el mundo. En Fase 1 solo maneja
    /// posición, ángulo de apuntado y color; sin vida/combate todavía.
    /// </summary>
    public class Player
    {
        public readonly int Index;
        public readonly Color Color;

        public Vector2 Position;
        public float FacingAngle; // radianes, 0 = mirando a la derecha (+X)
        public bool ShieldActive;
        public bool IsActive;

        public Player(int index, Vector2 spawnPosition)
        {
            Index = index;
            Color = GameConstants.PlayerColors[index % GameConstants.PlayerColors.Length];
            Position = spawnPosition;
            FacingAngle = 0f;
        }

        public void Update(in PlayerInputState input, float deltaTime, LevelManager levelManager = null)
        {
            if (!input.IsConnected)
            {
                return;
            }

            Vector2 moveDelta = input.MoveDirection * GameConstants.PlayerSpeed * deltaTime;
            Vector2 newPosition = Position + moveDelta;
            
            // Verificar colisiones por componente (X e Y) para permitir "deslizar" en paredes
            if (levelManager != null)
            {
                bool collisionX = levelManager.CheckCollision(new Vector2(newPosition.X, Position.Y), GameConstants.PlayerRadius);
                bool collisionY = levelManager.CheckCollision(new Vector2(Position.X, newPosition.Y), GameConstants.PlayerRadius);
                
                if (collisionX)
                {
                    newPosition.X = Position.X; // No mover en X si hay colisión
                }
                
                if (collisionY)
                {
                    newPosition.Y = Position.Y; // No mover en Y si hay colisión
                }
            }
            
            Position = newPosition;
            ClampToWorld();

            if (input.AimDirection != Vector2.Zero)
            {
                FacingAngle = (float)System.Math.Atan2(input.AimDirection.Y, input.AimDirection.X);
            }

            ShieldActive = input.ShieldHeld;
        }

        private void ClampToWorld()
        {
            float r = GameConstants.PlayerRadius;
            Position.X = MathHelper.Clamp(Position.X, r, GameConstants.WorldWidth - r);
            Position.Y = MathHelper.Clamp(Position.Y, r, GameConstants.WorldHeight - r);
        }
    }
}
