using Microsoft.Xna.Framework;

namespace TwinStickShooter.Core
{
    /// <summary>
    /// Helper estático para lógica de física y movimiento.
    /// Centraliza colisiones, deslizamiento en paredes y límites del mundo
    /// para evitar duplicación de código en entidades.
    /// </summary>
    public static class PhysicsHelper
    {
        /// <summary>
        /// Mueve una entidad y resuelve colisiones con deslizamiento en paredes.
        /// </summary>
        /// <param name="entity">Entidad a mover (debe implementar IEntity).</param>
        /// <param name="moveDelta">Desplazamiento solicitado.</param>
        /// <param name="levelManager">Instancia de LevelManager para verificar colisiones.</param>
        /// <returns>Posición final después de resolver colisiones.</returns>
        public static Vector2 MoveWithCollision(IEntity entity, Vector2 moveDelta, LevelManager levelManager)
        {
            Vector2 newPosition = entity.Position + moveDelta;
            
            // Verificar colisiones por componente (X e Y) para permitir "deslizar" en paredes
            bool collisionX = levelManager.CheckCollision(new Vector2(newPosition.X, entity.Position.Y), entity.Radius);
            bool collisionY = levelManager.CheckCollision(new Vector2(entity.Position.X, newPosition.Y), entity.Radius);

            if (collisionX && collisionY)
            {
                Vector2 safeX = FindLastSafePosition(entity.Position, new Vector2(moveDelta.X, 0f), entity.Radius, levelManager);
                Vector2 safeY = FindLastSafePosition(entity.Position, new Vector2(0f, moveDelta.Y), entity.Radius, levelManager);
                return Vector2.DistanceSquared(entity.Position, safeX) >= Vector2.DistanceSquared(entity.Position, safeY)
                    ? safeX
                    : safeY;
            }
            
            if (collisionX)
            {
                newPosition.X = entity.Position.X; // No mover en X si hay colisión
            }
            
            if (collisionY)
            {
                newPosition.Y = entity.Position.Y; // No mover en Y si hay colisión
            }
            
            return newPosition;
        }

        private static Vector2 FindLastSafePosition(Vector2 start, Vector2 axisDelta, float radius, LevelManager levelManager)
        {
            float safeFraction = 0f;
            float blockedFraction = 1f;

            for (int i = 0; i < 10; i++)
            {
                float fraction = (safeFraction + blockedFraction) * 0.5f;
                Vector2 candidate = start + axisDelta * fraction;
                if (levelManager.CheckCollision(candidate, radius))
                {
                    blockedFraction = fraction;
                }
                else
                {
                    safeFraction = fraction;
                }
            }

            return start + axisDelta * safeFraction;
        }
        
        /// <summary>
        /// Restringe la posición de una entidad dentro de los límites del mundo.
        /// </summary>
        public static Vector2 ClampToWorld(Vector2 position, float radius)
        {
            position.X = MathHelper.Clamp(position.X, radius, GameConstants.WorldWidth - radius);
            position.Y = MathHelper.Clamp(position.Y, radius, GameConstants.WorldHeight - radius);
            return position;
        }
    }
}