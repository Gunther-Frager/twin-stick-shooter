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