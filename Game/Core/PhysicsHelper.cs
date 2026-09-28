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
            Vector2 candidate = entity.Position;
            float moveLength = moveDelta.Length();
            float maxStepLength = entity.Radius > 0f ? entity.Radius * 0.5f : moveLength;
            int steps = maxStepLength > 0f
                ? (int)System.Math.Max(1d, System.Math.Ceiling(moveLength / maxStepLength))
                : 1;
            Vector2 stepDelta = moveDelta / steps;

            for (int step = 0; step < steps; step++)
            {
                candidate += stepDelta;
                for (int iteration = 0; iteration < 4; iteration++)
                {
                    if (!levelManager.TryGetPenetration(candidate, entity.Radius, out Vector2 pushOut))
                        break;

                    candidate += pushOut;
                }

                if (levelManager.TryGetPenetration(candidate, entity.Radius, out _))
                    return entity.Position;
            }

            return candidate;
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