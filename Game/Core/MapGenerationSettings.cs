namespace TwinStickShooter.Core
{
    public sealed class MapGenerationSettings
    {
        public int Seed { get; set; } = 20260928;
        public int MaxGenerationAttempts { get; set; } = GameConstants.MaxGenerationAttempts;
        public int MinimumSpawnExitPathLength { get; set; } = (int)GameConstants.MinSpawnExitDistance;
        public int CrawlerMaxSteps { get; set; } = 650;
        public int CrawlerRoomInterval { get; set; } = 35;
        public int MinPrefabRooms { get; set; } = 2;
        public int MaxPrefabRooms { get; set; } = 8;
        public int SpawnRoomMinWidth { get; set; } = GameConstants.SpawnRoomMinWidth;
        public int EncounterDifficultyBudget { get; set; } = 8;
        public int EncounterSwarmerCost { get; set; } = 1;
        public int EncounterRoamerCost { get; set; } = 2;
        public int EncounterTurretCost { get; set; } = 3;
        public int EncounterMinGroupSize { get; set; } = 3;
        public int EncounterMaxGroupSize { get; set; } = 4;
        public float EncounterClusterRadius { get; set; } = 3f;
        public float EncounterSpawnSafeDistance { get; set; } = 8f;
        public int EncounterSpawnerCost { get; set; } = 6;
    }
}