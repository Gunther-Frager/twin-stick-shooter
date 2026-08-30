using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TwinStickShooter.Core
{
    /// <summary>
    /// Gestión de mapas y colisiones. Mantiene la grilla legacy como base de juego,
    /// pero añade una representación geométrica primitiva para render y físicas suaves.
    /// </summary>
    public class LevelManager
    {
        private readonly int _gridWidth;
        private readonly int _gridHeight;
        private readonly int _cellSize;
        private readonly bool[,] _collisionGrid;
        private readonly List<MapCircle> _primitiveCircles = new List<MapCircle>();
        private readonly List<MapCapsule> _primitiveCapsules = new List<MapCapsule>();
        private MapGenerator _mapGenerator;
        public MapGenerator MapGenerator => _mapGenerator;
        private Point _spawnPosition;
        private Point _exitPosition;

        public LevelManager(int gridWidth, int gridHeight, int cellSize)
        {
            _gridWidth = gridWidth;
            _gridHeight = gridHeight;
            _cellSize = cellSize;
            _collisionGrid = new bool[gridWidth, gridHeight];
            _mapGenerator = new MapGenerator(gridWidth, gridHeight, cellSize);

            if (gridWidth >= 8 && gridHeight >= 8)
            {
                _mapGenerator.Initialize();
            }
        }

        public IReadOnlyList<MapCircle> PrimitiveCircles => _primitiveCircles;
        public IReadOnlyList<MapCapsule> PrimitiveCapsules => _primitiveCapsules;
        public bool HasPrimitiveMapData() => _primitiveCircles.Count > 0 || _primitiveCapsules.Count > 0;

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

        public void ConfigureCombatTestArena()
        {
            for (int x = 0; x < _gridWidth; x++)
            {
                for (int y = 0; y < _gridHeight; y++)
                {
                    _collisionGrid[x, y] = x == 0 || y == 0 || x == _gridWidth - 1 || y == _gridHeight - 1;
                }
            }

            SetSpawnPosition(_gridWidth / 2, _gridHeight / 2);
            SetExitPosition(_gridWidth - 2, _gridHeight - 2);
        }

        /// <summary>
        /// Verifica si una posición en el mundo colisiona con la grilla o con
        /// obstáculos primitivos durante la transición al mapa orgánico.
        /// </summary>
        public bool CheckCollision(Vector2 position, float radius)
        {
            if (HasPrimitiveMapData())
            {
                foreach (var circle in _primitiveCircles)
                {
                    if (Vector2.DistanceSquared(position, circle.Center) <= (radius + circle.Radius) * (radius + circle.Radius))
                    {
                        return true;
                    }
                }

                foreach (var capsule in _primitiveCapsules)
                {
                    Vector2 closest = ClosestPointOnSegment(position, capsule.Start, capsule.End);
                    if (Vector2.DistanceSquared(position, closest) <= (radius + capsule.Radius) * (radius + capsule.Radius))
                    {
                        return true;
                    }
                }

                return false;
            }

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

        public void SetRoomTemplates(System.Collections.Generic.List<RoomTemplateData> templates)
        {
            _mapGenerator.SetRoomTemplates(templates);
        }

        public void SetPrimitiveMap(MapDefinition definition)
        {
            _primitiveCircles.Clear();
            _primitiveCapsules.Clear();

            if (definition == null)
            {
                return;
            }

            if (definition.Circles != null)
            {
                _primitiveCircles.AddRange(definition.Circles);
            }

            if (definition.Capsules != null)
            {
                _primitiveCapsules.AddRange(definition.Capsules);
            }

            if (definition.Obstacles != null)
            {
                foreach (var obstacle in definition.Obstacles)
                {
                    if (obstacle?.Circle != null)
                    {
                        _primitiveCircles.Add(obstacle.Circle);
                    }

                    if (obstacle?.Capsule != null)
                    {
                        _primitiveCapsules.Add(obstacle.Capsule);
                    }
                }
            }
        }

        public void RebuildPrimitiveMapFromGrid()
        {
            _primitiveCircles.Clear();
            _primitiveCapsules.Clear();

            var visited = new bool[_gridWidth, _gridHeight];
            var queue = new Queue<Point>();

            for (int x = 0; x < _gridWidth; x++)
            {
                for (int y = 0; y < _gridHeight; y++)
                {
                    if (!_collisionGrid[x, y] || visited[x, y])
                    {
                        continue;
                    }

                    bool isWorldBorder = _gridWidth > 2 && _gridHeight > 2 &&
                        (x == 0 || y == 0 || x == _gridWidth - 1 || y == _gridHeight - 1);

                    if (isWorldBorder)
                    {
                        continue;
                    }

                    var region = new List<Point>();
                    queue.Enqueue(new Point(x, y));
                    visited[x, y] = true;

                    while (queue.Count > 0)
                    {
                        Point current = queue.Dequeue();
                        region.Add(current);

                        for (int dx = -1; dx <= 1; dx++)
                        {
                            for (int dy = -1; dy <= 1; dy++)
                            {
                                if (Math.Abs(dx) == Math.Abs(dy))
                                {
                                    continue;
                                }

                                int nx = current.X + dx;
                                int ny = current.Y + dy;
                                if (nx >= 0 && nx < _gridWidth && ny >= 0 && ny < _gridHeight && _collisionGrid[nx, ny] && !visited[nx, ny])
                                {
                                    visited[nx, ny] = true;
                                    queue.Enqueue(new Point(nx, ny));
                                }
                            }
                        }
                    }

                    BuildRegionPrimitives(region);
                }
            }
        }

        private void BuildRegionPrimitives(List<Point> region)
        {
            if (region.Count == 0)
            {
                return;
            }

            if (region.Count == 1)
            {
                var cell = region[0];
                _primitiveCircles.Add(new MapCircle
                {
                    Center = CellCenter(cell.X, cell.Y),
                    Radius = _cellSize * 0.38f
                });
                return;
            }

            var byRow = new Dictionary<int, List<int>>();
            var byColumn = new Dictionary<int, List<int>>();

            foreach (var cell in region)
            {
                if (!byRow.TryGetValue(cell.Y, out var rowCells))
                {
                    rowCells = new List<int>();
                    byRow[cell.Y] = rowCells;
                }
                rowCells.Add(cell.X);

                if (!byColumn.TryGetValue(cell.X, out var columnCells))
                {
                    columnCells = new List<int>();
                    byColumn[cell.X] = columnCells;
                }
                columnCells.Add(cell.Y);
            }

            foreach (var row in byRow)
            {
                var xs = row.Value.OrderBy(x => x).ToList();
                int start = xs[0];
                int previous = xs[0];

                for (int i = 1; i < xs.Count; i++)
                {
                    if (xs[i] == previous + 1)
                    {
                        previous = xs[i];
                        continue;
                    }

                    if (previous - start >= 1)
                    {
                        var first = new Vector2(start * _cellSize + _cellSize * 0.5f, row.Key * _cellSize + _cellSize * 0.5f);
                        var last = new Vector2(previous * _cellSize + _cellSize * 0.5f, row.Key * _cellSize + _cellSize * 0.5f);
                        _primitiveCapsules.Add(new MapCapsule
                        {
                            Start = first,
                            End = last,
                            Radius = _cellSize * 0.32f
                        });
                    }
                    else
                    {
                        var c = new Vector2(start * _cellSize + _cellSize * 0.5f, row.Key * _cellSize + _cellSize * 0.5f);
                        _primitiveCircles.Add(new MapCircle { Center = c, Radius = _cellSize * 0.38f });
                    }

                    start = xs[i];
                    previous = xs[i];
                }

                if (xs.Count > 0 && previous - start >= 1)
                {
                    var first = new Vector2(start * _cellSize + _cellSize * 0.5f, row.Key * _cellSize + _cellSize * 0.5f);
                    var last = new Vector2(previous * _cellSize + _cellSize * 0.5f, row.Key * _cellSize + _cellSize * 0.5f);
                    _primitiveCapsules.Add(new MapCapsule
                    {
                        Start = first,
                        End = last,
                        Radius = _cellSize * 0.32f
                    });
                }
                else if (xs.Count > 0)
                {
                    var c = new Vector2(xs[0] * _cellSize + _cellSize * 0.5f, row.Key * _cellSize + _cellSize * 0.5f);
                    _primitiveCircles.Add(new MapCircle { Center = c, Radius = _cellSize * 0.38f });
                }
            }

            foreach (var col in byColumn)
            {
                var ys = col.Value.OrderBy(y => y).ToList();
                int start = ys[0];
                int previous = ys[0];

                for (int i = 1; i < ys.Count; i++)
                {
                    if (ys[i] == previous + 1)
                    {
                        previous = ys[i];
                        continue;
                    }

                    if (previous - start >= 1)
                    {
                        var first = new Vector2(col.Key * _cellSize + _cellSize * 0.5f, start * _cellSize + _cellSize * 0.5f);
                        var last = new Vector2(col.Key * _cellSize + _cellSize * 0.5f, previous * _cellSize + _cellSize * 0.5f);
                        _primitiveCapsules.Add(new MapCapsule
                        {
                            Start = first,
                            End = last,
                            Radius = _cellSize * 0.32f
                        });
                    }

                    start = ys[i];
                    previous = ys[i];
                }

                if (ys.Count > 0 && previous - start >= 1)
                {
                    var first = new Vector2(col.Key * _cellSize + _cellSize * 0.5f, start * _cellSize + _cellSize * 0.5f);
                    var last = new Vector2(col.Key * _cellSize + _cellSize * 0.5f, previous * _cellSize + _cellSize * 0.5f);
                    _primitiveCapsules.Add(new MapCapsule
                    {
                        Start = first,
                        End = last,
                        Radius = _cellSize * 0.32f
                    });
                }
            }
        }

        private Vector2 CellCenter(int x, int y)
        {
            return new Vector2(x * _cellSize + _cellSize * 0.5f, y * _cellSize + _cellSize * 0.5f);
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

            RebuildPrimitiveMapFromGrid();
            _spawnPosition = _mapGenerator.SpawnPoint;
            _exitPosition = _mapGenerator.ExitPoint;
        }

        private static Vector2 ClosestPointOnSegment(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float denom = ab.LengthSquared();
            if (denom < 0.0001f)
            {
                return a;
            }

            float t = Vector2.Dot(p - a, ab) / denom;
            t = MathHelper.Clamp(t, 0f, 1f);
            return a + ab * t;
        }
    }
}