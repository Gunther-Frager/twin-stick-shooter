using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using TwinStickShooter.Core;

namespace TwinStickShooter.Rendering
{
    /// <summary>
    /// Renderizador del mundo basado en formas compuestas. El mapa se dibuja con
    /// círculos y cápsulas, con la grilla como fallback de compatibilidad.
    /// </summary>
    public class ArenaRenderer
    {
        private GraphicsDevice _graphicsDevice;
        private BasicEffect _effect;
        private VertexPositionColor[] _vertices;
        private int _vertexCount;
        private LevelManager _levelManager;

        public ArenaRenderer(GraphicsDevice graphicsDevice, LevelManager levelManager)
        {
            _graphicsDevice = graphicsDevice;
            _levelManager = levelManager;
            
            _effect = new BasicEffect(graphicsDevice)
            {
                VertexColorEnabled = true,
                World = Matrix.Identity,
                Projection = Matrix.CreateOrthographicOffCenter(0, GameConstants.ScreenWidth, GameConstants.ScreenHeight, 0, 0, -1)
            };

            BuildArenaGeometry();
        }

        private void BuildArenaGeometry()
        {
            System.Diagnostics.Debug.WriteLine("[ArenaRenderer] Reconstruyendo geometría...");
            List<VertexPositionColor> vertices = new List<VertexPositionColor>();

            float width = GameConstants.WorldWidth;
            float height = GameConstants.WorldHeight;

            Color borderColor = new Color(0, 255, 220);
            Color wallColor = new Color(255, 255, 255);
            Color startColor = new Color(0, 255, 0);
            Color endColor = new Color(255, 0, 0);

            // Borde exterior del mundo
            AddLine(vertices, new Vector3(0, 0, 0), new Vector3(width, 0, 0), borderColor);
            AddLine(vertices, new Vector3(width, 0, 0), new Vector3(width, height, 0), borderColor);
            AddLine(vertices, new Vector3(width, height, 0), new Vector3(0, height, 0), borderColor);
            AddLine(vertices, new Vector3(0, height, 0), new Vector3(0, 0, 0), borderColor);

            if (_levelManager.HasPrimitiveMapData())
            {
                foreach (var circle in _levelManager.PrimitiveCircles)
                {
                    AddCircle(vertices, circle.Center, circle.Radius, wallColor, 24);
                }

                foreach (var capsule in _levelManager.PrimitiveCapsules)
                {
                    AddCapsule(vertices, capsule.Start, capsule.End, capsule.Radius, wallColor, 18);
                }
            }
            else
            {
                for (int x = 0; x < GameConstants.GridWidth; x++)
                {
                    for (int y = 0; y < GameConstants.GridHeight; y++)
                    {
                        if (_levelManager.CheckCollision(new Vector2(x * GameConstants.GridCellSize + 1, y * GameConstants.GridCellSize + 1), 1f))
                        {
                            float px = x * GameConstants.GridCellSize;
                            float py = y * GameConstants.GridCellSize;
                            float s = GameConstants.GridCellSize;

                            AddLine(vertices, new Vector3(px, py, 0), new Vector3(px + s, py, 0), wallColor);
                            AddLine(vertices, new Vector3(px + s, py, 0), new Vector3(px + s, py + s, 0), wallColor);
                            AddLine(vertices, new Vector3(px + s, py + s, 0), new Vector3(px, py + s, 0), wallColor);
                            AddLine(vertices, new Vector3(px, py + s, 0), new Vector3(px, py, 0), wallColor);
                        }
                    }
                }
            }

            // Spawn y salida
            Vector2 spawnPosition = _levelManager.GetSpawnPosition();
            Point spawnPoint = _levelManager.WorldToGrid(spawnPosition);
            Vector2 exitPosition = _levelManager.GetExitPosition();
            Point exitPoint = _levelManager.WorldToGrid(exitPosition);

            float startX = spawnPoint.X * GameConstants.GridCellSize;
            float startY = spawnPoint.Y * GameConstants.GridCellSize;
            AddSquare(vertices, startX, startY, GameConstants.GridCellSize, startColor);

            float endX = exitPoint.X * GameConstants.GridCellSize;
            float endY = exitPoint.Y * GameConstants.GridCellSize;
            AddSquare(vertices, endX, endY, GameConstants.GridCellSize, endColor);

            _vertices = vertices.ToArray();
            _vertexCount = _vertices.Length;
        }

        private static void AddLine(List<VertexPositionColor> vertices, Vector3 a, Vector3 b, Color color)
        {
            vertices.Add(new VertexPositionColor(a, color));
            vertices.Add(new VertexPositionColor(b, color));
        }

        private static void AddSquare(List<VertexPositionColor> vertices, float x, float y, float size, Color color)
        {
            Vector3 a = new Vector3(x, y, 0);
            Vector3 b = new Vector3(x + size, y, 0);
            Vector3 c = new Vector3(x + size, y + size, 0);
            Vector3 d = new Vector3(x, y + size, 0);
            AddLine(vertices, a, b, color);
            AddLine(vertices, b, c, color);
            AddLine(vertices, c, d, color);
            AddLine(vertices, d, a, color);
        }

        private static void AddCircle(List<VertexPositionColor> vertices, Vector2 center, float radius, Color color, int segments)
        {
            for (int i = 0; i < segments; i++)
            {
                float a = (i / (float)segments) * MathHelper.TwoPi;
                float b = ((i + 1) / (float)segments) * MathHelper.TwoPi;
                Vector2 pa = center + new Vector2((float)Math.Cos(a), (float)Math.Sin(a)) * radius;
                Vector2 pb = center + new Vector2((float)Math.Cos(b), (float)Math.Sin(b)) * radius;
                AddLine(vertices, new Vector3(pa, 0), new Vector3(pb, 0), color);
            }
        }

        private static void AddCapsule(List<VertexPositionColor> vertices, Vector2 start, Vector2 end, float radius, Color color, int segments)
        {
            Vector2 dir = end - start;
            float length = dir.Length();
            if (length < 0.001f)
            {
                AddCircle(vertices, start, radius, color, segments);
                return;
            }

            Vector2 normal = dir / length;
            Vector2 tangent = new Vector2(-normal.Y, normal.X);
            Vector2 p1 = start + tangent * radius;
            Vector2 p2 = end + tangent * radius;
            Vector2 p3 = start - tangent * radius;
            Vector2 p4 = end - tangent * radius;

            AddLine(vertices, new Vector3(p1, 0), new Vector3(p2, 0), color);
            AddLine(vertices, new Vector3(p3, 0), new Vector3(p4, 0), color);
            AddCircle(vertices, start, radius, color, segments / 2);
            AddCircle(vertices, end, radius, color, segments / 2);
        }

        public void RebuildGeometry()
        {
            BuildArenaGeometry();
        }

        public void Draw(Matrix viewMatrix)
        {
            _effect.View = viewMatrix;

            foreach (var pass in _effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                _graphicsDevice.DrawUserPrimitives(PrimitiveType.LineList, _vertices, 0, _vertexCount / 2);
            }
        }
    }
}
