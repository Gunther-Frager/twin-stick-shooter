using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TwinStickShooter.Core;
using TwinStickShooter.Rendering;
using Xunit;

namespace TwinStickShooter.Tests;

public class SpawnerRendererTests
{
    [Fact]
    public void FillVertices_With20ActiveSpawnersInCapacity32_Returns120AndDoesNotThrow()
    {
        var spawners = new SpawnerManager.SpawnerData[32];
        for (int i = 0; i < 20; i++)
        {
            spawners[i] = new SpawnerManager.SpawnerData
            {
                Active = true,
                Position = new Vector2(i * 10f, i * 10f),
                Health = GameConstants.SpawnerHealth
            };
        }

        var vertices = new VertexPositionColor[32 * 6];

        int written = SpawnerRenderer.FillVertices(spawners, vertices);

        Assert.Equal(120, written);
    }

    [Fact]
    public void FillVertices_WithInactiveSpawnersInterleaved_CountsOnlyActive()
    {
        var spawners = new SpawnerManager.SpawnerData[10];
        // Activamos posiciones pares (0, 2, 4, 6, 8) -> 5 activos
        for (int i = 0; i < 10; i++)
        {
            spawners[i] = new SpawnerManager.SpawnerData
            {
                Active = (i % 2 == 0),
                Position = new Vector2(i * 10f, i * 10f),
                Health = GameConstants.SpawnerHealth
            };
        }

        var vertices = new VertexPositionColor[10 * 6];

        int written = SpawnerRenderer.FillVertices(spawners, vertices);

        Assert.Equal(30, written);
    }

    [Fact]
    public void FillVertices_WithZeroActive_ReturnsZero()
    {
        var spawners = new SpawnerManager.SpawnerData[16];
        for (int i = 0; i < 16; i++)
        {
            spawners[i] = new SpawnerManager.SpawnerData
            {
                Active = false,
                Position = new Vector2(i * 5f, i * 5f),
                Health = GameConstants.SpawnerHealth
            };
        }

        var vertices = new VertexPositionColor[16 * 6];

        int written = SpawnerRenderer.FillVertices(spawners, vertices);

        Assert.Equal(0, written);
    }
}

