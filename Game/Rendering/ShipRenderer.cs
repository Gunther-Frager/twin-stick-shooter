using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TwinStickShooter.Core;
using TwinStickShooter.Entities;

namespace TwinStickShooter.Rendering
{
    /// <summary>
    /// Dibuja cada Player como un triángulo vectorial orientado (sin texturas,
    /// sin Content Pipeline) usando DrawUserPrimitives + BasicEffect.
    ///
    /// Todos los buffers se alocan UNA vez en el constructor y se reescriben
    /// cada frame in-place: cero allocations por Draw().
    /// </summary>
    public class ShipRenderer
    {
        private const int ShieldSegments = 24;

        // Forma local de la nave (apuntando a +X), en coordenadas centradas en el origen.
        private static readonly Vector2[] ShipTemplate =
        {
            new Vector2(1.0f, 0.0f),    // nariz
            new Vector2(-0.7f, 0.6f),   // atrás-arriba
            new Vector2(-0.7f, -0.6f),  // atrás-abajo
        };

        private readonly BasicEffect _effect;
        private readonly VertexPositionColor[] _shipVertices;
        private readonly VertexPositionColor[] _shieldVertices;

        public ShipRenderer(GraphicsDevice graphicsDevice)
        {
            _effect = new BasicEffect(graphicsDevice)
            {
                VertexColorEnabled = true,
                World = Matrix.Identity,
                View = Matrix.Identity,
                Projection = Matrix.CreateOrthographicOffCenter(
                    0, GameConstants.ScreenWidth,
                    GameConstants.ScreenHeight, 0,
                    0f, 1f)
            };

            _shipVertices = new VertexPositionColor[GameConstants.MaxPlayers * 3];
            _shieldVertices = new VertexPositionColor[GameConstants.MaxPlayers * ShieldSegments * 2];
        }

        public void Draw(GraphicsDevice graphicsDevice, Player[] players)
        {
            int shipVertexCount = 0;
            int shieldVertexCount = 0;

            for (int p = 0; p < players.Length; p++)
            {
                Player player = players[p];

                float cos = (float)Math.Cos(player.FacingAngle);
                float sin = (float)Math.Sin(player.FacingAngle);
                float radius = GameConstants.PlayerRadius;

                for (int v = 0; v < 3; v++)
                {
                    Vector2 local = ShipTemplate[v] * radius;
                    Vector2 rotated = new Vector2(
                        local.X * cos - local.Y * sin,
                        local.X * sin + local.Y * cos);

                    _shipVertices[shipVertexCount++] = new VertexPositionColor(
                        new Vector3(player.Position + rotated, 0f),
                        player.Color);
                }

                if (player.ShieldActive)
                {
                    float shieldRadius = radius * 1.6f;
                    for (int s = 0; s < ShieldSegments; s++)
                    {
                        float a0 = MathHelper.TwoPi * s / ShieldSegments;
                        float a1 = MathHelper.TwoPi * (s + 1) / ShieldSegments;

                        Vector2 p0 = player.Position + new Vector2(
                            (float)Math.Cos(a0), (float)Math.Sin(a0)) * shieldRadius;
                        Vector2 p1 = player.Position + new Vector2(
                            (float)Math.Cos(a1), (float)Math.Sin(a1)) * shieldRadius;

                        _shieldVertices[shieldVertexCount++] =
                            new VertexPositionColor(new Vector3(p0, 0f), Color.White * 0.6f);
                        _shieldVertices[shieldVertexCount++] =
                            new VertexPositionColor(new Vector3(p1, 0f), Color.White * 0.6f);
                    }
                }
            }

            foreach (EffectPass pass in _effect.CurrentTechnique.Passes)
            {
                pass.Apply();

                if (shipVertexCount > 0)
                {
                    graphicsDevice.DrawUserPrimitives(
                        PrimitiveType.TriangleList, _shipVertices, 0, shipVertexCount / 3);
                }

                if (shieldVertexCount > 0)
                {
                    graphicsDevice.DrawUserPrimitives(
                        PrimitiveType.LineList, _shieldVertices, 0, shieldVertexCount / 2);
                }
            }
        }
    }
}
