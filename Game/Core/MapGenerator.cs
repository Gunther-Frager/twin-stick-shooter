using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TwinStickShooter.Core
{
    /// <summary>
    /// Genera corredores, expande arenas y clasifica la geometría resultante.
    /// </summary>
    public class MapGenerator
    {
        private readonly int _width;
        private readonly int _height;
        private Random _random;
        private List<RoomTemplateData> _roomTemplates = new List<RoomTemplateData>();

        public MapGenerationSettings Settings { get; }
        public Point SpawnPoint { get; private set; }
        public Point ExitPoint { get; private set; }
        public int SpawnExitPathLength { get; private set; }
        public string LastGenerationFailure { get; private set; }
        public int CrawlerStepCount { get; private set; }
        public int PrefabPlacementAttempts { get; private set; }
        public int PrefabRoomsPlaced { get; private set; }
        public int ArenaRoomsPlaced => PrefabRoomsPlaced;
        public List<Rectangle> Rooms { get; private set; } = new List<Rectangle>();
        public List<(int From, int To)> RoomConnections { get; } = new List<(int From, int To)>();
        public MapZoneType[,] ZoneMap { get; private set; }
        public List<ArenaObstacleDefinition> ArenaObstacles { get; } = new List<ArenaObstacleDefinition>();

        public MapGenerator(int width, int height, MapGenerationSettings settings = null)
        {
            _width = width;
            _height = height;
            Settings = settings ?? new MapGenerationSettings();
            _random = new Random(Settings.Seed);
            ZoneMap = new MapZoneType[width, height];
        }

        public void SetRoomTemplates(List<RoomTemplateData> templates)
        {
            _roomTemplates = templates ?? new List<RoomTemplateData>();
        }

        public int[,] GenerateMap()
        {
            Point previousSpawn = SpawnPoint;
            Point previousExit = ExitPoint;
            int previousPathLength = SpawnExitPathLength;
            int previousStepCount = CrawlerStepCount;
            int previousPlacementAttempts = PrefabPlacementAttempts;
            int previousPrefabRoomsPlaced = PrefabRoomsPlaced;
            List<Rectangle> previousRooms = Rooms.ToList();
            List<(int From, int To)> previousConnections = RoomConnections.ToList();
            MapZoneType[,] previousZones = (MapZoneType[,])ZoneMap.Clone();
            List<ArenaObstacleDefinition> previousArenaObstacles = ArenaObstacles.ToList();
            int[,] grid = new int[_width, _height];
            LastGenerationFailure = null;
            int requiredArenaRooms = Math.Min(Math.Max(0, Settings.MinArenaCount), Math.Max(0, Settings.MaxArenaCount));

            for (int attempt = 0; attempt < Math.Max(1, Settings.MaxGenerationAttempts); attempt++)
            {
                _random = new Random(unchecked(Settings.Seed + attempt));
                Rooms.Clear();
                RoomConnections.Clear();
                SpawnExitPathLength = -1;
                CrawlerStepCount = 0;
                PrefabPlacementAttempts = 0;
                PrefabRoomsPlaced = 0;
                ArenaObstacles.Clear();

                // El crawler excava desde la sala inicial; los prefabs nacen conectados a su recorrido.
                for (int x = 0; x < _width; x++)
                {
                    for (int y = 0; y < _height; y++)
                    {
                        grid[x, y] = 1;
                        ZoneMap[x, y] = MapZoneType.Wall;
                    }
                }

                if (!GrowCrawlerMap(grid) || ArenaRoomsPlaced < requiredArenaRooms) continue;

                ClassifyZones(grid);
                IReadOnlyList<EncounterSpawn> encounterPlan = EncounterDirector.Plan(
                    grid,
                    Rooms,
                    SpawnPoint,
                    GameConstants.GridCellSize,
                    Settings);
                AddArenaObstacles(grid, encounterPlan);
                ClassifyZones(grid);
                ExitPoint = FindFarthestReachablePoint(grid, SpawnPoint, out int routeLength);
                SpawnExitPathLength = routeLength;
                if (SpawnExitPathLength >= Settings.MinimumSpawnExitPathLength &&
                    AreAllRoomsReachable(grid, SpawnPoint))
                {
                    return grid;
                }
            }

            LastGenerationFailure = $"No se generó un mapa válido tras {Math.Max(1, Settings.MaxGenerationAttempts)} intentos. " +
                $"Distancia mínima requerida: {Settings.MinimumSpawnExitPathLength} celdas; " +
                $"arenas mínimas: {requiredArenaRooms}; generadas: {ArenaRoomsPlaced}; " +
                $"intentos de expansión: {PrefabPlacementAttempts}; pasos: {CrawlerStepCount}.";
            SpawnPoint = previousSpawn;
            ExitPoint = previousExit;
            SpawnExitPathLength = previousPathLength;
            CrawlerStepCount = previousStepCount;
            PrefabPlacementAttempts = previousPlacementAttempts;
            PrefabRoomsPlaced = previousPrefabRoomsPlaced;
            Rooms = previousRooms;
            RoomConnections.Clear();
            RoomConnections.AddRange(previousConnections);
            ZoneMap = previousZones;
            ArenaObstacles.Clear();
            ArenaObstacles.AddRange(previousArenaObstacles);
            throw new InvalidOperationException(LastGenerationFailure);
        }

        private bool GrowCrawlerMap(int[,] grid)
        {
            if (_width < 8 || _height < 8) return false;

            int spawnRoomSize = Math.Clamp(Settings.SpawnRoomMinWidth, 2, Math.Min(4, Math.Min(_width - 4, _height - 4)));
            int spawnX = _width / 2 - spawnRoomSize / 2;
            int spawnY = _height / 2 - spawnRoomSize / 2;
            Rectangle spawnRoom = new Rectangle(spawnX, spawnY, spawnRoomSize, spawnRoomSize);
            CarveRoom(grid, spawnRoom, null);
            Rooms.Add(spawnRoom);
            SpawnPoint = spawnRoom.Center;

            Point[] directions =
            {
                new Point(0, -1), new Point(1, 0), new Point(0, 1), new Point(-1, 0),
            };
            int crawlerCount = Math.Clamp(Settings.CrawlerCount, 1, directions.Length);
            Point[] crawlerPositions = new Point[crawlerCount];
            Point[] crawlerDirections = new Point[crawlerCount];
            int[] attachedRoomIndices = new int[crawlerCount];
            bool[] activeCrawlers = new bool[crawlerCount];
            int directionOffset = _random.Next(directions.Length);
            for (int crawlerIndex = 0; crawlerIndex < crawlerCount; crawlerIndex++)
            {
                crawlerPositions[crawlerIndex] = SpawnPoint;
                crawlerDirections[crawlerIndex] = directions[(directionOffset + crawlerIndex) % directions.Length];
                activeCrawlers[crawlerIndex] = true;
            }

            int maxSteps = Math.Max(1, Settings.CrawlerMaxSteps);
            int roomInterval = Math.Max(1, Settings.CrawlerRoomInterval);
            int maxArenaRooms = Math.Max(0, Settings.MaxArenaCount);

            int remainingCrawlers = crawlerCount;
            int crawlerCursor = 0;
            for (int step = 0; step < maxSteps && remainingCrawlers > 0; step++)
            {
                bool allCrawlersExitedSpawn = true;
                for (int i = 0; i < crawlerCount; i++)
                {
                    if (activeCrawlers[i] && Rooms[0].Contains(crawlerPositions[i]))
                    {
                        allCrawlersExitedSpawn = false;
                        break;
                    }
                }

                if (CrawlerStepCount >= crawlerCount && allCrawlersExitedSpawn && ArenaRoomsPlaced >= maxArenaRooms)
                {
                    ExitPoint = FindFarthestReachablePoint(grid, SpawnPoint, out int currentPathLength);
                    SpawnExitPathLength = currentPathLength;
                    if (currentPathLength >= Settings.MinimumSpawnExitPathLength) break;
                }

                while (!activeCrawlers[crawlerCursor])
                {
                    crawlerCursor = (crawlerCursor + 1) % crawlerCount;
                }

                int currentCrawler = crawlerCursor;
                crawlerCursor = (crawlerCursor + 1) % crawlerCount;
                Point current = crawlerPositions[currentCrawler];
                Point next = Rooms[0].Contains(current)
                    ? current + crawlerDirections[currentCrawler]
                    : MoveCrawler(grid, current, ref crawlerDirections[currentCrawler], directions);
                if (next == current)
                {
                    activeCrawlers[currentCrawler] = false;
                    remainingCrawlers--;
                    continue;
                }

                crawlerPositions[currentCrawler] = next;
                CarveCrawlerCorridor(grid, next, crawlerDirections[currentCrawler], Settings.CrawlerCorridorWidth);
                CrawlerStepCount++;

                if (CrawlerStepCount % roomInterval == 0 && ArenaRoomsPlaced < maxArenaRooms)
                {
                    PrefabPlacementAttempts++;
                    if (TryExpandArena(grid, next, out Rectangle arena))
                    {
                        int roomIndex = Rooms.Count;
                        Rooms.Add(arena);
                        RoomConnections.Add((attachedRoomIndices[currentCrawler], roomIndex));
                        attachedRoomIndices[currentCrawler] = roomIndex;
                        PrefabRoomsPlaced++;
                    }
                }
            }

            return true;
        }

        private bool TryExpandArena(int[,] grid, Point center, out Rectangle arena)
        {
            int minimumRadius = Math.Clamp(Settings.ArenaRadiusMin, 3, 7);
            int maximumRadius = Math.Clamp(Settings.ArenaRadiusMax, minimumRadius, 8);
            int radiusX = _random.Next(minimumRadius, maximumRadius + 1);
            int radiusY = _random.Next(minimumRadius, maximumRadius + 1);
            Rectangle candidate = new Rectangle(center.X - radiusX, center.Y - radiusY, radiusX * 2 + 1, radiusY * 2 + 1);
            Rectangle padded = new Rectangle(candidate.X - 1, candidate.Y - 1, candidate.Width + 2, candidate.Height + 2);
            arena = Rectangle.Empty;

            if (padded.Left <= 0 || padded.Top <= 0 || padded.Right >= _width - 1 || padded.Bottom >= _height - 1)
                return false;

            foreach (Rectangle room in Rooms)
            {
                if (padded.Intersects(room)) return false;
            }

            for (int x = candidate.Left; x < candidate.Right; x++)
            {
                for (int y = candidate.Top; y < candidate.Bottom; y++)
                {
                    float normalizedX = (x - center.X) / (float)radiusX;
                    float normalizedY = (y - center.Y) / (float)radiusY;
                    float organicEdge = 0.82f + (float)_random.NextDouble() * 0.2f;
                    if (normalizedX * normalizedX + normalizedY * normalizedY > organicEdge)
                        continue;

                    grid[x, y] = 0;
                    ZoneMap[x, y] = MapZoneType.Arena;
                }
            }

            grid[center.X, center.Y] = 0;
            ZoneMap[center.X, center.Y] = MapZoneType.Arena;
            arena = candidate;
            return true;
        }

        private void ClassifyZones(int[,] grid)
        {
            List<Point> chokeCells = new List<Point>();
            Point[] directions = { new Point(0, -1), new Point(1, 0), new Point(0, 1), new Point(-1, 0) };
            for (int x = 0; x < _width; x++)
            {
                for (int y = 0; y < _height; y++)
                {
                    if (grid[x, y] != 0)
                    {
                        ZoneMap[x, y] = MapZoneType.Wall;
                        continue;
                    }

                    if (ZoneMap[x, y] != MapZoneType.Arena)
                        ZoneMap[x, y] = MapZoneType.Corridor;
                }
            }

            for (int x = 1; x < _width - 1; x++)
            {
                for (int y = 1; y < _height - 1; y++)
                {
                    if (grid[x, y] != 0 || ZoneMap[x, y] != MapZoneType.Corridor) continue;
                    for (int i = 0; i < directions.Length; i++)
                    {
                        Point neighbor = new Point(x, y) + directions[i];
                        if (ZoneMap[neighbor.X, neighbor.Y] == MapZoneType.Arena)
                        {
                            chokeCells.Add(new Point(x, y));
                            break;
                        }
                    }
                }
            }

            for (int i = 0; i < chokeCells.Count; i++)
                ZoneMap[chokeCells[i].X, chokeCells[i].Y] = MapZoneType.ChokePoint;
        }

        private void AddArenaObstacles(int[,] grid, IReadOnlyList<EncounterSpawn> encounterPlan)
        {
            int minimumSeeds = Math.Max(0, Settings.ArenaObstacleMinSeeds);
            int maximumSeeds = Math.Max(minimumSeeds, Settings.ArenaObstacleMaxSeeds);
            int wallClearance = Math.Max(2, (int)MathF.Ceiling(Settings.ArenaObstacleWallClearanceTiles));
            float minimumSpacingSquared = MathF.Max(1f, Settings.ArenaObstacleMinSpacingTiles);
            minimumSpacingSquared *= minimumSpacingSquared;

            for (int roomIndex = 1; roomIndex < Rooms.Count; roomIndex++)
            {
                Rectangle arena = Rooms[roomIndex];
                List<Point> candidates = new List<Point>();
                for (int x = arena.Left + wallClearance; x < arena.Right - wallClearance; x++)
                {
                    for (int y = arena.Top + wallClearance; y < arena.Bottom - wallClearance; y++)
                    {
                        if (grid[x, y] == 0 && ZoneMap[x, y] == MapZoneType.Arena)
                            candidates.Add(new Point(x, y));
                    }
                }

                for (int i = candidates.Count - 1; i > 0; i--)
                {
                    int swapIndex = _random.Next(i + 1);
                    (candidates[i], candidates[swapIndex]) = (candidates[swapIndex], candidates[i]);
                }

                int seedTarget = maximumSeeds == minimumSeeds
                    ? minimumSeeds
                    : _random.Next(minimumSeeds, maximumSeeds + 1);
                List<Vector2> placedCenters = new List<Vector2>();
                int placedCount = 0;
                bool makeSolidCore = seedTarget >= 5 && _random.NextDouble() < Settings.ArenaSolidCoreClusterChance;
                if (makeSolidCore && candidates.Count > 0)
                {
                    for (int candidateIndex = 0; candidateIndex < candidates.Count && placedCount == 0; candidateIndex++)
                    {
                        Point seed = candidates[candidateIndex];
                        Vector2 center = new Vector2(seed.X + 0.5f, seed.Y + 0.5f);
                        Vector2[] offsets =
                        {
                            Vector2.Zero,
                            new Vector2(-0.375f, 0f), new Vector2(0.375f, 0f),
                            new Vector2(0f, -0.375f), new Vector2(0f, 0.375f),
                        };
                        bool clusterClear = true;
                        for (int i = 0; i < offsets.Length; i++)
                        {
                            Vector2 partCenter = center + offsets[i];
                            bool horizontal = offsets[i].Y == 0f && offsets[i] != Vector2.Zero;
                            Vector2 halfSegment = horizontal ? new Vector2(0.125f, 0f) :
                                offsets[i] == Vector2.Zero ? Vector2.Zero : new Vector2(0f, 0.125f);
                            if (!IsObstacleClearFromEncounters(
                                partCenter - halfSegment, partCenter + halfSegment, 0.125f, encounterPlan))
                            {
                                clusterClear = false;
                                break;
                            }
                        }

                        if (!clusterClear) continue;

                        AddArenaObstacle(center, center, 0.125f, false);
                        placedCenters.Add(center);
                        placedCount++;
                        for (int i = 1; i < offsets.Length; i++)
                        {
                            Vector2 satellite = center + offsets[i];
                            bool horizontal = offsets[i].Y == 0f;
                            Vector2 halfSegment = horizontal ? new Vector2(0.125f, 0f) : new Vector2(0f, 0.125f);
                            AddArenaObstacle(satellite - halfSegment, satellite + halfSegment, 0.125f, true);
                            placedCenters.Add(satellite);
                            placedCount++;
                        }
                    }
                }

                for (int i = 0; i < candidates.Count && placedCount < seedTarget; i++)
                {
                    Point seed = candidates[i];
                    Vector2 center = new Vector2(seed.X + 0.5f, seed.Y + 0.5f);
                    bool tooClose = false;
                    for (int j = 0; j < placedCenters.Count; j++)
                    {
                        if (Vector2.DistanceSquared(center, placedCenters[j]) < minimumSpacingSquared)
                        {
                            tooClose = true;
                            break;
                        }
                    }
                    if (tooClose) continue;

                    int directionIndex = _random.Next(4);
                    Vector2 direction = directionIndex switch
                    {
                        0 => Vector2.UnitX,
                        1 => -Vector2.UnitX,
                        2 => Vector2.UnitY,
                        _ => -Vector2.UnitY,
                    };
                    float halfLength = _random.Next(0, 4) * 0.25f;
                    bool destructible = _random.NextDouble() >= Settings.ArenaSolidObstacleChance;
                    Vector2 start = center - direction * halfLength;
                    Vector2 end = center + direction * halfLength;
                    if (!IsObstacleClearFromEncounters(start, end, 0.125f, encounterPlan)) continue;

                    AddArenaObstacle(start, end, 0.125f, destructible);
                    placedCenters.Add(center);
                    placedCount++;
                }
            }
        }

        private void AddArenaObstacle(Vector2 startInTiles, Vector2 endInTiles, float radiusInTiles, bool destructible)
        {
            MapCapsule capsule = new MapCapsule
            {
                Start = startInTiles,
                End = endInTiles,
                Radius = radiusInTiles,
            };
            ArenaObstacles.Add(new ArenaObstacleDefinition(
                capsule,
                destructible,
                destructible ? Math.Max(1, Settings.ArenaObstacleHitPoints) : 0));
        }

        private static bool IsObstacleClearFromEncounters(
            Vector2 start,
            Vector2 end,
            float obstacleRadius,
            IReadOnlyList<EncounterSpawn> encounterPlan)
        {
            for (int i = 0; i < encounterPlan.Count; i++)
            {
                EncounterSpawn spawn = encounterPlan[i];
                Vector2 spawnCenter = new Vector2(
                    spawn.Position.X / (float)GameConstants.GridCellSize,
                    spawn.Position.Y / (float)GameConstants.GridCellSize);
                Vector2 closest = ClosestPointOnSegment(spawnCenter, start, end);
                float spawnRadius;
                switch (spawn.Type)
                {
                    case EnemyType.Spawner:
                    case EnemyType.MobileGenerator:
                        spawnRadius = GameConstants.SpawnerRadius;
                        break;
                    case EnemyType.Turret:
                    case EnemyType.StaticShooter:
                        spawnRadius = GameConstants.StaticShooterRadius;
                        break;
                    case EnemyType.Rusher:
                        spawnRadius = GameConstants.RusherRadius;
                        break;
                    default:
                        spawnRadius = GameConstants.EnemyRadius;
                        break;
                }
                float combinedRadius = obstacleRadius + spawnRadius / GameConstants.GridCellSize + 0.03f;
                if (Vector2.DistanceSquared(spawnCenter, closest) <= combinedRadius * combinedRadius)
                    return false;
            }

            return true;
        }

        private static Vector2 ClosestPointOnSegment(Vector2 point, Vector2 start, Vector2 end)
        {
            Vector2 segment = end - start;
            float lengthSquared = segment.LengthSquared();
            if (lengthSquared < 0.0001f) return start;
            float amount = Math.Clamp(Vector2.Dot(point - start, segment) / lengthSquared, 0f, 1f);
            return start + segment * amount;
        }

        private void CarveCrawlerCorridor(int[,] grid, Point center, Point direction, int width)
        {
            Point perpendicular = new Point(-direction.Y, direction.X);
            int corridorWidth = Math.Clamp(width, 1, 3);
            int firstOffset = -(corridorWidth / 2);

            for (int offset = firstOffset; offset < firstOffset + corridorWidth; offset++)
            {
                Point cell = center + perpendicular * offset;
                if (cell.X <= 0 || cell.X >= _width - 1 || cell.Y <= 0 || cell.Y >= _height - 1)
                    continue;

                bool insideRoom = false;
                foreach (Rectangle room in Rooms)
                {
                    if (room.Contains(cell))
                    {
                        insideRoom = true;
                        break;
                    }
                }

                if (!insideRoom)
                {
                    grid[cell.X, cell.Y] = 0;
                    if (ZoneMap[cell.X, cell.Y] != MapZoneType.Arena)
                        ZoneMap[cell.X, cell.Y] = MapZoneType.Corridor;
                }
            }
        }

        private Point MoveCrawler(int[,] grid, Point current, ref Point previousDirection, Point[] directions)
        {
            List<Point> unvisited = new List<Point>();
            List<Point> available = new List<Point>();
            foreach (Point direction in directions)
            {
                Point candidate = current + direction;
                if (candidate.X <= 1 || candidate.X >= _width - 2 || candidate.Y <= 1 || candidate.Y >= _height - 2)
                {
                    continue;
                }
                if (ZoneMap[candidate.X, candidate.Y] == MapZoneType.Arena) continue;

                bool insideRoom = false;
                foreach (Rectangle room in Rooms)
                {
                    if (room.Contains(candidate))
                    {
                        insideRoom = true;
                        break;
                    }
                }
                if (insideRoom && grid[candidate.X, candidate.Y] != 0) continue;

                available.Add(direction);
                if (grid[candidate.X, candidate.Y] != 0) unvisited.Add(direction);
            }

            List<Point> choices = unvisited.Count > 0 && _random.NextDouble() < 0.8 ? unvisited : available;
            if (choices.Count == 0) return current;

            Point chosen;
            if (_random.NextDouble() < 0.55 && choices.Contains(previousDirection))
            {
                chosen = previousDirection;
            }
            else
            {
                chosen = choices[_random.Next(choices.Count)];
            }

            previousDirection = chosen;
            return current + chosen;
        }

        private bool TryInsertPrefab(int[,] grid, Point crawlerPosition, Point[] directions, out Rectangle placedRoom)
        {
            placedRoom = Rectangle.Empty;
            if (_roomTemplates == null || _roomTemplates.Count == 0) return false;

            int firstTemplate = _random.Next(_roomTemplates.Count);
            if (Settings.MaxPrefabRooms <= 2)
            {
                int largestArea = -1;
                for (int templateIndex = 0; templateIndex < _roomTemplates.Count; templateIndex++)
                {
                    RoomTemplateData template = _roomTemplates[templateIndex];
                    if (template.Grid == null || template.Grid.Length == 0) continue;

                    int templateWidth = 0;
                    for (int y = 0; y < template.Grid.Length; y++)
                    {
                        if (template.Grid[y] != null)
                            templateWidth = Math.Max(templateWidth, template.Grid[y].Length);
                    }

                    int area = templateWidth * template.Grid.Length;
                    if (area > largestArea)
                    {
                        largestArea = area;
                        firstTemplate = templateIndex;
                    }
                }
            }

            for (int templateOffset = 0; templateOffset < _roomTemplates.Count; templateOffset++)
            {
                RoomTemplateData template = _roomTemplates[(firstTemplate + templateOffset) % _roomTemplates.Count];
                if (template.Grid == null || template.Grid.Length == 0) continue;
                int templateHeight = template.Grid.Length;
                int templateWidth = 0;
                for (int y = 0; y < templateHeight; y++)
                {
                    if (template.Grid[y] != null) templateWidth = Math.Max(templateWidth, template.Grid[y].Length);
                }
                if (templateWidth < 3 || templateHeight < 3) continue;

                int firstDirection = _random.Next(directions.Length);
                for (int directionOffset = 0; directionOffset < directions.Length; directionOffset++)
                {
                    Point side = directions[(firstDirection + directionOffset) % directions.Length];
                    for (int offset = 2; offset <= 4; offset++)
                    {
                        Rectangle candidate = GetPrefabBounds(crawlerPosition, side, offset, templateWidth, templateHeight);
                        if (!CanPlacePrefab(grid, candidate)) continue;

                        CarveRoom(grid, candidate, template);
                        Point door = GetRoomDoor(candidate, crawlerPosition, side);
                        Point outsideDoor = door - side;
                        ConnectRooms(grid, crawlerPosition, outsideDoor);
                        grid[door.X, door.Y] = 0;
                        placedRoom = candidate;
                        return true;
                    }
                }
            }

            return false;
        }

        private static Rectangle GetPrefabBounds(Point crawler, Point side, int offset, int width, int height)
        {
            int x = side.X > 0 ? crawler.X + offset : side.X < 0 ? crawler.X - offset - width + 1 : crawler.X - width / 2;
            int y = side.Y > 0 ? crawler.Y + offset : side.Y < 0 ? crawler.Y - offset - height + 1 : crawler.Y - height / 2;
            return new Rectangle(x, y, width, height);
        }

        private bool CanPlacePrefab(int[,] grid, Rectangle candidate)
        {
            Rectangle padded = new Rectangle(candidate.X - 1, candidate.Y - 1, candidate.Width + 2, candidate.Height + 2);
            if (padded.Left <= 0 || padded.Top <= 0 || padded.Right >= _width - 1 || padded.Bottom >= _height - 1)
            {
                return false;
            }

            foreach (Rectangle room in Rooms)
            {
                if (padded.Intersects(room)) return false;
            }

            for (int x = candidate.Left; x < candidate.Right; x++)
            {
                for (int y = candidate.Top; y < candidate.Bottom; y++)
                {
                    if (grid[x, y] == 0) return false;
                }
            }

            return true;
        }

        private static Point GetRoomDoor(Rectangle room, Point crawler, Point side)
        {
            if (side.X > 0) return new Point(room.Left, Math.Clamp(crawler.Y, room.Top + 1, room.Bottom - 2));
            if (side.X < 0) return new Point(room.Right - 1, Math.Clamp(crawler.Y, room.Top + 1, room.Bottom - 2));
            if (side.Y > 0) return new Point(Math.Clamp(crawler.X, room.Left + 1, room.Right - 2), room.Top);
            return new Point(Math.Clamp(crawler.X, room.Left + 1, room.Right - 2), room.Bottom - 1);
        }

        private static void CarveRoom(int[,] grid, Rectangle room, RoomTemplateData template)
        {
            for (int x = room.Left; x < room.Right; x++)
            {
                for (int y = room.Top; y < room.Bottom; y++)
                {
                    bool isWall = false;
                    if (template != null) template.TryGetCell(x - room.Left, y - room.Top, out isWall);
                    grid[x, y] = isWall ? 1 : 0;
                }
            }
        }

        private bool AreAllRoomsReachable(int[,] grid, Point start)
        {
            bool[,] visited = FindReachableCells(grid, start, out _);
            foreach (Rectangle room in Rooms)
            {
                bool reachableFloor = false;
                for (int x = room.Left; x < room.Right && !reachableFloor; x++)
                {
                    for (int y = room.Top; y < room.Bottom; y++)
                    {
                        if (grid[x, y] == 0 && visited[x, y])
                        {
                            reachableFloor = true;
                            break;
                        }
                    }
                }
                if (!reachableFloor) return false;
            }

            return true;
        }

        private Point FindFarthestReachablePoint(int[,] grid, Point start, out int pathLength)
        {
            bool[,] visited = FindReachableCells(grid, start, out int[,] distances);
            Point farthest = start;
            pathLength = 0;
            for (int x = 0; x < _width; x++)
            {
                for (int y = 0; y < _height; y++)
                {
                    if (visited[x, y] && distances[x, y] > pathLength)
                    {
                        pathLength = distances[x, y];
                        farthest = new Point(x, y);
                    }
                }
            }

            return farthest;
        }

        private bool[,] FindReachableCells(int[,] grid, Point start, out int[,] distances)
        {
            distances = new int[_width, _height];
            bool[,] visited = new bool[_width, _height];
            Queue<Point> queue = new Queue<Point>();
            queue.Enqueue(start);
            visited[start.X, start.Y] = true;
            Point[] directions = { new Point(0, 1), new Point(1, 0), new Point(0, -1), new Point(-1, 0) };

            while (queue.Count > 0)
            {
                Point current = queue.Dequeue();
                foreach (Point direction in directions)
                {
                    Point next = current + direction;
                    if (next.X < 0 || next.X >= _width || next.Y < 0 || next.Y >= _height ||
                        visited[next.X, next.Y] || grid[next.X, next.Y] != 0)
                    {
                        continue;
                    }

                    visited[next.X, next.Y] = true;
                    distances[next.X, next.Y] = distances[current.X, current.Y] + 1;
                    queue.Enqueue(next);
                }
            }

            return visited;
        }

        private void ConnectRooms(int[,] grid, Point start, Point end)
        {
            int x = start.X;
            int y = start.Y;

            while (x != end.X)
            {
                grid[x, y] = 0;
                x += Math.Sign(end.X - x);
            }

            while (y != end.Y)
            {
                grid[x, y] = 0;
                y += Math.Sign(end.Y - y);
            }
            grid[end.X, end.Y] = 0;
        }

        public bool IsMapTraversable(int[,] grid, Point start, Point end)
        {
            return BFS(grid, start, end);
        }

        public int FindPathDistance(int[,] grid, Point start, Point end)
        {
            if (!IsInsideGrid(start) || !IsInsideGrid(end) || grid[start.X, start.Y] != 0 || grid[end.X, end.Y] != 0)
            {
                return -1;
            }

            int[,] distances = new int[_width, _height];
            for (int x = 0; x < _width; x++)
            {
                for (int y = 0; y < _height; y++) distances[x, y] = -1;
            }

            Queue<Point> queue = new Queue<Point>();
            queue.Enqueue(start);
            distances[start.X, start.Y] = 0;
            Point[] directions = { new Point(0, 1), new Point(1, 0), new Point(0, -1), new Point(-1, 0) };

            while (queue.Count > 0)
            {
                Point current = queue.Dequeue();
                if (current == end) return distances[current.X, current.Y];

                foreach (Point direction in directions)
                {
                    int nextX = current.X + direction.X;
                    int nextY = current.Y + direction.Y;
                    if (nextX >= 0 && nextX < _width && nextY >= 0 && nextY < _height &&
                        grid[nextX, nextY] == 0 && distances[nextX, nextY] < 0)
                    {
                        distances[nextX, nextY] = distances[current.X, current.Y] + 1;
                        queue.Enqueue(new Point(nextX, nextY));
                    }
                }
            }

            return -1;
        }

        private bool IsInsideGrid(Point point)
        {
            return point.X >= 0 && point.X < _width && point.Y >= 0 && point.Y < _height;
        }

        private bool BFS(int[,] grid, Point start, Point end)
        {
            if (grid[start.X, start.Y] != 0 || grid[end.X, end.Y] != 0)
                return false;

            Queue<Point> queue = new Queue<Point>();
            bool[,] visited = new bool[_width, _height];

            queue.Enqueue(start);
            visited[start.X, start.Y] = true;

            int[,] directions = { { 0, 1 }, { 1, 0 }, { 0, -1 }, { -1, 0 } };

            while (queue.Count > 0)
            {
                Point current = queue.Dequeue();

                if (current.X == end.X && current.Y == end.Y)
                {
                    return true;
                }

                for (int i = 0; i < 4; i++)
                {
                    int newX = current.X + directions[i, 0];
                    int newY = current.Y + directions[i, 1];

                    if (newX >= 0 && newX < _width && newY >= 0 && newY < _height &&
                        grid[newX, newY] == 0 && !visited[newX, newY])
                    {
                        visited[newX, newY] = true;
                        queue.Enqueue(new Point(newX, newY));
                    }
                }
            }

            return false;
        }

    }
}
