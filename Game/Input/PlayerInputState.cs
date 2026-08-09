using Microsoft.Xna.Framework;

namespace TwinStickShooter.Input
{
    /// <summary>
    /// Estado de input de UN jugador en UN frame.
    /// Es un struct a propósito: vive en un array fijo (MaxPlayers) y se
    /// sobreescribe cada frame, sin generar basura para el GC.
    /// </summary>
    public struct PlayerInputState
    {
        public bool IsConnected;

        public Vector2 MoveDirection;   // stick izquierdo, normalizado (0 si en deadzone)
        public Vector2 AimDirection;    // stick derecho, normalizado (0 si en deadzone)

        public bool IsShooting;         // gatillo derecho o magnitud del stick derecho
        public bool ShieldHeld;         // botón de escudo (A / Cross)

        public bool ShieldPressedThisFrame; // flanco de subida, útil para lógica de "activar una vez"
    }
}
