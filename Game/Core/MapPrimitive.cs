using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace TwinStickShooter.Core
{
    public class MapCircle
    {
        public Vector2 Center { get; set; }
        public float Radius { get; set; }
    }

    public class MapCapsule
    {
        public Vector2 Start { get; set; }
        public Vector2 End { get; set; }
        public float Radius { get; set; }
    }

    public class MapObstacle
    {
        public string Type { get; set; } = "circle";
        public MapCircle Circle { get; set; }
        public MapCapsule Capsule { get; set; }
    }

    public class MapDefinition
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public List<MapCircle> Circles { get; set; } = new List<MapCircle>();
        public List<MapCapsule> Capsules { get; set; } = new List<MapCapsule>();
        public List<MapObstacle> Obstacles { get; set; } = new List<MapObstacle>();
        public MapPoint Spawn { get; set; }
        public MapPoint Exit { get; set; }
    }

    public class MapPoint
    {
        public int X { get; set; }
        public int Y { get; set; }
    }

    public static class MapCollisionResolver
    {
        public static bool CircleIntersectsCircle(MapCircle a, MapCircle b)
        {
            float radiusSum = a.Radius + b.Radius;
            return Vector2.DistanceSquared(a.Center, b.Center) <= radiusSum * radiusSum;
        }

        public static bool CircleIntersectsCapsule(MapCircle circle, MapCapsule capsule)
        {
            Vector2 closest = ClosestPointOnSegment(circle.Center, capsule.Start, capsule.End);
            float radiusSum = circle.Radius + capsule.Radius;
            return Vector2.DistanceSquared(circle.Center, closest) <= radiusSum * radiusSum;
        }

        private static Vector2 ClosestPointOnSegment(Vector2 point, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float lengthSq = ab.LengthSquared();
            if (lengthSq <= 0.0001f)
                return a;

            float t = Vector2.Dot(point - a, ab) / lengthSq;
            t = MathHelper.Clamp(t, 0f, 1f);
            return a + ab * t;
        }
    }
}
