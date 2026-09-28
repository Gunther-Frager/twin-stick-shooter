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
        private VertexPositionColor[] _fillVertices;
        private int _fillVertexCount;
        private VertexPositionColor[] _outerGlowVertices;
        private int _outerGlowVertexCount;
        private VertexPositionColor[] _innerGlowVertices;
        private int _innerGlowVertexCount;
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
            if (!_levelManager.HasPrimitiveMapData())
            {
                throw new InvalidOperationException("Primitive map data must be initialized before building arena geometry.");
            }

            System.Diagnostics.Debug.WriteLine("[ArenaRenderer] Reconstruyendo geometría...");
            List<VertexPositionColor> vertices = new List<VertexPositionColor>();
            List<VertexPositionColor> fillVertices = new List<VertexPositionColor>();
            List<VertexPositionColor> outerGlowVertices = new List<VertexPositionColor>();
            List<VertexPositionColor> innerGlowVertices = new List<VertexPositionColor>();

            float width = GameConstants.WorldWidth;
            float height = GameConstants.WorldHeight;

            Color borderColor = new Color(0, 255, 220);
            Color wallColor = new Color(255, 255, 255);
            Color wallFillColor = new Color(10, 10, 30, 110);
            Color outerGlowColor = new Color(255, 255, 255, 28);
            Color innerGlowColor = new Color(255, 255, 255, 72);
            Color startColor = new Color(0, 255, 0);
            Color endColor = new Color(255, 0, 0);

            // Borde exterior del mundo con esquinas redondeadas.
            const float cornerRadius = 28f;
            const int cornerSegments = 6;
            AddLine(vertices, new Vector3(cornerRadius, 0, 0), new Vector3(width - cornerRadius, 0, 0), borderColor);
            AddArc(vertices, new Vector2(width - cornerRadius, cornerRadius), cornerRadius, -MathHelper.PiOver2, 0f, borderColor, cornerSegments);
            AddLine(vertices, new Vector3(width, cornerRadius, 0), new Vector3(width, height - cornerRadius, 0), borderColor);
            AddArc(vertices, new Vector2(width - cornerRadius, height - cornerRadius), cornerRadius, 0f, MathHelper.PiOver2, borderColor, cornerSegments);
            AddLine(vertices, new Vector3(width - cornerRadius, height, 0), new Vector3(cornerRadius, height, 0), borderColor);
            AddArc(vertices, new Vector2(cornerRadius, height - cornerRadius), cornerRadius, MathHelper.PiOver2, MathHelper.Pi, borderColor, cornerSegments);
            AddLine(vertices, new Vector3(0, height - cornerRadius, 0), new Vector3(0, cornerRadius, 0), borderColor);
            AddArc(vertices, new Vector2(cornerRadius, cornerRadius), cornerRadius, MathHelper.Pi, MathHelper.Pi + MathHelper.PiOver2, borderColor, cornerSegments);

            foreach (var circle in _levelManager.PrimitiveCircles)
            {
                AddCircle(vertices, circle.Center, circle.Radius, wallColor, 24);
                AddFilledCircle(fillVertices, circle.Center, circle.Radius, wallFillColor, 24);
                AddCircle(outerGlowVertices, circle.Center, circle.Radius + 6f, outerGlowColor, 24);
                AddCircle(innerGlowVertices, circle.Center, circle.Radius + 2.5f, innerGlowColor, 24);
            }

            foreach (var capsule in _levelManager.PrimitiveCapsules)
            {
                AddCapsule(vertices, capsule.Start, capsule.End, capsule.Radius, wallColor, 18);
                AddFilledCapsule(fillVertices, capsule.Start, capsule.End, capsule.Radius, wallFillColor, 18);
                AddCapsule(outerGlowVertices, capsule.Start, capsule.End, capsule.Radius + 6f, outerGlowColor, 18);
                AddCapsule(innerGlowVertices, capsule.Start, capsule.End, capsule.Radius + 2.5f, innerGlowColor, 18);
            }

            // Spawn y salida
            Vector2 spawnPosition = _levelManager.GetSpawnPosition();
            Point spawnPoint = _levelManager.WorldToGrid(spawnPosition);
            Vector2 exitPosition = _levelManager.GetExitPosition();
            Point exitPoint = _levelManager.WorldToGrid(exitPosition);

            float startX = spawnPoint.X * GameConstants.GridCellSize;
            float startY = spawnPoint.Y * GameConstants.GridCellSize;
            float markerRadius = GameConstants.GridCellSize * 0.45f;
            AddFilledCircle(fillVertices,
                new Vector2(startX + GameConstants.GridCellSize * 0.5f, startY + GameConstants.GridCellSize * 0.5f),
                markerRadius, startColor, 24);

            float endX = exitPoint.X * GameConstants.GridCellSize;
            float endY = exitPoint.Y * GameConstants.GridCellSize;
            AddFilledCircle(fillVertices,
                new Vector2(endX + GameConstants.GridCellSize * 0.5f, endY + GameConstants.GridCellSize * 0.5f),
                markerRadius, endColor, 24);

            _vertices = vertices.ToArray();
            _vertexCount = _vertices.Length;
            _fillVertices = fillVertices.ToArray();
            _fillVertexCount = _fillVertices.Length;
            _outerGlowVertices = outerGlowVertices.ToArray();
            _outerGlowVertexCount = _outerGlowVertices.Length;
            _innerGlowVertices = innerGlowVertices.ToArray();
            _innerGlowVertexCount = _innerGlowVertices.Length;
        }

        private static void AddLine(List<VertexPositionColor> vertices, Vector3 a, Vector3 b, Color color)
        {
            vertices.Add(new VertexPositionColor(a, color));
            vertices.Add(new VertexPositionColor(b, color));
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

        private static void AddFilledCircle(List<VertexPositionColor> vertices, Vector2 center, float radius, Color color, int segments)
        {
            for (int i = 0; i < segments; i++)
            {
                float a = (i / (float)segments) * MathHelper.TwoPi;
                float b = ((i + 1) / (float)segments) * MathHelper.TwoPi;
                Vector2 pa = center + new Vector2((float)Math.Cos(a), (float)Math.Sin(a)) * radius;
                Vector2 pb = center + new Vector2((float)Math.Cos(b), (float)Math.Sin(b)) * radius;
                AddTriangle(vertices, center, pa, pb, color);
            }
        }

        private static void AddArc(List<VertexPositionColor> vertices, Vector2 center, float radius, float startAngle, float endAngle, Color color, int segments)
        {
            Vector2 previous = center + new Vector2((float)Math.Cos(startAngle), (float)Math.Sin(startAngle)) * radius;
            for (int i = 1; i <= segments; i++)
            {
                float angle = startAngle + (endAngle - startAngle) * i / segments;
                Vector2 current = center + new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * radius;
                AddLine(vertices, new Vector3(previous, 0), new Vector3(current, 0), color);
                previous = current;
            }
        }

        private static void AddFilledCapsule(List<VertexPositionColor> vertices, Vector2 start, Vector2 end, float radius, Color color, int segments)
        {
            Vector2 direction = end - start;
            float length = direction.Length();
            if (length < 0.001f)
            {
                AddFilledCircle(vertices, start, radius, color, segments);
                return;
            }

            Vector2 normal = direction / length;
            Vector2 tangent = new Vector2(-normal.Y, normal.X);
            Vector2 startUpper = start + tangent * radius;
            Vector2 endUpper = end + tangent * radius;
            Vector2 startLower = start - tangent * radius;
            Vector2 endLower = end - tangent * radius;

            AddTriangle(vertices, startUpper, startLower, endUpper, color);
            AddTriangle(vertices, endUpper, startLower, endLower, color);

            float angle = (float)Math.Atan2(normal.Y, normal.X);
            AddFilledSemicircle(vertices, start, radius, angle + MathHelper.PiOver2, angle + 3f * MathHelper.PiOver2, segments / 2, color);
            AddFilledSemicircle(vertices, end, radius, angle - MathHelper.PiOver2, angle + MathHelper.PiOver2, segments / 2, color);
        }

        private static void AddFilledSemicircle(List<VertexPositionColor> vertices, Vector2 center, float radius, float startAngle, float endAngle, int segments, Color color)
        {
            Vector2 previous = center + new Vector2((float)Math.Cos(startAngle), (float)Math.Sin(startAngle)) * radius;
            for (int i = 1; i <= segments; i++)
            {
                float angle = startAngle + (endAngle - startAngle) * i / segments;
                Vector2 current = center + new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * radius;
                AddTriangle(vertices, center, previous, current, color);
                previous = current;
            }
        }

        private static void AddTriangle(List<VertexPositionColor> vertices, Vector2 a, Vector2 b, Vector2 c, Color color)
        {
            vertices.Add(new VertexPositionColor(new Vector3(a, 0), color));
            vertices.Add(new VertexPositionColor(new Vector3(b, 0), color));
            vertices.Add(new VertexPositionColor(new Vector3(c, 0), color));
        }

        public void RebuildGeometry()
        {
            BuildArenaGeometry();
        }

        public void Draw(Matrix viewMatrix)
        {
            _effect.View = viewMatrix;
            BlendState previousBlendState = _graphicsDevice.BlendState;

            try
            {
                foreach (var pass in _effect.CurrentTechnique.Passes)
                {
                    pass.Apply();

                    if (_fillVertexCount > 0)
                    {
                        _graphicsDevice.BlendState = BlendState.NonPremultiplied;
                        _graphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleList, _fillVertices, 0, _fillVertexCount / 3);
                    }

                    if (_outerGlowVertexCount > 0 || _innerGlowVertexCount > 0)
                    {
                        _graphicsDevice.BlendState = BlendState.Additive;
                        if (_outerGlowVertexCount > 0)
                        {
                            _graphicsDevice.DrawUserPrimitives(PrimitiveType.LineList, _outerGlowVertices, 0, _outerGlowVertexCount / 2);
                        }
                        if (_innerGlowVertexCount > 0)
                        {
                            _graphicsDevice.DrawUserPrimitives(PrimitiveType.LineList, _innerGlowVertices, 0, _innerGlowVertexCount / 2);
                        }
                    }

                    _graphicsDevice.BlendState = previousBlendState;
                    _graphicsDevice.DrawUserPrimitives(PrimitiveType.LineList, _vertices, 0, _vertexCount / 2);
                }
            }
            finally
            {
                _graphicsDevice.BlendState = previousBlendState;
            }
        }
    }
}
