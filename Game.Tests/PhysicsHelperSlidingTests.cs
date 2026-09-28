using Microsoft.Xna.Framework;
using TwinStickShooter.Core;
using Xunit;

namespace TwinStickShooter.Tests;

public class PhysicsHelperSlidingTests
{
    [Fact]
    public void MoveWithCollision_ShouldSlideWhenDiagonalMovementCatchesOnLWallCorner()
    {
        var level = new LevelManager(7, 7, 16)
        {
            UseRoundedContours = false
        };
        level.SetCollision(2, 2, true);
        level.SetCollision(3, 2, true);
        level.SetCollision(4, 2, true);
        level.SetCollision(4, 3, true);
        level.SetCollision(4, 4, true);
        level.RebuildPrimitiveMapFromGrid();

        var entity = new TestEntity(new Vector2(56f, 56f), 4f);
        Vector2 moveDelta = new Vector2(10f, -10f);

        Assert.False(level.CheckCollision(entity.Position, entity.Radius));
        Assert.True(level.CheckCollision(entity.Position + new Vector2(moveDelta.X, 0f), entity.Radius));
        Assert.True(level.CheckCollision(entity.Position + new Vector2(0f, moveDelta.Y), entity.Radius));
        Assert.True(level.CheckCollision(entity.Position + moveDelta, entity.Radius));

        Vector2 result = PhysicsHelper.MoveWithCollision(entity, moveDelta, level);

        Assert.NotEqual(entity.Position, result);
        Assert.False(level.CheckCollision(result, entity.Radius));
    }

    [Fact]
    public void MoveWithCollision_ShouldSlideAlongStraightWallAtTenDegrees()
    {
        var level = new LevelManager(7, 7, 16);
        var map = new MapDefinition();
        map.Capsules.Add(new MapCapsule
        {
            Start = new Vector2(60f, 0f),
            End = new Vector2(60f, 200f),
            Radius = 4f
        });
        level.SetPrimitiveMap(map);
        var entity = new TestEntity(new Vector2(40f, 100f), 5f);
        Vector2 moveDelta = new Vector2(24f, 24f * (float)System.Math.Tan(System.Math.PI / 18d));

        Vector2 result = PhysicsHelper.MoveWithCollision(entity, moveDelta, level);

        Assert.True(result.Y > entity.Position.Y + 3f);
        Assert.False(level.CheckCollision(result, entity.Radius));
    }

    [Fact]
    public void MoveWithCollision_ShouldMoveAroundRoundedCorner()
    {
        var level = new LevelManager(7, 7, 16)
        {
            UseRoundedContours = true
        };
        for (int x = 2; x <= 3; x++)
        {
            for (int y = 2; y <= 3; y++)
                level.SetCollision(x, y, true);
        }
        level.RebuildPrimitiveMapFromGrid();

        var entity = new TestEntity(new Vector2(27.8f, 37.6f), 4f);
        Vector2 result = PhysicsHelper.MoveWithCollision(entity, new Vector2(9.8f, -9.8f), level);

        Assert.True(result.Y < entity.Position.Y - 4f);
        Assert.False(level.CheckCollision(result, entity.Radius));
    }

    [Fact]
    public void MoveWithCollision_ShouldNotPassThroughPerpendicularWall()
    {
        var level = new LevelManager(7, 7, 16);
        var map = new MapDefinition();
        map.Capsules.Add(new MapCapsule
        {
            Start = new Vector2(60f, 0f),
            End = new Vector2(60f, 200f),
            Radius = 4f
        });
        level.SetPrimitiveMap(map);
        var entity = new TestEntity(new Vector2(40f, 100f), 5f);

        Vector2 result = PhysicsHelper.MoveWithCollision(entity, new Vector2(25f, 0f), level);

        Assert.True(result.X < 60f);
        Assert.False(level.CheckCollision(result, entity.Radius));
    }

    private sealed class TestEntity : IEntity
    {
        public TestEntity(Vector2 position, float radius)
        {
            Position = position;
            Radius = radius;
            Active = true;
            Color = Color.White;
        }

        public Vector2 Position { get; set; }
        public float Radius { get; }
        public bool Active { get; set; }
        public Color Color { get; set; }
    }
}