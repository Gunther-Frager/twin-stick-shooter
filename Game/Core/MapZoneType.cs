namespace TwinStickShooter.Core
{
    public enum MapZoneType : byte
    {
        Wall,       // Celda sólida del mapa.
        Corridor,   // Pasillo principal excavado por un crawler.
        ChokePoint, // Umbral de una arena o pocket.
        Arena,      // Suelo de una sala de combate.
        Pocket,     // Sala lateral pequeña conectada a un pasillo.
    }
}