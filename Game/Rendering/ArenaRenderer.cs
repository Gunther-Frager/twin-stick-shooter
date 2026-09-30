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
        private int _builtPrimitiveMapRevision = -1;
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

            foreach (MapRoundedPoly poly in _levelManager.PrimitiveRoundedPolys)
            {
                List<Vector2> contour = GetRoundedPolyPoints(poly, 8);
                AddRoundedPolyOutline(vertices, poly, wallColor, 0f, 8);
                AddRoundedPolyOutline(outerGlowVertices, poly, outerGlowColor, 6f, 8);
                AddRoundedPolyOutline(innerGlowVertices, poly, innerGlowColor, 2.5f, 8);

                // TraceContours returns holes as separate, oppositely wound contours; skip their fill rather than bridging them unsafely.
                if (GetSignedArea(contour) > 0f)
                    TriangulateContour(fillVertices, contour, wallFillColor);
            }

            foreach (ArenaObstacle obstacle in _levelManager.ArenaObstacles)
            {
                MapCapsule capsule = obstacle.Capsule;
                Color outlineColor;
                Color fillColor;
                if (obstacle.IsDestructible)
                {
                    outlineColor = obstacle.OutlineColor;
                    fillColor = new Color(45, 30, 22, 220);
                }
                else
                {
                    outlineColor = obstacle.OutlineColor;
                    fillColor = new Color(28, 35, 44, 235);
                }

                AddFilledCapsule(fillVertices, capsule.Start, capsule.End, capsule.Radius, fillColor, 18);
                AddCapsule(vertices, capsule.Start, capsule.End, capsule.Radius, outlineColor, 18);
                AddCapsule(outerGlowVertices, capsule.Start, capsule.End, capsule.Radius + 5f,
                    new Color((int)outlineColor.R, (int)outlineColor.G, (int)outlineColor.B, 28), 18);
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
            _builtPrimitiveMapRevision = _levelManager.PrimitiveMapRevision;
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

        private static void AddRoundedPolyOutline(List<VertexPositionColor> vertices, MapRoundedPoly poly, Color color, float offset, int arcSegments)
        {
            float signedArea = GetSignedArea(GetRoundedPolyPoints(poly, arcSegments));
            bool clockwiseOnScreen = signedArea >= 0f;

            foreach (MapRoundedPoly.Edge edge in poly.Edges)
            {
                if (edge is MapRoundedPoly.Segment segment)
                {
                    Vector2 direction = segment.B - segment.A;
                    if (direction.LengthSquared() <= 0.0001f)
                        continue;

                    direction.Normalize();
                    Vector2 normal = clockwiseOnScreen
                        ? new Vector2(direction.Y, -direction.X)
                        : new Vector2(-direction.Y, direction.X);
                    AddLine(vertices,
                        new Vector3(segment.A + normal * offset, 0f),
                        new Vector3(segment.B + normal * offset, 0f), color);
                }
                else if (edge is MapRoundedPoly.Arc arc)
                {
                    AddArc(vertices, arc.Center, arc.Radius + offset,
                        arc.StartAngle, arc.EndAngle, color,
                        Math.Max(1, (int)Math.Ceiling(Math.Abs(arc.EndAngle - arc.StartAngle) / MathHelper.TwoPi * arcSegments * 4)));
                }
            }
        }

        private static List<Vector2> GetRoundedPolyPoints(MapRoundedPoly poly, int arcSegments)
        {
            var points = new List<Vector2>();
            foreach (MapRoundedPoly.Edge edge in poly.Edges)
            {
                if (edge is MapRoundedPoly.Segment segment)
                {
                    points.Add(segment.A);
                }
                else if (edge is MapRoundedPoly.Arc arc)
                {
                    float sweep = arc.EndAngle - arc.StartAngle;
                    int segments = Math.Max(1, (int)Math.Ceiling(Math.Abs(sweep) / MathHelper.PiOver2 * arcSegments));
                    for (int i = 1; i < segments; i++)
                    {
                        float angle = arc.StartAngle + sweep * i / segments;
                        points.Add(arc.Center + new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * arc.Radius);
                    }
                }
            }

            return points;
        }

        private static float GetSignedArea(List<Vector2> points)
        {
            float twiceArea = 0f;
            for (int i = 0; i < points.Count; i++)
            {
                Vector2 current = points[i];
                Vector2 next = points[(i + 1) % points.Count];
                twiceArea += current.X * next.Y - next.X * current.Y;
            }

            return twiceArea * 0.5f;
        }

        private static void TriangulateContour(List<VertexPositionColor> vertices, List<Vector2> points, Color color)
        {
            if (points.Count < 3)
                return;

            float area = GetSignedArea(points);
            if (Math.Abs(area) <= 0.0001f)
                return;

            var remaining = new List<int>(points.Count);
            for (int i = 0; i < points.Count; i++)
                remaining.Add(i);

            float winding = Math.Sign(area);
            int attemptsWithoutEar = 0;
            while (remaining.Count > 3 && attemptsWithoutEar < remaining.Count)
            {
                bool clippedEar = false;
                for (int i = 0; i < remaining.Count; i++)
                {
                    int previousIndex = remaining[(i + remaining.Count - 1) % remaining.Count];
                    int currentIndex = remaining[i];
                    int nextIndex = remaining[(i + 1) % remaining.Count];
                    Vector2 previous = points[previousIndex];
                    Vector2 current = points[currentIndex];
                    Vector2 next = points[nextIndex];

                    if (Cross(current - previous, next - current) * winding <= 0.0001f)
                        continue;

                    bool containsVertex = false;
                    foreach (int candidateIndex in remaining)
                    {
                        if (candidateIndex == previousIndex || candidateIndex == currentIndex || candidateIndex == nextIndex)
                            continue;

                        if (PointInTriangle(points[candidateIndex], previous, current, next, winding))
                        {
                            containsVertex = true;
                            break;
                        }
                    }

                    if (containsVertex)
                        continue;

                    AddTriangle(vertices, previous, current, next, color);
                    remaining.RemoveAt(i);
                    clippedEar = true;
                    attemptsWithoutEar = 0;
                    break;
                }

                if (!clippedEar)
                    attemptsWithoutEar++;
            }

            if (remaining.Count == 3)
                AddTriangle(vertices, points[remaining[0]], points[remaining[1]], points[remaining[2]], color);
        }

        private static bool PointInTriangle(Vector2 point, Vector2 a, Vector2 b, Vector2 c, float winding)
        {
            return Cross(b - a, point - a) * winding >= -0.0001f &&
                Cross(c - b, point - b) * winding >= -0.0001f &&
                Cross(a - c, point - c) * winding >= -0.0001f;
        }

        private static float Cross(Vector2 a, Vector2 b)
        {
            return a.X * b.Y - a.Y * b.X;
        }

        public void RebuildGeometry()
        {
            BuildArenaGeometry();
        }

        public void Draw(Matrix viewMatrix)
        {
            if (_builtPrimitiveMapRevision != _levelManager.PrimitiveMapRevision)
                BuildArenaGeometry();

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
