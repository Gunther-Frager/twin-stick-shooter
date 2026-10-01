namespace TwinStickShooter.Core
{
    public sealed class MapGenerationSettings
    {
        public int Seed { get; set; } = 1355728287;
        public int MaxGenerationAttempts { get; set; } = GameConstants.MaxGenerationAttempts;
        public int MinimumSpawnExitPathLength { get; set; } = 23;
        public int CrawlerCount { get; set; } = 4;
        public int CrawlerMaxSteps { get; set; } = 400;
        public int CrawlerCorridorWidth { get; set; } = 1;
        public int CrawlerRoomInterval { get; set; } = 25;
        public int MinArenaCount { get; set; } = 2;
        public int MaxArenaCount { get; set; } = 40;
        public int MinPrefabRooms { get => MinArenaCount; set => MinArenaCount = value; }
        public int MaxPrefabRooms { get => MaxArenaCount; set => MaxArenaCount = value; }
        public int SpawnRoomMinWidth { get; set; } = GameConstants.SpawnRoomMinWidth;
        public int ArenaRadiusMin { get; set; } = 4;
        public int ArenaRadiusMax { get; set; } = 5;
        public int ArenaObstacleMinSeeds { get; set; } = 6;
        public int ArenaObstacleMaxSeeds { get; set; } = 9;
        public float ArenaObstacleWallClearanceTiles { get; set; } = 2f;
        public float ArenaObstacleMinSpacingTiles { get; set; } = 2f;
        public float ArenaSolidObstacleChance { get; set; } = 0.25f;
        public float ArenaSolidCoreClusterChance { get; set; } = 0.35f;
        public int ArenaObstacleHitPoints { get; set; } = 5;
        public int EncounterDifficultyBudget { get; set; } = 18;
        /// <summary>Capacidad fija preasignada para enemigos y generadores de todas las regiones.</summary>
        public int EnemyPoolCapacity { get; set; } = GameConstants.MaxEnemies;
        // El budget de arena crece por nivel de profundidad en el grafo.
        public int EncounterBudgetPerDepth { get; set; } = 2;
        // Los pockets son encuentros más pequeños, independientes de su arena vecina.
        public int EncounterPocketDifficultyBudget { get; set; } = 6;
        // Probabilidad por celda de pasillo elegible, limitada por MaxPocketCount.
        public float PocketChance { get; set; } = 0.2f;
        public int MaxPocketCount { get; set; } = 8;
        public int PocketRadiusMin { get; set; } = 2;
        public int PocketRadiusMax { get; set; } = 3;
        public int EncounterSwarmerCost { get; set; } = 1;
        public int EncounterRusherCost { get; set; } = 1;
        public int EncounterRoamerCost { get; set; } = 1;
        public int EncounterTurretCost { get; set; } = 3;
        public int EncounterStaticShooterCost { get; set; } = 3;
        public int EncounterMobileGeneratorCost { get; set; } = 10;
        public int EncounterMinGroupSize { get; set; } = 3;
        public int EncounterMaxGroupSize { get; set; } = 12;
        public float EncounterClusterRadius { get; set; } = 3f;
        public float EncounterSpawnSafeDistance { get; set; } = 8f;
        public int EncounterSpawnerCost { get; set; } = 6;
    }
}