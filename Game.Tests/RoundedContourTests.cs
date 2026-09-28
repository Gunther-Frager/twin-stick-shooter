using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using TwinStickShooter.Core;
using Xunit;

namespace TwinStickShooter.Tests;

public class RoundedContourTests
{
    [Fact]
    public void TraceContours_RectangleThreeByOne_HasFourVertices()
    {
        var cells = new[] { new Point(0, 0), new Point(1, 0), new Point(2, 0) };

        List<List<Vector2>> contours = RoundedContour.TraceContours(IsSolid(cells), cells, 10);

        var contour = Assert.Single(contours);
        Assert.Equal(4, contour.Count);
    }

    [Fact]
    public void TraceContours_LShape_HasSixVertices()
    {
        var cells = new[] { new Point(0, 0), new Point(1, 0), new Point(0, 1) };

        List<List<Vector2>> contours = RoundedContour.TraceContours(IsSolid(cells), cells, 10);

        var contour = Assert.Single(contours);
        Assert.Equal(6, contour.Count);
    }

    [Fact]
    public void TraceContours_Ring_ReturnsOuterAndHoleContours()
    {
        var cells = new List<Point>();
        for (int y = 0; y < 3; y++)
        {
            for (int x = 0; x < 3; x++)
            {
                if (x != 1 || y != 1)
                    cells.Add(new Point(x, y));
            }
        }

        List<List<Vector2>> contours = RoundedContour.TraceContours(IsSolid(cells), cells, 10);

        Assert.Equal(2, contours.Count);
    }

    [Fact]
    public void TraceContours_DiagonalTouch_SeparatesContours()
    {
        var cells = new[] { new Point(0, 0), new Point(1, 1) };

        List<List<Vector2>> contours = RoundedContour.TraceContours(IsSolid(cells), cells, 10);

        Assert.Equal(2, contours.Count);
        Assert.All(contours, contour => Assert.Equal(4, contour.Count));
    }

    [Fact]
    public void RoundCorners_ZeroRadius_ReproducesOriginalPolygon()
    {
        var polygon = new List<Vector2>
        {
            new Vector2(0f, 0f),
            new Vector2(4f, 0f),
            new Vector2(4f, 2f),
            new Vector2(0f, 2f)
        };

        MapRoundedPoly rounded = RoundedContour.RoundCorners(polygon, 0f);

        Assert.Equal(polygon.Count, rounded.Edges.Count);
        for (int i = 0; i < polygon.Count; i++)
        {
            var segment = Assert.IsType<MapRoundedPoly.Segment>(rounded.Edges[i]);
            Assert.Equal(polygon[i], segment.A);
            Assert.Equal(polygon[(i + 1) % polygon.Count], segment.B);
        }
    }

    [Fact]
    public void RoundCorners_PositiveRadius_RoundsConvexAndConcaveCorners()
    {
        var lShape = new List<Vector2>
        {
            new Vector2(0f, 0f),
            new Vector2(3f, 0f),
            new Vector2(3f, 1f),
            new Vector2(1f, 1f),
            new Vector2(1f, 3f),
            new Vector2(0f, 3f)
        };

        MapRoundedPoly rounded = RoundedContour.RoundCorners(lShape, 0.4f);

        Assert.Equal(6, rounded.Edges.OfType<MapRoundedPoly.Arc>().Count());
        Assert.Equal(6, rounded.Edges.OfType<MapRoundedPoly.Segment>().Count());
        Assert.All(rounded.Edges.OfType<MapRoundedPoly.Arc>(), arc => Assert.Equal(0.4f, arc.Radius, 4));
    }

    [Fact]
    public void PenetrationAgainst_Segment_ReturnsPerpendicularPushOut()
    {
        var poly = new MapRoundedPoly();
        poly.Edges.Add(new MapRoundedPoly.Segment(new Vector2(0f, 0f), new Vector2(10f, 0f)));

        bool penetrates = RoundedContour.PenetrationAgainst(poly, new Vector2(5f, -0.5f), 1f, out Vector2 pushOut);

        Assert.True(penetrates);
        Assert.Equal(0f, pushOut.X, 4);
        Assert.Equal(-0.5f, pushOut.Y, 4);
    }

    [Fact]
    public void PenetrationAgainst_Arc_ReturnsRadialPushOut()
    {
        var poly = new MapRoundedPoly();
        poly.Edges.Add(new MapRoundedPoly.Arc(Vector2.Zero, 4f, 0f, MathHelper.PiOver2));
        Vector2 radial = Vector2.Normalize(new Vector2(1f, 1f));

        bool penetrates = RoundedContour.PenetrationAgainst(poly, radial * 4.5f, 1f, out Vector2 pushOut);

        Assert.True(penetrates);
        Assert.Equal(0f, pushOut.X * radial.Y - pushOut.Y * radial.X, 4);
        Assert.Equal(0.5f, pushOut.Length(), 4);
    }

    private static System.Func<int, int, bool> IsSolid(IEnumerable<Point> cells)
    {
        var set = new HashSet<Point>(cells);
        return (x, y) => set.Contains(new Point(x, y));
    }
}