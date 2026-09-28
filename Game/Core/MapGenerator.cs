using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TwinStickShooter.Core
{
    /// <summary>
    /// Generador de mapas procedurales con garantía de transitabilidad vía MST.
    /// Conecta las habitaciones usando Kruskal y permite loops adicionales.
    /// </summary>
    public class MapGenerator
    {
        private readonly int _width;
        private readonly int _height;
        private readonly int _cellSize;
        private Random _random;
        private List<RoomTemplateData> _roomTemplates = new List<RoomTemplateData>();

        public MapGenerationSettings Settings { get; }
        public Point SpawnPoint { get; private set; }
        public Point ExitPoint { get; private set; }
        public int SpawnExitPathLength { get; private set; }
        public string LastGenerationFailure { get; private set; }
        public List<Vector2> RoomEnemySpawnPoints { get; } = new List<Vector2>();
        public List<RoomTemplateData.EnemySpawn> RoomEnemySpawns { get; } = new List<RoomTemplateData.EnemySpawn>();
        public List<Rectangle> Rooms { get; private set; } = new List<Rectangle>();
        public List<(int From, int To)> RoomConnections { get; } = new List<(int From, int To)>();

        public MapGenerator(int width, int height, int cellSize, MapGenerationSettings settings = null)
        {
            _width = width;
            _height = height;
            _cellSize = cellSize;
            Settings = settings ?? new MapGenerationSettings();
            _random = new Random(Settings.Seed);
        }

        public void SetRoomTemplates(List<RoomTemplateData> templates)
        {
            _roomTemplates = templates ?? new List<RoomTemplateData>();
        }

        private class Edge : IComparable<Edge>
        {
            public int From { get; set; }
            public int To { get; set; }
            public float Distance { get; set; }

            public int CompareTo(Edge other)
            {
                return Distance.CompareTo(other.Distance);
            }
        }

        private class UnionFind
        {
            private readonly int[] _parent;
            private readonly int[] _rank;

            public UnionFind(int size)
            {
                _parent = new int[size];
                _rank = new int[size];
                for (int i = 0; i < size; i++)
                {
                    _parent[i] = i;
                }
            }

            public int Find(int x)
            {
                if (_parent[x] != x)
                {
                    _parent[x] = Find(_parent[x]);
                }
                return _parent[x];
            }

            public void Union(int x, int y)
            {
                int rootX = Find(x);
                int rootY = Find(y);
                if (rootX == rootY) return;

                if (_rank[rootX] < _rank[rootY])
                {
                    _parent[rootX] = rootY;
                }
                else if (_rank[rootX] > _rank[rootY])
                {
                    _parent[rootY] = rootX;
                }
                else
                {
                    _parent[rootY] = rootX;
                    _rank[rootX]++;
                }
            }
        }

        public int[,] GenerateMap()
        {
            Point previousSpawn = SpawnPoint;
            Point previousExit = ExitPoint;
            int previousPathLength = SpawnExitPathLength;
            List<Rectangle> previousRooms = Rooms.ToList();
            List<(int From, int To)> previousConnections = RoomConnections.ToList();
            List<Vector2> previousSpawnPoints = RoomEnemySpawnPoints.ToList();
            List<RoomTemplateData.EnemySpawn> previousTypedSpawns = RoomEnemySpawns.ToList();
            _random = new Random(Settings.Seed);
            int[,] grid = new int[_width, _height];
            LastGenerationFailure = null;

            for (int attempt = 0; attempt < Math.Max(1, Settings.MaxGenerationAttempts); attempt++)
            {
                RoomEnemySpawnPoints.Clear();
                RoomEnemySpawns.Clear();
                Rooms.Clear();
                RoomConnections.Clear();
                SpawnExitPathLength = -1;

                // 1. Inicializar con paredes
                for (int x = 0; x < _width; x++)
                {
                    for (int y = 0; y < _height; y++)
                    {
                        grid[x, y] = 1;
                    }
                }

                // 2. Generar salas y obtener 1 centro exacto por sala
                List<Rectangle> rooms = GenerateRooms(grid);
                List<Point> roomCenters = rooms.Select(r => r.Center).ToList();

                // 3. Conectar vía MST (Kruskal) + loops opcionales
                GenerateCorridors(grid, roomCenters);

                // 4. Spawn en sala inicial (muy pequeña), Exit en la sala más lejana
                bool validSpawn = false;
                if (rooms.Count > 0)
                {
                    // Obtener la habitación del spawn (siempre la primera, pequeña)
                    Rectangle spawnRoom = rooms[0];
                    
                    // Validar que la habitación de spawn sea pequeña
                    if (spawnRoom.Width <= Settings.SpawnRoomMaxSize && spawnRoom.Height <= Settings.SpawnRoomMaxSize)
                    {
                        validSpawn = true;
                        // Asegurar que el spawn esté DENTRO de la habitación pequeña
                        SpawnPoint = new Point(
                            spawnRoom.X + spawnRoom.Width / 2,
                            spawnRoom.Y + spawnRoom.Height / 2
                        );
                        
                        if (roomCenters.Count > 1)
                        {
                            // Filtrar centros que estén en habitaciones distintas y a una distancia mínima
                            var distantCenters = roomCenters
                                .Where((c, index) => index > 0 && !IsPointInRoom(c, spawnRoom))
                                .Select(c => new { Point = c, Distance = FindPathDistance(grid, SpawnPoint, c) })
                                .Where(candidate => candidate.Distance >= Settings.MinimumSpawnExitPathLength)
                                .OrderByDescending(candidate => candidate.Distance)
                                .ToList();
                            
                            if (distantCenters.Count > 0)
                            {
                                ExitPoint = distantCenters[0].Point;
                                SpawnExitPathLength = distantCenters[0].Distance;
                            }
                            else
                            {
                                validSpawn = false;
                            }
                        }
                        else
                        {
                            ExitPoint = new Point(_width - 2, _height - 2); // Fallback extremo
                        }
                    }
                }
                else
                {
                    SpawnPoint = new Point(1, 1);
                    ExitPoint = new Point(_width - 2, _height - 2);
                }

                SpawnExitPathLength = FindPathDistance(grid, SpawnPoint, ExitPoint);
                // 5. Verificar transitabilidad total con BFS (asegurar que todas las salas sean accesibles)
                if (validSpawn && IsMapFullyTraversable(grid, SpawnPoint, ExitPoint, roomCenters) &&
                    SpawnExitPathLength >= Settings.MinimumSpawnExitPathLength)
                {
                    return grid;
                }
            }

            LastGenerationFailure = $"No se generó un mapa válido tras {Math.Max(1, Settings.MaxGenerationAttempts)} intentos. " +
                $"Distancia mínima requerida: {Settings.MinimumSpawnExitPathLength} celdas.";
            SpawnPoint = previousSpawn;
            ExitPoint = previousExit;
            SpawnExitPathLength = previousPathLength;
            Rooms = previousRooms;
            RoomConnections.Clear();
            RoomConnections.AddRange(previousConnections);
            RoomEnemySpawnPoints.Clear();
            RoomEnemySpawnPoints.AddRange(previousSpawnPoints);
            RoomEnemySpawns.Clear();
            RoomEnemySpawns.AddRange(previousTypedSpawns);
            throw new InvalidOperationException(LastGenerationFailure);
        }

        private List<Rectangle> GenerateRooms(int[,] grid)
        {
            List<Rectangle> rooms = new List<Rectangle>();
            Rooms.Clear();
            int minRoomCount = Math.Max(2, Settings.MinRoomCount);
            int maxRoomCount = Math.Max(minRoomCount + 1, Settings.MaxRoomCount);
            int roomCount = _random.Next(minRoomCount, maxRoomCount);

            for (int i = 0; i < roomCount; i++)
            {
                int roomWidth, roomHeight;
                
                if (i == 0) // La habitación de spawn siempre es muy pequeña
                {
                    roomWidth = _random.Next(Settings.SpawnRoomMinWidth, Settings.SpawnRoomMaxWidth);
                    roomHeight = _random.Next(Settings.SpawnRoomMinWidth, Settings.SpawnRoomMaxWidth);
                }
                else
                {
                    // Habitaciones más grandes para el resto del mapa
                    if (_random.NextDouble() < Settings.MediumRoomProbability) // Probabilidad de habitación mediana
                    {
                        roomWidth = _random.Next(Settings.MediumRoomMinWidth, Settings.MediumRoomMaxWidth);
                        roomHeight = _random.Next(Settings.MediumRoomMinWidth, Settings.MediumRoomMaxWidth);
                    }
                    else
                    {
                        roomWidth = _random.Next(Settings.StandardRoomMinWidth, Settings.StandardRoomMaxWidth);
                        roomHeight = _random.Next(Settings.StandardRoomMinWidth, Settings.StandardRoomMaxWidth);
                    }
                }

                // Intentar colocar la habitación sin solapamientos
                for (int attempt = 0; attempt < Settings.MaxRoomPlacementAttempts; attempt++)
                {
                    int roomX = _random.Next(2, _width - roomWidth - 2);
                    int roomY = _random.Next(2, _height - roomHeight - 2);

                    Rectangle newRoom = new Rectangle(roomX, roomY, roomWidth, roomHeight);

                    // Verificar intersección con margen para evitar que se toquen o fusionen
                    Rectangle paddedRoom = new Rectangle(
                        newRoom.X - Settings.RoomPadding,
                        newRoom.Y - Settings.RoomPadding,
                        newRoom.Width + Settings.RoomPadding * 2,
                        newRoom.Height + Settings.RoomPadding * 2);
                    
                    bool overlaps = false;
                    foreach (var room in rooms)
                    {
                        if (paddedRoom.Intersects(room))
                        {
                            overlaps = true;
                            break;
                        }
                    }

                    if (!overlaps)
                    {
                        for (int x = roomX; x < roomX + roomWidth; x++)
                        {
                            for (int y = roomY; y < roomY + roomHeight; y++)
                            {
                                grid[x, y] = 0;
                            }
                        }

                        RoomTemplateData template = null;
                        if (i > 0)
                        {
                            template = SelectTemplate(newRoom);
                        }

                        if (template != null)
                        {
                            for (int localX = 0; localX < roomWidth; localX++)
                            {
                                for (int localY = 0; localY < roomHeight; localY++)
                                {
                                    if (template.TryGetCell(localX, localY, out bool isWall))
                                    {
                                        int gx = roomX + localX;
                                        int gy = roomY + localY;
                                        if (gx >= 0 && gx < _width && gy >= 0 && gy < _height)
                                        {
                                            grid[gx, gy] = isWall ? 1 : 0;
                                        }
                                    }
                                }
                            }

                            foreach (var spawn in template.TypedEnemySpawns)
                            {
                                float worldX = (roomX + spawn.Position.X) * _cellSize + _cellSize / 2f;
                                float worldY = (roomY + spawn.Position.Y) * _cellSize + _cellSize / 2f;
                                RoomEnemySpawns.Add(new RoomTemplateData.EnemySpawn(
                                    new Point((int)worldX, (int)worldY), spawn.Type));
                                RoomEnemySpawnPoints.Add(new Vector2(worldX, worldY));
                            }
                        }
                        else
                        {
                            if (i > 0 && _random.NextDouble() < Math.Clamp(Settings.CrawlerRoomProbability, 0.0, 1.0))
                            {
                                AddCrawlerRoom(grid, newRoom);
                            }
                            // Agregar islas solo en habitaciones grandes y no en la de spawn
                            else if (i > 0 && roomWidth >= GameConstants.IslandMinRoomSize && roomHeight >= GameConstants.IslandMinRoomSize)
                            {
                                AddIslandsToRoom(grid, newRoom);
                            }
                        }

                        rooms.Add(newRoom);
                        Rooms.Add(newRoom);
                        break;
                    }
                }
            }

            return rooms;
        }



        private RoomTemplateData SelectTemplate(Rectangle room)
        {
            if (!Settings.UseRoomTemplates || _roomTemplates == null || _roomTemplates.Count == 0)
            {
                return null;
            }

            var matchingTemplates = _roomTemplates.Where(t =>
                room.Width >= t.MinSize && room.Width <= t.MaxSize &&
                room.Height >= t.MinSize && room.Height <= t.MaxSize).ToList();

            if (matchingTemplates.Count == 0)
            {
                return null;
            }

            return matchingTemplates[_random.Next(matchingTemplates.Count)];
        }

        private void AddIslandsToRoom(int[,] grid, Rectangle room)
        {
            int islandCount = _random.Next(GameConstants.MinIslandsPerRoom, GameConstants.MaxIslandsPerRoom);
            for (int i = 0; i < islandCount; i++)
            {
                // Colocar isla lejos de los bordes para no bloquear pasillos
                int ix = _random.Next(room.X + GameConstants.IslandEdgePadding, room.X + room.Width - GameConstants.IslandEdgePadding);
                int iy = _random.Next(room.Y + GameConstants.IslandEdgePadding, room.Y + room.Height - GameConstants.IslandEdgePadding);
                
                grid[ix, iy] = 1;
                
                // Probabilidad de que sea una isla de 2x2 si hay espacio
                if (_random.NextDouble() < GameConstants.LargeIslandProbability && 
                    ix + 1 < room.X + room.Width - GameConstants.IslandEdgePadding && 
                    iy + 1 < room.Y + room.Height - GameConstants.IslandEdgePadding)
                {
                    grid[ix + 1, iy] = 1;
                    grid[ix, iy + 1] = 1;
                    grid[ix + 1, iy + 1] = 1;
                }
            }
        }

        private void AddCrawlerRoom(int[,] grid, Rectangle room)
        {
            for (int x = room.X; x < room.X + room.Width; x++)
            {
                for (int y = room.Y; y < room.Y + room.Height; y++)
                {
                    grid[x, y] = 1;
                }
            }

            Point center = room.Center;
            grid[center.X, center.Y] = 0;

            long totalSteps = (long)Math.Max(0, Settings.CrawlerStepsPerRoomCell) * room.Width * room.Height;
            if (totalSteps == 0)
            {
                return;
            }

            int walkerCount = _random.Next(1, 4);
            Point[] directions =
            {
                new Point(0, 1),
                new Point(1, 0),
                new Point(0, -1),
                new Point(-1, 0),
            };

            for (int walker = 0; walker < walkerCount; walker++)
            {
                Point position = center;
                long walkerSteps = totalSteps / walkerCount + (walker < totalSteps % walkerCount ? 1 : 0);
                for (long step = 0; step < walkerSteps; step++)
                {
                    Point direction = directions[_random.Next(directions.Length)];
                    position = new Point(
                        Math.Clamp(position.X + direction.X, room.X, room.X + room.Width - 1),
                        Math.Clamp(position.Y + direction.Y, room.Y, room.Y + room.Height - 1));
                    grid[position.X, position.Y] = 0;
                }
            }
        }

        // Verifica si un punto está dentro de una habitación
        private bool IsPointInRoom(Point point, Rectangle room)
        {
            return point.X >= room.X && point.X < room.X + room.Width && 
                   point.Y >= room.Y && point.Y < room.Y + room.Height;
        }

        private void GenerateCorridors(int[,] grid, List<Point> roomCenters)
        {
            if (roomCenters.Count < 2) return;

            // Calcular grafo completo de distancias
            List<Edge> edges = new List<Edge>();
            for (int i = 0; i < roomCenters.Count; i++)
            {
                for (int j = i + 1; j < roomCenters.Count; j++)
                {
                    float distance = Vector2.Distance(
                        new Vector2(roomCenters[i].X, roomCenters[i].Y),
                        new Vector2(roomCenters[j].X, roomCenters[j].Y)
                    );
                    edges.Add(new Edge { From = i, To = j, Distance = distance });
                }
            }

            edges.Sort();

            UnionFind uf = new UnionFind(roomCenters.Count);
            List<Edge> mstEdges = new List<Edge>();
            List<Edge> remainingEdges = new List<Edge>();

            foreach (Edge edge in edges)
            {
                if (uf.Find(edge.From) != uf.Find(edge.To))
                {
                    uf.Union(edge.From, edge.To);
                    mstEdges.Add(edge);
                }
                else
                {
                    remainingEdges.Add(edge);
                }
            }

            // Conectar aristas del MST
            foreach (Edge edge in mstEdges)
            {
                RoomConnections.Add((edge.From, edge.To));
                ConnectRooms(grid, roomCenters[edge.From], roomCenters[edge.To]);
            }

            // Flag / Opción: aristas extra para crear loops (evita pasillos únicos aburridos)
            bool addLoops = true;
            if (addLoops && remainingEdges.Count > 0)
            {
                int loopsToAdd = Math.Min(_random.Next(Settings.MinExtraLoops, Settings.MaxExtraLoops), remainingEdges.Count);
                for (int i = 0; i < loopsToAdd; i++)
                {
                    int index = _random.Next(remainingEdges.Count);
                    Edge extra = remainingEdges[index];
                    RoomConnections.Add((extra.From, extra.To));
                    ConnectRooms(grid, roomCenters[extra.From], roomCenters[extra.To]);
                    remainingEdges.RemoveAt(index);
                }
            }
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

        /// <summary>
        /// Verifica que tanto el punto de salida como todos los centros de las habitaciones 
        /// sean alcanzables desde el punto de inicio.
        /// </summary>
        private bool IsMapFullyTraversable(int[,] grid, Point start, Point end, List<Point> centers)
        {
            if (grid[start.X, start.Y] != 0 || grid[end.X, end.Y] != 0)
                return false;

            HashSet<Point> targets = new HashSet<Point>(centers);
            targets.Add(end);

            Queue<Point> queue = new Queue<Point>();
            bool[,] visited = new bool[_width, _height];

            queue.Enqueue(start);
            visited[start.X, start.Y] = true;

            int[,] directions = { { 0, 1 }, { 1, 0 }, { 0, -1 }, { -1, 0 } };

            while (queue.Count > 0)
            {
                Point current = queue.Dequeue();

                if (targets.Contains(current))
                {
                    targets.Remove(current);
                    if (targets.Count == 0) return true;
                }

                for (int i = 0; i < 4; i++)
                {
                    int nx = current.X + directions[i, 0];
                    int ny = current.Y + directions[i, 1];

                    if (nx >= 0 && nx < _width && ny >= 0 && ny < _height &&
                        grid[nx, ny] == 0 && !visited[nx, ny])
                    {
                        visited[nx, ny] = true;
                        queue.Enqueue(new Point(nx, ny));
                    }
                }
            }

            return targets.Count == 0;
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

        public void TestMapTraversability(int mapCount = 50)
        {
            int validCount = 0;
            for (int i = 0; i < mapCount; i++)
            {
                int[,] grid = GenerateMap();
                if (IsMapTraversable(grid, SpawnPoint, ExitPoint))
                {
                    validCount++;
                }
            }
            Console.WriteLine($"[MapGenerator] Baseline MST: {validCount}/{mapCount} mapas transitables directamente.");
        }

        public void Initialize()
        {
            TestMapTraversability(50);
        }
    }
}
