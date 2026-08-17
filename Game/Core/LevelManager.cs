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
        private Point _spawnPosition;
        private Point _exitPosition;

        public LevelManager(int gridWidth, int gridHeight, int cellSize)
        {
            _gridWidth = gridWidth;
            _gridHeight = gridHeight;
            _cellSize = cellSize;
            _collisionGrid = new bool[gridWidth, gridHeight];
            _mapGenerator = new MapGenerator(gridWidth, gridHeight, cellSize);
            _mapGenerator.Initialize();
        }

        /// <summary>
        /// Establece una celda como colisionable.
        /// </summary>
        public void SetCollision(int x, int y, bool collides)
        {
            if (x >= 0 && x < _gridWidth && y >= 0 && y < _gridHeight)
            {
                _collisionGrid[x, y] = collides;
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
        /// Verifica si una posición en el mundo es transitable (no colisiona con paredes).
        /// Wrapper de CheckCollision invertido para mayor legibilidad.
        /// </summary>
        public bool IsWalkable(Vector2 worldPosition, float radius)
        {
            return !CheckCollision(worldPosition, radius);
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
        /// Establece la posición de spawn del jugador.
        /// </summary>
        public void SetSpawnPosition(int x, int y)
        {
            _spawnPosition = new Point(x, y);
        }

        /// <summary>
        /// Establece la posición de salida del nivel.
        /// </summary>
        public void SetExitPosition(int x, int y)
        {
            _exitPosition = new Point(x, y);
        }

        /// <summary>
        /// Obtiene la posición de spawn del jugador en coordenadas del mundo.
        /// </summary>
        public Vector2 GetSpawnPosition()
        {
            return GridToWorld(_spawnPosition);
        }

        /// <summary>
        /// Obtiene la posición de salida del nivel en coordenadas del mundo.
        /// </summary>
        public Vector2 GetExitPosition()
        {
            return GridToWorld(_exitPosition);
        }

        /// <summary>
        /// Verifica si el jugador ha alcanzado el marcador de salida.
        /// </summary>
        public bool CheckExitReached(Vector2 playerPosition)
        {
            Vector2 exitWorldPosition = GridToWorld(_exitPosition);
            return Vector2.Distance(playerPosition, exitWorldPosition) < _cellSize;
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

            _spawnPosition = _mapGenerator.SpawnPoint;
            _exitPosition = _mapGenerator.ExitPoint;
        }
    }
}