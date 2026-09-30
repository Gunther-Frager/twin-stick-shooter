using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TwinStickShooter.Core
{
    /// <summary>
    /// Genera terreno mediante un crawler que excava y conecta salas prefab.
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
        public List<Rectangle> Rooms { get; private set; } = new List<Rectangle>();
        public List<(int From, int To)> RoomConnections { get; } = new List<(int From, int To)>();

        public MapGenerator(int width, int height, MapGenerationSettings settings = null)
        {
            _width = width;
            _height = height;
            Settings = settings ?? new MapGenerationSettings();
            _random = new Random(Settings.Seed);
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
            int[,] grid = new int[_width, _height];
            LastGenerationFailure = null;
            int requiredPrefabRooms = _roomTemplates.Count == 0
                ? 0
                : Math.Min(Math.Max(0, Settings.MinPrefabRooms), Math.Max(0, Settings.MaxPrefabRooms));

            for (int attempt = 0; attempt < Math.Max(1, Settings.MaxGenerationAttempts); attempt++)
            {
                _random = new Random(unchecked(Settings.Seed + attempt));
                Rooms.Clear();
                RoomConnections.Clear();
                SpawnExitPathLength = -1;
                CrawlerStepCount = 0;
                PrefabPlacementAttempts = 0;
                PrefabRoomsPlaced = 0;

                // El crawler excava desde la sala inicial; los prefabs nacen conectados a su recorrido.
                for (int x = 0; x < _width; x++)
                {
                    for (int y = 0; y < _height; y++)
                    {
                        grid[x, y] = 1;
                    }
                }

                if (!GrowCrawlerMap(grid)) continue;

                ExitPoint = FindFarthestReachablePoint(grid, SpawnPoint, out int routeLength);
                SpawnExitPathLength = routeLength;
                if (SpawnExitPathLength >= Settings.MinimumSpawnExitPathLength &&
                    PrefabRoomsPlaced >= requiredPrefabRooms &&
                    AreAllRoomsReachable(grid, SpawnPoint))
                {
                    return grid;
                }
            }

            LastGenerationFailure = $"No se generó un mapa válido tras {Math.Max(1, Settings.MaxGenerationAttempts)} intentos. " +
                $"Distancia mínima requerida: {Settings.MinimumSpawnExitPathLength} celdas; " +
                $"prefabs mínimos: {requiredPrefabRooms}.";
            SpawnPoint = previousSpawn;
            ExitPoint = previousExit;
            SpawnExitPathLength = previousPathLength;
            CrawlerStepCount = previousStepCount;
            PrefabPlacementAttempts = previousPlacementAttempts;
            PrefabRoomsPlaced = previousPrefabRoomsPlaced;
            Rooms = previousRooms;
            RoomConnections.Clear();
            RoomConnections.AddRange(previousConnections);
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

            Point current = SpawnPoint;
            Point direction = new Point(1, 0);
            int attachedRoomIndex = 0;
            Point[] directions =
            {
                new Point(0, -1), new Point(1, 0), new Point(0, 1), new Point(-1, 0),
            };
            int maxSteps = Math.Max(1, Settings.CrawlerMaxSteps);
            int roomInterval = Math.Max(1, Settings.CrawlerRoomInterval);
            int maxPrefabRooms = Math.Max(0, Settings.MaxPrefabRooms);

            for (int step = 1; step <= maxSteps; step++)
            {
                current = MoveCrawler(grid, current, ref direction, directions);
                grid[current.X, current.Y] = 0;
                CrawlerStepCount = step;

                if (step % roomInterval == 0 && PrefabRoomsPlaced < maxPrefabRooms && _roomTemplates.Count > 0)
                {
                    PrefabPlacementAttempts++;
                    if (TryInsertPrefab(grid, current, directions, out Rectangle prefabRoom))
                    {
                        int roomIndex = Rooms.Count;
                        Rooms.Add(prefabRoom);
                        RoomConnections.Add((attachedRoomIndex, roomIndex));
                        attachedRoomIndex = roomIndex;
                        PrefabRoomsPlaced++;
                    }
                }
            }

            return true;
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
