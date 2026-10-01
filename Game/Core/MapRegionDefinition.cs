using Microsoft.Xna.Framework;

namespace TwinStickShooter.Core
{
    public enum MapRegionKind : byte
    {
        Spawn,
        Arena,
        Pocket,
    }

    /// <summary>Describe la identidad y profundidad de una región del mapa.</summary>
    public sealed class MapRegionDefinition
    {
        public MapRegionDefinition(int id, MapRegionKind kind, Rectangle bounds, int depth, int budget)
        {
            Id = id;
            Kind = kind;
            Bounds = bounds;
            Depth = depth;
            Budget = budget;
        }

        /// <summary>ID estable que coincide con el índice de su sala en el grafo.</summary>
        public int Id { get; }

        /// <summary>Clasifica la región como spawn, arena o pocket lateral.</summary>
        public MapRegionKind Kind { get; }

        /// <summary>Límites en celdas; el interior transitable se consulta en RegionIdMap.</summary>
        public Rectangle Bounds { get; }

        /// <summary>Cantidad de conexiones desde el spawn según BFS del grafo de salas.</summary>
        public int Depth { get; }

        /// <summary>Presupuesto fijo de encuentro asignado al generar el mapa.</summary>
        public int Budget { get; }
    }
}