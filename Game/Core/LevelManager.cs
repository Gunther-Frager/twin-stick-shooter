using Microsoft.Xna.Framework;
using System;

namespace TwinStickShooter.Core
{
    /// <summary>
    /// Gestión de mapas y colisiones por grilla. Carga mapas desde JSON/Grid
    /// y proporciona métodos para verificar colisiones en O(1).
    /// </summary>
    public class LevelManager
    {
        private readonly int _gridWidth;
        private readonly int _gridHeight;
        private readonly int _cellSize;
        private readonly bool[,] _collisionGrid;
        private MapGenerator _mapGenerator;

        public LevelManager(int gridWidth, int gridHeight, int cellSize)
        {
            _gridWidth = gridWidth;
            _gridHeight = gridHeight;
            _cellSize = cellSize;
            _collisionGrid = new bool[gridWidth, gridHeight];
            _mapGenerator = new MapGenerator(gridWidth, gridHeight, cellSize);
        }

        /// <summary>
        /// Establece una celda como colisionable.
        /// </summary>
        public void SetCollision(int x, int y, bool collides)
        {
            if (x >= 0 && x < _gridWidth && y >= 0 && y < _gridHeight)
            {
                _collisionGrid[x, y] = collides;
                System.Diagnostics.Debug.WriteLine("[LevelManager] Celda marcada como colisionable: (" + x + ", " + y + ") = " + collides);
            }
        }

        /// <summary>
        /// Verifica si una posición en el mundo colisiona con la grilla.
        /// </summary>
        public bool CheckCollision(Vector2 position, float radius)
        {
            int minX = (int)Math.Floor((position.X - radius) / _cellSize);
            int maxX = (int)Math.Floor((position.X + radius) / _cellSize);
            int minY = (int)Math.Floor((position.Y - radius) / _cellSize);
            int maxY = (int)Math.Floor((position.Y + radius) / _cellSize);

            for (int x = minX; x <= maxX; x++)
            {
                for (int y = minY; y <= maxY; y++)
                {
                    if (x >= 0 && x < _gridWidth && y >= 0 && y < _gridHeight && _collisionGrid[x, y])
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Convierte una posición en el mundo a coordenadas de grilla.
        /// </summary>
        public Point WorldToGrid(Vector2 worldPosition)
        {
            return new Point(
                (int)Math.Floor(worldPosition.X / _cellSize),
                (int)Math.Floor(worldPosition.Y / _cellSize)
            );
        }

        /// <summary>
        /// Convierte coordenadas de grilla a posición en el mundo.
        /// </summary>
        public Vector2 GridToWorld(Point gridPosition)
        {
            return new Vector2(
                gridPosition.X * _cellSize + _cellSize / 2f,
                gridPosition.Y * _cellSize + _cellSize / 2f
            );
        }

        /// <summary>
        /// Genera un mapa procedural y lo carga en el LevelManager.
        /// </summary>
        public void GenerateProceduralMap()
        {
            int[,] generatedGrid = _mapGenerator.GenerateMap();
            for (int x = 0; x < _gridWidth; x++)
            {
                for (int y = 0; y < _gridHeight; y++)
                {
                    _collisionGrid[x, y] = generatedGrid[x, y] == 1;
                }
            }
        }
    }
}