using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;

namespace TwinStickShooter.Core
{
    /// <summary>
    /// Generador de mapas procedurales con garantía de transitabilidad.
    /// Utiliza algoritmos de generación de mazmorras y BFS para verificar caminos válidos.
    /// </summary>
    public class MapGenerator
    {
        private readonly int _width;
        private readonly int _height;
        private readonly int _cellSize;
        private readonly Random _random;

        public MapGenerator(int width, int height, int cellSize)
        {
            _width = width;
            _height = height;
            _cellSize = cellSize;
            _random = new Random();
        }

        /// <summary>
        /// Genera un mapa procedural con habitaciones y pasillos.
        /// </summary>
        public int[,] GenerateMap()
        {
            int[,] grid = new int[_width, _height];

            // Inicializar el mapa con paredes
            for (int x = 0; x < _width; x++)
            {
                for (int y = 0; y < _height; y++)
                {
                    grid[x, y] = 1;
                }
            }

            // Generar habitaciones y pasillos
            GenerateRooms(grid);
            GenerateCorridors(grid);

            // Verificar la transitabilidad
            if (!IsMapTraversable(grid))
            {
                // Si el mapa no es transitable, regenerarlo
                return GenerateMap();
            }

            return grid;
        }

        /// <summary>
        /// Genera habitaciones aleatorias en el mapa.
        /// </summary>
        private void GenerateRooms(int[,] grid)
        {
            int roomCount = _random.Next(3, 6);
            for (int i = 0; i < roomCount; i++)
            {
                int roomWidth = _random.Next(3, 8);
                int roomHeight = _random.Next(3, 8);
                int roomX = _random.Next(1, _width - roomWidth - 1);
                int roomY = _random.Next(1, _height - roomHeight - 1);

                for (int x = roomX; x < roomX + roomWidth; x++)
                {
                    for (int y = roomY; y < roomY + roomHeight; y++)
                    {
                        grid[x, y] = 0;
                    }
                }
            }
        }

        /// <summary>
        /// Genera pasillos entre habitaciones para conectar el mapa.
        /// </summary>
        private void GenerateCorridors(int[,] grid)
        {
            // Encontrar centros de habitaciones
            List<Point> roomCenters = new List<Point>();
            for (int x = 1; x < _width - 1; x++)
            {
                for (int y = 1; y < _height - 1; y++)
                {
                    if (grid[x, y] == 0 && IsRoomCenter(grid, x, y))
                    {
                        roomCenters.Add(new Point(x, y));
                    }
                }
            }

            // Conectar habitaciones con pasillos
            for (int i = 0; i < roomCenters.Count - 1; i++)
            {
                Point start = roomCenters[i];
                Point end = roomCenters[i + 1];
                ConnectRooms(grid, start, end);
            }
        }

        /// <summary>
        /// Verifica si una celda es el centro de una habitación.
        /// </summary>
        private bool IsRoomCenter(int[,] grid, int x, int y)
        {
            // Verificar si hay espacio vacío alrededor
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (grid[x + dx, y + dy] != 0)
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// Conecta dos habitaciones con un pasillo.
        /// </summary>
        private void ConnectRooms(int[,] grid, Point start, Point end)
        {
            // Moverse horizontalmente hasta la misma columna
            int x = start.X;
            int y = start.Y;
            while (x != end.X)
            {
                grid[x, y] = 0;
                x += Math.Sign(end.X - x);
            }

            // Moverse verticalmente hasta la misma fila
            while (y != end.Y)
            {
                grid[x, y] = 0;
                y += Math.Sign(end.Y - y);
            }
        }

        /// <summary>
        /// Verifica si el mapa es transitable desde el punto de inicio hasta el punto de salida.
        /// </summary>
        private bool IsMapTraversable(int[,] grid)
        {
            Point start = FindStartPoint(grid);
            Point end = FindEndPoint(grid);

            return BFS(grid, start, end);
        }

        /// <summary>
        /// Encuentra un punto de inicio válido en el mapa.
        /// </summary>
        private Point FindStartPoint(int[,] grid)
        {
            // Buscar una celda vacía cerca del borde izquierdo
            for (int x = 1; x < _width - 1; x++)
            {
                for (int y = 1; y < _height - 1; y++)
                {
                    if (grid[x, y] == 0)
                    {
                        return new Point(x, y);
                    }
                }
            }
            return new Point(1, 1);
        }

        /// <summary>
        /// Encuentra un punto de salida válido en el mapa.
        /// </summary>
        private Point FindEndPoint(int[,] grid)
        {
            // Buscar una celda vacía cerca del borde derecho
            for (int x = _width - 2; x > 0; x--)
            {
                for (int y = _height - 2; y > 0; y--)
                {
                    if (grid[x, y] == 0)
                    {
                        return new Point(x, y);
                    }
                }
            }
            return new Point(_width - 2, _height - 2);
        }

        /// <summary>
        /// Algoritmo BFS para verificar la transitabilidad del mapa.
        /// </summary>
        private bool BFS(int[,] grid, Point start, Point end)
        {
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