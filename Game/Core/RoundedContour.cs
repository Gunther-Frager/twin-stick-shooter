using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace TwinStickShooter.Core
{
    public static class RoundedContour
    {
        private const float Epsilon = 0.0001f;

        private readonly struct GridEdge
        {
            public Point Start { get; }
            public Point End { get; }

            public GridEdge(Point start, Point end)
            {
                Start = start;
                End = end;
            }
        }

        public static List<List<Vector2>> TraceContours(Func<int, int, bool> isSolid, IEnumerable<Point> regionCells, int cellSize)
        {
            if (isSolid == null)
                throw new ArgumentNullException(nameof(isSolid));
            if (regionCells == null)
                throw new ArgumentNullException(nameof(regionCells));
            if (cellSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(cellSize));

            var solidCells = new HashSet<Point>();
            foreach (Point cell in regionCells)
            {
                if (isSolid(cell.X, cell.Y))
                    solidCells.Add(cell);
            }

            var edges = new List<GridEdge>();
            foreach (Point cell in solidCells)
            {
                if (!solidCells.Contains(new Point(cell.X, cell.Y - 1)))
                    edges.Add(new GridEdge(new Point(cell.X, cell.Y), new Point(cell.X + 1, cell.Y)));
                if (!solidCells.Contains(new Point(cell.X + 1, cell.Y)))
                    edges.Add(new GridEdge(new Point(cell.X + 1, cell.Y), new Point(cell.X + 1, cell.Y + 1)));
                if (!solidCells.Contains(new Point(cell.X, cell.Y + 1)))
                    edges.Add(new GridEdge(new Point(cell.X + 1, cell.Y + 1), new Point(cell.X, cell.Y + 1)));
                if (!solidCells.Contains(new Point(cell.X - 1, cell.Y)))
                    edges.Add(new GridEdge(new Point(cell.X, cell.Y + 1), new Point(cell.X, cell.Y)));
            }

            var outgoing = new Dictionary<Point, List<int>>();
            for (int i = 0; i < edges.Count; i++)
            {
                if (!outgoing.TryGetValue(edges[i].Start, out List<int> atVertex))
                {
                    atVertex = new List<int>();
                    outgoing.Add(edges[i].Start, atVertex);
                }
                atVertex.Add(i);
            }

            var used = new bool[edges.Count];
            var contours = new List<List<Vector2>>();
            for (int startIndex = 0; startIndex < edges.Count; startIndex++)
            {
                if (used[startIndex])
                    continue;

                var path = new List<int>();
                int currentIndex = startIndex;
                bool closed = false;
                for (int step = 0; step <= edges.Count; step++)
                {
                    if (used[currentIndex])
                        break;

                    used[currentIndex] = true;
                    path.Add(currentIndex);
                    GridEdge current = edges[currentIndex];
                    if (current.End == edges[startIndex].Start)
                    {
                        closed = true;
                        break;
                    }

                    if (!outgoing.TryGetValue(current.End, out List<int> candidates))
                        break;

                    currentIndex = ChooseNextEdge(edges, candidates, used, current);
                    if (currentIndex < 0)
                        break;
                }

                if (!closed)
                    continue;

                var contour = new List<Vector2>();
                for (int i = 0; i < path.Count; i++)
                {
                    GridEdge edge = edges[path[i]];
                    GridEdge next = edges[path[(i + 1) % path.Count]];
                    if (GetDirection(edge) != GetDirection(next))
                        contour.Add(new Vector2(edge.End.X * cellSize, edge.End.Y * cellSize));
                }

                if (contour.Count >= 3)
                    contours.Add(contour);
            }

            return contours;
        }

        private static int ChooseNextEdge(List<GridEdge> edges, List<int> candidates, bool[] used, GridEdge incoming)
        {
            int incomingDirection = GetDirection(incoming);
            int bestIndex = -1;
            int bestPriority = int.MaxValue;

            foreach (int candidateIndex in candidates)
            {
                if (used[candidateIndex])
                    continue;

                int turn = (GetDirection(edges[candidateIndex]) - incomingDirection + 4) % 4;
                int priority = turn == 1 ? 0 : turn == 0 ? 1 : turn == 3 ? 2 : 3;
                if (priority < bestPriority)
                {
                    bestPriority = priority;
                    bestIndex = candidateIndex;
                }
            }

            return bestIndex;
        }

        private static int GetDirection(GridEdge edge)
        {
            int deltaX = edge.End.X - edge.Start.X;
            int deltaY = edge.End.Y - edge.Start.Y;
            if (deltaX > 0) return 0;
            if (deltaY > 0) return 1;
            if (deltaX < 0) return 2;
            return 3;
        }

        public static MapRoundedPoly RoundCorners(List<Vector2> contour, float radius)
        {
            if (contour == null)
                throw new ArgumentNullException(nameof(contour));

            var result = new MapRoundedPoly();
            int count = contour.Count;
            if (count < 3)
                return result;

            if (radius <= 0f)
            {
                for (int i = 0; i < count; i++)
                    result.Edges.Add(new MapRoundedPoly.Segment(contour[i], contour[(i + 1) % count]));
                return result;
            }

            var incomingPoints = new Vector2[count];
            var outgoingPoints = new Vector2[count];
            var arcs = new MapRoundedPoly.Arc[count];

            for (int i = 0; i < count; i++)
            {
                Vector2 previous = contour[(i + count - 1) % count];
                Vector2 vertex = contour[i];
                Vector2 next = contour[(i + 1) % count];
                Vector2 incoming = vertex - previous;
                Vector2 outgoing = next - vertex;
                float incomingLength = incoming.Length();
                float outgoingLength = outgoing.Length();

                if (incomingLength <= Epsilon || outgoingLength <= Epsilon)
                {
                    incomingPoints[i] = vertex;
                    outgoingPoints[i] = vertex;
                    continue;
                }

                incoming /= incomingLength;
                outgoing /= outgoingLength;
                float cross = Cross(incoming, outgoing);
                float turnAngle = MathF.Acos(MathHelper.Clamp(Vector2.Dot(incoming, outgoing), -1f, 1f));
                if (MathF.Abs(cross) <= Epsilon || turnAngle <= Epsilon)
                {
                    incomingPoints[i] = vertex;
                    outgoingPoints[i] = vertex;
                    continue;
                }

                float trim = MathF.Min(radius, MathF.Min(incomingLength, outgoingLength) * 0.5f);
                if (trim <= Epsilon)
                {
                    incomingPoints[i] = vertex;
                    outgoingPoints[i] = vertex;
                    continue;
                }

                incomingPoints[i] = vertex - incoming * trim;
                outgoingPoints[i] = vertex + outgoing * trim;

                float arcRadius = trim / MathF.Tan(turnAngle * 0.5f);
                Vector2 tangentStart = incomingPoints[i];
                Vector2 tangentEnd = outgoingPoints[i];
                Vector2 leftNormal = new Vector2(-incoming.Y, incoming.X);
                Vector2 center = tangentStart + leftNormal * (MathF.Sign(cross) * arcRadius);
                float startAngle = MathF.Atan2(tangentStart.Y - center.Y, tangentStart.X - center.X);
                float endAngle = MathF.Atan2(tangentEnd.Y - center.Y, tangentEnd.X - center.X);

                if (cross > 0f)
                {
                    while (endAngle < startAngle) endAngle += MathHelper.TwoPi;
                }
                else
                {
                    while (endAngle > startAngle) endAngle -= MathHelper.TwoPi;
                }

                arcs[i] = new MapRoundedPoly.Arc(center, arcRadius, startAngle, endAngle);
            }

            for (int i = 0; i < count; i++)
            {
                int nextIndex = (i + 1) % count;
                if (Vector2.DistanceSquared(outgoingPoints[i], incomingPoints[nextIndex]) > Epsilon * Epsilon)
                    result.Edges.Add(new MapRoundedPoly.Segment(outgoingPoints[i], incomingPoints[nextIndex]));
                if (arcs[nextIndex] != null)
                    result.Edges.Add(arcs[nextIndex]);
            }

            return result;
        }

        public static void ClosestPoint(MapRoundedPoly poly, Vector2 p, out Vector2 closest, out Vector2 normal)
        {
            if (poly == null)
                throw new ArgumentNullException(nameof(poly));

            float bestDistanceSquared = float.MaxValue;
            closest = p;
            normal = Vector2.UnitX;

            foreach (MapRoundedPoly.Edge edge in poly.Edges)
            {
                Vector2 candidate;
                Vector2 candidateNormal;
                if (edge is MapRoundedPoly.Segment segment)
                {
                    Vector2 direction = segment.B - segment.A;
                    float lengthSquared = direction.LengthSquared();
                    float amount = lengthSquared <= Epsilon ? 0f : Vector2.Dot(p - segment.A, direction) / lengthSquared;
                    amount = MathHelper.Clamp(amount, 0f, 1f);
                    candidate = segment.A + direction * amount;
                    candidateNormal = lengthSquared <= Epsilon
                        ? Vector2.UnitX
                        : Vector2.Normalize(new Vector2(direction.Y, -direction.X));
                }
                else if (edge is MapRoundedPoly.Arc arc)
                {
                    float angle = MathF.Atan2(p.Y - arc.Center.Y, p.X - arc.Center.X);
                    if (IsAngleOnArc(angle, arc.StartAngle, arc.EndAngle))
                    {
                        Vector2 radial = p - arc.Center;
                        if (radial.LengthSquared() <= Epsilon)
                            angle = arc.StartAngle;
                        candidate = arc.Center + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * arc.Radius;
                    }
                    else
                    {
                        Vector2 start = PointOnArc(arc, arc.StartAngle);
                        Vector2 end = PointOnArc(arc, arc.EndAngle);
                        candidate = Vector2.DistanceSquared(p, start) <= Vector2.DistanceSquared(p, end) ? start : end;
                    }

                    Vector2 arcRadial = candidate - arc.Center;
                    candidateNormal = arcRadial.LengthSquared() <= Epsilon
                        ? Vector2.UnitX
                        : Vector2.Normalize(arcRadial) * MathF.Sign(arc.EndAngle - arc.StartAngle);
                }
                else
                {
                    continue;
                }

                float distanceSquared = Vector2.DistanceSquared(p, candidate);
                if (distanceSquared < bestDistanceSquared)
                {
                    bestDistanceSquared = distanceSquared;
                    closest = candidate;
                    normal = candidateNormal;
                }
            }
        }

        public static bool PenetrationAgainst(MapRoundedPoly poly, Vector2 center, float radius, out Vector2 pushOut)
        {
            pushOut = Vector2.Zero;
            if (radius <= 0f)
                return false;

            ClosestPoint(poly, center, out Vector2 closest, out Vector2 normal);
            Vector2 offset = center - closest;
            float distanceSquared = offset.LengthSquared();
            if (distanceSquared > radius * radius)
                return false;

            float distance = MathF.Sqrt(distanceSquared);
            pushOut = distance <= Epsilon
                ? normal * radius
                : offset * ((radius - distance) / distance);
            return true;
        }

        private static bool IsAngleOnArc(float angle, float startAngle, float endAngle)
        {
            float sweep = endAngle - startAngle;
            if (sweep >= 0f)
                return NormalizePositive(angle - startAngle) <= sweep + Epsilon;

            return NormalizePositive(startAngle - angle) <= -sweep + Epsilon;
        }

        private static float NormalizePositive(float angle)
        {
            while (angle < 0f) angle += MathHelper.TwoPi;
            while (angle >= MathHelper.TwoPi) angle -= MathHelper.TwoPi;
            return angle;
        }

        private static Vector2 PointOnArc(MapRoundedPoly.Arc arc, float angle)
        {
            return arc.Center + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * arc.Radius;
        }

        private static float Cross(Vector2 a, Vector2 b)
        {
            return a.X * b.Y - a.Y * b.X;
        }
    }
}