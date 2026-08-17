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
        public const bool AutoShootOnAim = false; // Por defecto off: solo dispara con R2/Espacio/Mouse

        // --- Balas (Fase 2: pooling) ---
        public const int MaxBullets = 128;
        public const float BulletSpeed = 640f;        // px/seg
        public const float BulletLifetimeSeconds = 1.2f;
        public const float BulletRadius = 4f;
        public const float ShootCooldownSeconds = 0.12f; // ~8.3 disparos/seg

        // --- Partículas (Fase 2: struct-based, sin pooling de objetos) ---
        public const int MaxParticles = 512;
        public const float ThrusterParticleLifeSeconds = 0.35f;
        public const float ThrusterParticleSize = 5f;
        public const float ThrusterEmitIntervalSeconds = 0.03f;
        public const float MuzzleParticleLifeSeconds = 0.15f;
        public const float MuzzleParticleSize = 6f;

        // Colores por índice de jugador (estilo neón).
        public static readonly Color[] PlayerColors =
        {
            new Color(0, 255, 220),   // P1 - Cyan neón
            new Color(255, 40, 140),  // P2 - Magenta neón
            new Color(255, 220, 0),   // P3 - Amarillo neón
            new Color(120, 80, 255),  // P4 - Violeta neón
        };

        // --- Mundo (Fase 3: mundo más grande que la pantalla) ---
        public const int WorldWidth = 2400;
        public const int WorldHeight = 2400;

        // --- Cámara (Fase 3: dinámica, sigue centro de masa + zoom automático) ---
        public const float CameraZoomMin = 0.35f;   // Máximo alejamiento
        public const float CameraZoomMax = 1.0f;    // Máximo acercamiento (1:1)
        public const float CameraLerpFactor = 0.12f; // Suavizado de movimiento/zoom
        public const float CameraPadding = 100f;    // Margen extra para que los jugadores no queden pegados al borde

        // --- Grilla de Colisiones (Fase 4: laberinto y colisiones) ---
        public const int GridWidth = 30;            // Coincide con el JSON
        public const int GridHeight = 30;           // Coincide con el JSON
        public const int GridCellSize = WorldWidth / GridWidth;  // 2400/30 = 80 píxeles por celda
        
        // --- Generación de mapas ---
        public const int MaxGenerationAttempts = 20; // Intentos máximos para generar un mapa transitable
        public const float MinSpawnExitDistance = 15f; // Distancia mínima en celdas entre spawn y salida
        public const int MinRoomCount = 8;
        public const int MaxRoomCount = 12;
        public const int SpawnRoomMaxSize = 3;
        public const int SpawnRoomMinWidth = 2;
        public const int SpawnRoomMaxWidth = 4; // Exclusivo para Random.Next(2, 4)
        public const int MediumRoomMinWidth = 6;
        public const int MediumRoomMaxWidth = 10; // Exclusivo para Random.Next(6, 10)
        public const float MediumRoomProbability = 0.2f;
        public const int StandardRoomMinWidth = 4;
        public const int StandardRoomMaxWidth = 7; // Exclusivo para Random.Next(4, 7)
        public const int MaxRoomPlacementAttempts = 15;
        public const int RoomPadding = 1; // Margen de overlap entre salas (+1/+2 en el Rectangle)
        public const int IslandMinRoomSize = 6; // Umbral de tamaño de sala para agregar islas
        public const int MinIslandsPerRoom = 1;
        public const int MaxIslandsPerRoom = 3; // Exclusivo para Random.Next(1, 3)
        public const int IslandEdgePadding = 2; // Distancia a los bordes de la sala para colocar islas
        public const double LargeIslandProbability = 0.4;
        public const int MinExtraLoops = 1;
        public const int MaxExtraLoops = 3; // Exclusivo para Random.Next(1, 3)
    }
}
