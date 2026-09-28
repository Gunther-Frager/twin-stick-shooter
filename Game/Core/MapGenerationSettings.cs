namespace TwinStickShooter.Core
{
    public sealed class MapGenerationSettings
    {
        public int Seed { get; set; } = 20260928;
        public int MaxGenerationAttempts { get; set; } = GameConstants.MaxGenerationAttempts;
        public int MinimumSpawnExitPathLength { get; set; } = (int)GameConstants.MinSpawnExitDistance;
        public int MinRoomCount { get; set; } = GameConstants.MinRoomCount;
        public int MaxRoomCount { get; set; } = GameConstants.MaxRoomCount;
        public int SpawnRoomMinWidth { get; set; } = GameConstants.SpawnRoomMinWidth;
        public int SpawnRoomMaxWidth { get; set; } = GameConstants.SpawnRoomMaxWidth;
        public int SpawnRoomMaxSize { get; set; } = GameConstants.SpawnRoomMaxSize;
        public int MediumRoomMinWidth { get; set; } = GameConstants.MediumRoomMinWidth;
        public int MediumRoomMaxWidth { get; set; } = GameConstants.MediumRoomMaxWidth;
        public double MediumRoomProbability { get; set; } = GameConstants.MediumRoomProbability;
        public double CrawlerRoomProbability { get; set; } = 0.35;
        public int CrawlerStepsPerRoomCell { get; set; } = 3;
        public int StandardRoomMinWidth { get; set; } = GameConstants.StandardRoomMinWidth;
        public int StandardRoomMaxWidth { get; set; } = GameConstants.StandardRoomMaxWidth;
        public int MaxRoomPlacementAttempts { get; set; } = GameConstants.MaxRoomPlacementAttempts;
        public int RoomPadding { get; set; } = GameConstants.RoomPadding;
        public int MinExtraLoops { get; set; } = GameConstants.MinExtraLoops;
        public int MaxExtraLoops { get; set; } = GameConstants.MaxExtraLoops;
        public bool UseRoomTemplates { get; set; } = GameConstants.UseRoomTemplates;
        public int EncounterDifficultyBudget { get; set; } = 24;
        public int EncounterSwarmerCost { get; set; } = 1;
        public int EncounterRoamerCost { get; set; } = 2;
        public int EncounterTurretCost { get; set; } = 3;
        public int EncounterMinGroupSize { get; set; } = 3;
        public int EncounterMaxGroupSize { get; set; } = 4;
        public int EncounterClusterRadius { get; set; } = 3;
        public int EncounterSpawnSafeDistance { get; set; } = 8;
    }
}