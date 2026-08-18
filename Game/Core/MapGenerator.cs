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
        private readonly Random _random;
        private List<RoomTemplateData> _roomTemplates = new List<RoomTemplateData>();

        public Point SpawnPoint { get; private set; }
        public Point ExitPoint { get; private set; }
        public List<Vector2> RoomEnemySpawnPoints { get; } = new List<Vector2>();

        public MapGenerator(int width, int height, int cellSize)
        {
            _width = width;
            _height = height;
            _cellSize = cellSize;
            _random = new Random();
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
            RoomEnemySpawnPoints.Clear();
            int[,] grid = new int[_width, _height];

            for (int attempt = 0; attempt < GameConstants.MaxGenerationAttempts; attempt++)
            {
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
                    if (spawnRoom.Width <= GameConstants.SpawnRoomMaxSize && spawnRoom.Height <= GameConstants.SpawnRoomMaxSize)
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
                                .Where((c, index) => index > 0 && // Excluir la habitación del spawn
                                       !IsPointInRoom(c, spawnRoom) && // Asegurar que no esté en la misma habitación
                                       Vector2.Distance(new Vector2(c.X, c.Y), new Vector2(SpawnPoint.X, SpawnPoint.Y)) > GameConstants.MinSpawnExitDistance) // Distancia mínima
                                .OrderByDescending(c => Vector2.Distance(new Vector2(c.X, c.Y), new Vector2(SpawnPoint.X, SpawnPoint.Y)))
                                .ToList();
                            
                            if (distantCenters.Count > 0)
                            {
                                ExitPoint = distantCenters[0];
                            }
                            else
                            {
                                // Si no hay habitaciones suficientemente lejanas, elegir la más lejana disponible
                                ExitPoint = roomCenters
                                    .Where((c, index) => index > 0 && !IsPointInRoom(c, spawnRoom))
                                    .OrderByDescending(c => Vector2.Distance(new Vector2(c.X, c.Y), new Vector2(SpawnPoint.X, SpawnPoint.Y)))
                                    .FirstOrDefault(new Point(_width - 2, _height - 2));
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

                // 5. Verificar transitabilidad total con BFS (asegurar que todas las salas sean accesibles)
                if (validSpawn && IsMapFullyTraversable(grid, SpawnPoint, ExitPoint, roomCenters))
                {
                    return grid;
                }
            }

            Console.WriteLine("[MapGenerator] WARNING: Se agotaron los intentos de generación. Devolviendo último mapa generado.");
            return grid;
        }

        private List<Rectangle> GenerateRooms(int[,] grid)
        {
            List<Rectangle> rooms = new List<Rectangle>();
            int roomCount = _random.Next(GameConstants.MinRoomCount, GameConstants.MaxRoomCount); // Más habitaciones para mayor complejidad

            for (int i = 0; i < roomCount; i++)
            {
                int roomWidth, roomHeight;
                
                if (i == 0) // La habitación de spawn siempre es muy pequeña
                {
                    roomWidth = _random.Next(GameConstants.SpawnRoomMinWidth, GameConstants.SpawnRoomMaxWidth);
                    roomHeight = _random.Next(GameConstants.SpawnRoomMinWidth, GameConstants.SpawnRoomMaxWidth);
                }
                else
                {
                    // Habitaciones más grandes para el resto del mapa
                    if (_random.NextDouble() < GameConstants.MediumRoomProbability) // Probabilidad de habitación mediana
                    {
                        roomWidth = _random.Next(GameConstants.MediumRoomMinWidth, GameConstants.MediumRoomMaxWidth);
                        roomHeight = _random.Next(GameConstants.MediumRoomMinWidth, GameConstants.MediumRoomMaxWidth);
                    }
                    else
                    {
                        roomWidth = _random.Next(GameConstants.StandardRoomMinWidth, GameConstants.StandardRoomMaxWidth);
                        roomHeight = _random.Next(GameConstants.StandardRoomMinWidth, GameConstants.StandardRoomMaxWidth);
                    }
                }

                // Intentar colocar la habitación sin solapamientos
                bool placed = false;
                for (int attempt = 0; attempt < GameConstants.MaxRoomPlacementAttempts; attempt++)
                {
                    int roomX = _random.Next(2, _width - roomWidth - 2);
                    int roomY = _random.Next(2, _height - roomHeight - 2);

                    Rectangle newRoom = new Rectangle(roomX, roomY, roomWidth, roomHeight);

                    // Verificar intersección con margen para evitar que se toquen o fusionen
                    Rectangle paddedRoom = new Rectangle(
                        newRoom.X - GameConstants.RoomPadding, 
                        newRoom.Y - GameConstants.RoomPadding, 
                        newRoom.Width + GameConstants.RoomPadding * 2, 
                        newRoom.Height + GameConstants.RoomPadding * 2);
                    
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

                            foreach (var p in template.EnemySpawns)
                            {
                                float worldX = (roomX + p.X) * _cellSize + _cellSize / 2f;
                                float worldY = (roomY + p.Y) * _cellSize + _cellSize / 2f;
                                RoomEnemySpawnPoints.Add(new Vector2(worldX, worldY));
                            }
                        }
                        else
                        {
                            // Agregar islas solo en habitaciones grandes y no en la de spawn
                            if (i > 0 && roomWidth >= GameConstants.IslandMinRoomSize && roomHeight >= GameConstants.IslandMinRoomSize)
                            {
                                AddIslandsToRoom(grid, newRoom);
                            }
                        }

                        rooms.Add(newRoom);
                        placed = true;
                        break;
                    }
                }
            }

            return rooms;
        }



        private RoomTemplateData SelectTemplate(Rectangle room)
        {
            if (!GameConstants.UseRoomTemplates || _roomTemplates == null || _roomTemplates.Count == 0)
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
                ConnectRooms(grid, roomCenters[edge.From], roomCenters[edge.To]);
            }

            // Flag / Opción: aristas extra para crear loops (evita pasillos únicos aburridos)
            bool addLoops = true;
            if (addLoops && remainingEdges.Count > 0)
            {
                int loopsToAdd = Math.Min(_random.Next(GameConstants.MinExtraLoops, GameConstants.MaxExtraLoops), remainingEdges.Count);
                for (int i = 0; i < loopsToAdd; i++)
                {
                    int index = _random.Next(remainingEdges.Count);
                    Edge extra = remainingEdges[index];
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
