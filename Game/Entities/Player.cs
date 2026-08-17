using Microsoft.Xna.Framework;
using TwinStickShooter.Core;
using TwinStickShooter.Input;

namespace TwinStickShooter.Entities
{
    /// <summary>
    /// Representa a un jugador en el mundo. En Fase 1 solo maneja
    /// posición, ángulo de apuntado y color; sin vida/combate todavía.
    /// </summary>
    public class Player : IEntity
    {
        public readonly int Index;
        public Color Color { get; set; }

        public Vector2 Position { get; set; }
        public float Radius => GameConstants.PlayerRadius;
        public bool Active { get => IsActive; set => IsActive = value; }

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
            
            if (levelManager != null)
            {
                Position = PhysicsHelper.MoveWithCollision(this, moveDelta, levelManager);
            }
            else
            {
                Position += moveDelta;
            }
            
            Position = PhysicsHelper.ClampToWorld(Position, Radius);

            if (input.AimDirection != Vector2.Zero)
            {
                FacingAngle = (float)System.Math.Atan2(input.AimDirection.Y, input.AimDirection.X);
            }

            ShieldActive = input.ShieldHeld;
        }


    }
}
