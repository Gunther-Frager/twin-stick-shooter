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
        private const float PenetrationSeparationEpsilon = 0.001f;
        private readonly int _gridWidth;
        private readonly int _gridHeight;
        private readonly int _cellSize;
        private readonly bool[,] _collisionGrid;
        private readonly List<MapCircle> _primitiveCircles = new List<MapCircle>();
        private readonly List<MapCapsule> _primitiveCapsules = new List<MapCapsule>();
        private readonly List<MapRoundedPoly> _primitiveRoundedPolys = new List<MapRoundedPoly>();
        private bool _primitiveMapInitialized;
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
        public bool UseRoundedContours { get; set; } = true;
        public IReadOnlyList<MapRoundedPoly> PrimitiveRoundedPolys => _primitiveRoundedPolys;
        public bool HasPrimitiveMapData() => _primitiveMapInitialized;

        /// <summary>
        /// Establece una celda como colisionable.
        /// </summary>
        public void SetCollision(int x, int y, bool collides)
        {
            if (x >= 0 && x < _gridWidth && y >= 0 && y < _gridHeight)
            {
                _collisionGrid[x, y] = collides;
                _primitiveMapInitialized = false;
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
            RebuildPrimitiveMapFromGrid();
        }

        /// <summary>
        /// Verifica si una posición en el mundo colisiona con la grilla o con
        /// obstáculos primitivos durante la transición al mapa orgánico.
        /// </summary>
        public bool CheckCollision(Vector2 position, float radius)
        {
            if (!HasPrimitiveMapData())
            {
                throw new InvalidOperationException("Primitive map data is not initialized. Call RebuildPrimitiveMapFromGrid or SetPrimitiveMap before checking collisions.");
            }

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

            foreach (var poly in _primitiveRoundedPolys)
            {
                if (RoundedContour.PenetrationAgainst(poly, position, radius, out _))
                    return true;
            }

            return false;
        }

        public bool TryGetPenetration(Vector2 center, float radius, out Vector2 pushOut)
        {
            if (!HasPrimitiveMapData())
            {
                throw new InvalidOperationException("Primitive map data is not initialized. Call RebuildPrimitiveMapFromGrid or SetPrimitiveMap before checking collisions.");
            }

            pushOut = Vector2.Zero;
            float deepestOverlap = 0f;

            foreach (var circle in _primitiveCircles)
            {
                float combinedRadius = radius + circle.Radius;
                Vector2 offset = center - circle.Center;
                float distanceSquared = offset.LengthSquared();
                if (distanceSquared >= combinedRadius * combinedRadius)
                    continue;

                float distance = MathF.Sqrt(distanceSquared);
                float overlap = combinedRadius - distance;
                Vector2 candidate = distance > 0.0001f
                    ? offset * ((overlap + PenetrationSeparationEpsilon) / distance)
                    : new Vector2(overlap + PenetrationSeparationEpsilon, 0f);
                if (overlap > deepestOverlap)
                {
                    deepestOverlap = overlap;
                    pushOut = candidate;
                }
            }

            foreach (var capsule in _primitiveCapsules)
            {
                Vector2 closest = ClosestPointOnSegment(center, capsule.Start, capsule.End);
                Vector2 offset = center - closest;
                float distanceSquared = offset.LengthSquared();
                float combinedRadius = radius + capsule.Radius;
                if (distanceSquared >= combinedRadius * combinedRadius)
                    continue;

                float distance = MathF.Sqrt(distanceSquared);
                float overlap = combinedRadius - distance;
                Vector2 axis = capsule.End - capsule.Start;
                Vector2 fallback = axis.LengthSquared() > 0.0001f
                    ? Vector2.Normalize(new Vector2(-axis.Y, axis.X))
                    : Vector2.UnitX;
                Vector2 candidate = distance > 0.0001f
                    ? offset * ((overlap + PenetrationSeparationEpsilon) / distance)
                    : fallback * (overlap + PenetrationSeparationEpsilon);
                if (overlap > deepestOverlap)
                {
                    deepestOverlap = overlap;
                    pushOut = candidate;
                }
            }

            foreach (var poly in _primitiveRoundedPolys)
            {
                if (!RoundedContour.PenetrationAgainst(poly, center, radius, out Vector2 candidate))
                    continue;

                float overlap = candidate.Length();
                if (overlap > deepestOverlap)
                {
                    deepestOverlap = overlap;
                    pushOut = candidate + Vector2.Normalize(candidate) * PenetrationSeparationEpsilon;
                }
            }

            return deepestOverlap > 0f;
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

        public void SetSpawnPosition(int x, int y)
        {
            _spawnPosition = new Point(x, y);
        }

        public void SetExitPosition(int x, int y)
        {
            _exitPosition = new Point(x, y);
        }

        public Vector2 GetSpawnPosition()
        {
            return GridToWorld(_spawnPosition);
        }

        public Vector2 GetExitPosition()
        {
            return GridToWorld(_exitPosition);
        }

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
            _primitiveRoundedPolys.Clear();
            _primitiveMapInitialized = true;

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
            _primitiveRoundedPolys.Clear();

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
                                bool isNeighborWorldBorder = _gridWidth > 2 && _gridHeight > 2 &&
                                    (nx == 0 || ny == 0 || nx == _gridWidth - 1 || ny == _gridHeight - 1);
                                if (!isNeighborWorldBorder && nx >= 0 && nx < _gridWidth && ny >= 0 && ny < _gridHeight && _collisionGrid[nx, ny] && !visited[nx, ny])
                                {
                                    visited[nx, ny] = true;
                                    queue.Enqueue(new Point(nx, ny));
                                }
                            }
                        }
                    }

                    if (UseRoundedContours && region.Count > 1)
                    {
                        List<List<Vector2>> contours = RoundedContour.TraceContours(
                            (cellX, cellY) => _collisionGrid[cellX, cellY], region, _cellSize);
                        float cornerRadius = _cellSize * GameConstants.RoundedContourRadiusScale;
                        foreach (List<Vector2> contour in contours)
                            _primitiveRoundedPolys.Add(RoundedContour.RoundCorners(contour, cornerRadius));
                    }
                    else
                    {
                        BuildRegionPrimitives(region);
                    }
                }
            }

            BuildBoundaryPrimitives();
            _primitiveMapInitialized = true;
        }

        private void BuildBoundaryPrimitives()
        {
            if (_gridWidth < 3 || _gridHeight < 3)
            {
                return;
            }

            AddHorizontalBoundaryPrimitives(0);
            AddHorizontalBoundaryPrimitives(_gridHeight - 1);
            AddVerticalBoundaryPrimitives(0);
            AddVerticalBoundaryPrimitives(_gridWidth - 1);

            AddBoundaryCorner(0, 0);
            AddBoundaryCorner(_gridWidth - 1, 0);
            AddBoundaryCorner(0, _gridHeight - 1);
            AddBoundaryCorner(_gridWidth - 1, _gridHeight - 1);
        }

        private void AddHorizontalBoundaryPrimitives(int y)
        {
            int x = 1;
            while (x < _gridWidth - 1)
            {
                if (!_collisionGrid[x, y])
                {
                    x++;
                    continue;
                }

                int startX = x;
                while (x + 1 < _gridWidth - 1 && _collisionGrid[x + 1, y])
                {
                    x++;
                }

                Vector2 start = CellCenter(startX, y);
                Vector2 end = CellCenter(x, y);
                AddBoundaryRun(start, end);
                x++;
            }
        }

        private void AddVerticalBoundaryPrimitives(int x)
        {
            int y = 1;
            while (y < _gridHeight - 1)
            {
                if (!_collisionGrid[x, y])
                {
                    y++;
                    continue;
                }

                int startY = y;
                while (y + 1 < _gridHeight - 1 && _collisionGrid[x, y + 1])
                {
                    y++;
                }

                Vector2 start = CellCenter(x, startY);
                Vector2 end = CellCenter(x, y);
                AddBoundaryRun(start, end);
                y++;
            }
        }

        private void AddBoundaryRun(Vector2 start, Vector2 end)
        {
            float radius = _cellSize * 0.5f;
            if (start == end)
            {
                _primitiveCircles.Add(new MapCircle { Center = start, Radius = radius });
                return;
            }

            _primitiveCapsules.Add(new MapCapsule { Start = start, End = end, Radius = radius });
        }

        private void AddBoundaryCorner(int x, int y)
        {
            if (_collisionGrid[x, y])
            {
                _primitiveCircles.Add(new MapCircle
                {
                    Center = CellCenter(x, y),
                    Radius = _cellSize * 0.72f
                });
            }
        }

        private void BuildRegionPrimitives(List<Point> region)
        {
            var remaining = new HashSet<Point>(region);
            var orderedCells = region.OrderBy(cell => cell.Y).ThenBy(cell => cell.X);

            foreach (Point start in orderedCells)
            {
                if (!remaining.Contains(start))
                {
                    continue;
                }

                int width = 1;
                while (remaining.Contains(new Point(start.X + width, start.Y)))
                {
                    width++;
                }

                int height = 1;
                while (Enumerable.Range(start.X, width)
                    .All(x => remaining.Contains(new Point(x, start.Y + height))) &&
                    !remaining.Contains(new Point(start.X - 1, start.Y + height)) &&
                    !remaining.Contains(new Point(start.X + width, start.Y + height)))
                {
                    height++;
                }

                foreach (int x in Enumerable.Range(start.X, width))
                {
                    foreach (int y in Enumerable.Range(start.Y, height))
                    {
                        remaining.Remove(new Point(x, y));
                    }
                }

                int minorSide = Math.Min(width, height);
                Vector2 center = new Vector2(
                    (start.X + width * 0.5f) * _cellSize,
                    (start.Y + height * 0.5f) * _cellSize);

                if (Math.Abs(width - height) <= 1)
                {
                    _primitiveCircles.Add(new MapCircle
                    {
                        Center = center,
                        Radius = minorSide * _cellSize * 0.38f
                    });
                    continue;
                }

                Vector2 capsuleStart;
                Vector2 capsuleEnd;
                if (width > height)
                {
                    capsuleStart = new Vector2((start.X + 0.5f) * _cellSize, center.Y);
                    capsuleEnd = new Vector2((start.X + width - 0.5f) * _cellSize, center.Y);
                }
                else
                {
                    capsuleStart = new Vector2(center.X, (start.Y + 0.5f) * _cellSize);
                    capsuleEnd = new Vector2(center.X, (start.Y + height - 0.5f) * _cellSize);
                }

                _primitiveCapsules.Add(new MapCapsule
                {
                    Start = capsuleStart,
                    End = capsuleEnd,
                    Radius = minorSide * _cellSize * 0.32f
                });
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