using Microsoft.Xna.Framework;

namespace TwinStickShooter.Core
{
    /// <summary>
    /// Constantes globales. Centralizadas acá para que balancear el juego
    /// no implique tocar lógica en múltiples archivos.
    /// </summary>
    public static class GameConstants
    {
        // --- Ventana / Loop ---
        public const int ScreenWidth = 1280;
        public const int ScreenHeight = 720;
        public const int TargetFps = 60;

        // --- Jugadores ---
        public const int MaxPlayers = 4;
        public const float PlayerSpeed = 260f;       // px/seg
        public const float PlayerRadius = 16f;

        // --- Input ---
        // Deadzone radial: por debajo de este magnitud, el stick se considera en reposo.
        public const float LeftStickDeadzone = 0.20f;
        public const float RightStickDeadzone = 0.25f;
        public const float TriggerShootThreshold = 0.35f;

        // Colores por índice de jugador (estilo neón).
        public static readonly Color[] PlayerColors =
        {
            new Color(0, 255, 220),   // P1 - Cyan neón
            new Color(255, 40, 140),  // P2 - Magenta neón
            new Color(255, 220, 0),   // P3 - Amarillo neón
            new Color(120, 80, 255),  // P4 - Violeta neón
        };
    }
}
