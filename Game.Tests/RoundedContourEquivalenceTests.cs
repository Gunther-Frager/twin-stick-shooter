using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using TwinStickShooter.Core;
using Xunit;

namespace TwinStickShooter.Tests;

public class RoundedContourEquivalenceTests
{
    private const float Epsilon = 0.0001f;

    /// <summary>
    /// Copia idéntica de la implementación de referencia anterior de ClosestPoint.
    /// </summary>
    private static void ClosestPointReference(MapRoundedPoly poly, Vector2 p, out Vector2 closest, out Vector2 normal)
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

    [Fact]
    public void ClosestPoint_GeneratedMapPolygons_MatchesReferenceImplementation()
    {
        // Usa la seed por defecto (misma que el golden master).
        var level = new LevelManager(GameConstants.GridWidth, GameConstants.GridHeight, GameConstants.GridCellSize);
        level.GenerateProceduralMap();

        IReadOnlyList<MapRoundedPoly> polys = level.PrimitiveRoundedPolys;
        Assert.NotEmpty(polys);

        var rnd = new Random(42);
        const int queryCount = 20_000;

        for (int i = 0; i < queryCount; i++)
        {
            MapRoundedPoly poly = polys[rnd.Next(polys.Count)];
            Vector2 p = new Vector2(
                (float)(rnd.NextDouble() * level.WorldWidth),
                (float)(rnd.NextDouble() * level.WorldHeight));

            ClosestPointReference(poly, p, out Vector2 expectedClosest, out Vector2 expectedNormal);
            RoundedContour.ClosestPoint(poly, p, out Vector2 actualClosest, out Vector2 actualNormal);

            Assert.True(
                Vector2.Distance(expectedClosest, actualClosest) <= 1e-4f,
                $"Closest mismatch at query {i}: expected {expectedClosest}, got {actualClosest}");

            Assert.True(
                Vector2.Distance(expectedNormal, actualNormal) <= 1e-4f,
                $"Normal mismatch at query {i}: expected {expectedNormal}, got {actualNormal}");
        }
    }

    [Fact]
    public void ClosestPoint_SyntheticPolygons_MatchesReferenceImplementation()
    {
        // 1. Rectángulo simple con 4 segmentos
        var rectPoly = new MapRoundedPoly();
        rectPoly.Edges.Add(new MapRoundedPoly.Segment(new Vector2(0, 0), new Vector2(100, 0)));
        rectPoly.Edges.Add(new MapRoundedPoly.Segment(new Vector2(100, 0), new Vector2(100, 50)));
        rectPoly.Edges.Add(new MapRoundedPoly.Segment(new Vector2(100, 50), new Vector2(0, 50)));
        rectPoly.Edges.Add(new MapRoundedPoly.Segment(new Vector2(0, 50), new Vector2(0, 0)));

        // 2. Forma en L
        var lPoly = new MapRoundedPoly();
        lPoly.Edges.Add(new MapRoundedPoly.Segment(new Vector2(0, 0), new Vector2(50, 0)));
        lPoly.Edges.Add(new MapRoundedPoly.Segment(new Vector2(50, 0), new Vector2(50, 50)));
        lPoly.Edges.Add(new MapRoundedPoly.Segment(new Vector2(50, 50), new Vector2(100, 50)));
        lPoly.Edges.Add(new MapRoundedPoly.Segment(new Vector2(100, 50), new Vector2(100, 100)));
        lPoly.Edges.Add(new MapRoundedPoly.Segment(new Vector2(100, 100), new Vector2(0, 100)));
        lPoly.Edges.Add(new MapRoundedPoly.Segment(new Vector2(0, 100), new Vector2(0, 0)));

        // 3. Polígono con arcos en las esquinas
        var roundedPoly = new MapRoundedPoly();
        roundedPoly.Edges.Add(new MapRoundedPoly.Segment(new Vector2(20, 10), new Vector2(80, 10)));
        roundedPoly.Edges.Add(new MapRoundedPoly.Arc(new Vector2(80, 20), 10f, -MathHelper.PiOver2, 0f));
        roundedPoly.Edges.Add(new MapRoundedPoly.Segment(new Vector2(90, 20), new Vector2(90, 80)));
        roundedPoly.Edges.Add(new MapRoundedPoly.Arc(new Vector2(80, 80), 10f, 0f, MathHelper.PiOver2));
        roundedPoly.Edges.Add(new MapRoundedPoly.Segment(new Vector2(80, 90), new Vector2(20, 90)));
        roundedPoly.Edges.Add(new MapRoundedPoly.Arc(new Vector2(20, 80), 10f, MathHelper.PiOver2, MathHelper.Pi));
        roundedPoly.Edges.Add(new MapRoundedPoly.Segment(new Vector2(10, 80), new Vector2(10, 20)));
        roundedPoly.Edges.Add(new MapRoundedPoly.Arc(new Vector2(20, 20), 10f, MathHelper.Pi, MathHelper.Pi * 1.5f));

        MapRoundedPoly[] syntheticPolys = { rectPoly, lPoly, roundedPoly };
        var rnd = new Random(999);

        foreach (MapRoundedPoly poly in syntheticPolys)
        {
            for (int i = 0; i < 5000; i++)
            {
                Vector2 p = new Vector2(
                    (float)(rnd.NextDouble() * 200.0 - 50.0),
                    (float)(rnd.NextDouble() * 200.0 - 50.0));

                ClosestPointReference(poly, p, out Vector2 expectedClosest, out Vector2 expectedNormal);
                RoundedContour.ClosestPoint(poly, p, out Vector2 actualClosest, out Vector2 actualNormal);

                Assert.True(
                    Vector2.Distance(expectedClosest, actualClosest) <= 1e-4f,
                    $"Closest mismatch on synthetic poly: expected {expectedClosest}, got {actualClosest}");

                Assert.True(
                    Vector2.Distance(expectedNormal, actualNormal) <= 1e-4f,
                    $"Normal mismatch on synthetic poly: expected {expectedNormal}, got {actualNormal}");
            }
        }
    }
}
