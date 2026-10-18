using Microsoft.Xna.Framework;
using TwinStickShooter.Core;
using TwinStickShooter.Entities;
using Xunit;

namespace TwinStickShooter.Tests;

public class CameraTests
{
    [Fact]
    public void Reset_WithGivenPosition_CentersPositionAndSetsMaxZoom()
    {
        var camera = new Camera();
        Vector2 target = new Vector2(450f, 320f);

        camera.Reset(target);

        Vector2 transformed = Vector2.Transform(target, camera.ViewMatrix);
        Vector2 expectedCenter = new Vector2(GameConstants.ScreenWidth / 2f, GameConstants.ScreenHeight / 2f);

        Assert.Equal(expectedCenter.X, transformed.X, precision: 3);
        Assert.Equal(expectedCenter.Y, transformed.Y, precision: 3);
        Assert.Equal(GameConstants.CameraZoomMax, camera.ViewMatrix.M11, precision: 3);
    }

    [Fact]
    public void Update_WithNullPlayers_DoesNotThrowAndDoesNotChangeViewMatrix()
    {
        var camera = new Camera();
        camera.Reset(new Vector2(200f, 200f));
        Matrix initialMatrix = camera.ViewMatrix;

        camera.Update(null);
        Assert.Equal(initialMatrix, camera.ViewMatrix);
    }

    [Fact]
    public void Update_WithoutActivePlayers_DoesNotChangeViewMatrix()
    {
        var camera = new Camera();
        camera.Reset(new Vector2(300f, 300f));
        Matrix initialMatrix = camera.ViewMatrix;

        var player = new Player(0, Vector2.Zero);
        player.IsActive = false;
        var players = new Player[] { player };

        camera.Update(players);

        Assert.Equal(initialMatrix, camera.ViewMatrix);
    }

    [Fact]
    public void Update_WithSingleActivePlayerFarFromCenter_MovesTransformedPositionCloserToScreenCenter()
    {
        var camera = new Camera();
        camera.Reset(new Vector2(100f, 100f));

        var player = new Player(0, new Vector2(900f, 700f));
        player.IsActive = true;
        var players = new Player[] { player };

        Vector2 screenCenter = new Vector2(GameConstants.ScreenWidth / 2f, GameConstants.ScreenHeight / 2f);
        float initialDist = Vector2.Distance(Vector2.Transform(player.Position, camera.ViewMatrix), screenCenter);

        for (int i = 0; i < 20; i++)
        {
            camera.Update(players);
        }

        float finalDist = Vector2.Distance(Vector2.Transform(player.Position, camera.ViewMatrix), screenCenter);
        Assert.True(finalDist < initialDist, $"Expected final distance ({finalDist}) to be closer to center than initial ({initialDist})");
    }

    [Fact]
    public void Update_WithTwoSeparatedPlayers_DecreasesZoomComparedToOnePlayerAndClampsBetweenMinAndMax()
    {
        // Cámara 1 con un solo jugador
        var cameraSingle = new Camera();
        var p1Single = new Player(0, new Vector2(400f, 400f)) { IsActive = true };
        cameraSingle.Reset(p1Single.Position);
        for (int i = 0; i < 30; i++)
        {
            cameraSingle.Update(new[] { p1Single });
        }

        // Cámara 2 con dos jugadores muy separados
        var cameraDuo = new Camera();
        var p1 = new Player(0, new Vector2(100f, 100f)) { IsActive = true };
        var p2 = new Player(1, new Vector2(1800f, 1200f)) { IsActive = true };
        cameraDuo.Reset(new Vector2(950f, 650f));

        for (int i = 0; i < 30; i++)
        {
            cameraDuo.Update(new[] { p1, p2 });
        }

        float singleZoom = cameraSingle.ViewMatrix.M11;
        float duoZoom = cameraDuo.ViewMatrix.M11;

        Assert.True(duoZoom < singleZoom, $"Expected duo zoom ({duoZoom}) to be less than single zoom ({singleZoom})");
        Assert.InRange(duoZoom, GameConstants.CameraZoomMin - 0.01f, GameConstants.CameraZoomMax + 0.01f);
    }

    [Fact]
    public void VisibleWorldBounds_AfterReset_IsCenteredAroundTargetWithExpectedDimensions()
    {
        var camera = new Camera();
        Vector2 target = new Vector2(600f, 500f);
        camera.Reset(target);

        Rectangle bounds = camera.VisibleWorldBounds;

        float expectedWidth = GameConstants.ScreenWidth / GameConstants.CameraZoomMax;
        float expectedHeight = GameConstants.ScreenHeight / GameConstants.CameraZoomMax;

        Assert.InRange(bounds.Width, expectedWidth - 2f, expectedWidth + 2f);
        Assert.InRange(bounds.Height, expectedHeight - 2f, expectedHeight + 2f);

        Vector2 boundsCenter = new Vector2(bounds.Center.X, bounds.Center.Y);
        Assert.InRange(boundsCenter.X, target.X - 2f, target.X + 2f);
        Assert.InRange(boundsCenter.Y, target.Y - 2f, target.Y + 2f);
    }
}
