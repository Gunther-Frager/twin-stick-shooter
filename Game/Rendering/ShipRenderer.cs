using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TwinStickShooter.Core;
using TwinStickShooter.Entities;

namespace TwinStickShooter.Rendering
{
    /// <summary>
    /// Dibuja cada Player como un círculo con una hendidura semicircular en el frente
    /// (sin texturas, sin Content Pipeline) usando DrawUserPrimitives + BasicEffect.
    ///
    /// Todos los buffers se alocan UNA vez en el constructor y se reescriben
    /// cada frame in-place: cero allocations por Draw().
    /// </summary>
    public class ShipRenderer
    {
        private const int ShipSegments = 32;
        private const int ShieldSegments = 24;

        private readonly BasicEffect _effect;
        private readonly VertexPositionColor[] _shipVertices;
        private readonly VertexPositionColor[] _shieldVertices;
        private readonly Vector2[] _localShipPoints;

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

            _shipVertices = new VertexPositionColor[GameConstants.MaxPlayers * ShipSegments * 3];
            _shieldVertices = new VertexPositionColor[GameConstants.MaxPlayers * ShieldSegments * 2];

            // Precalcular los puntos locales de la nave con la hendidura semicircular en el frente
            _localShipPoints = new Vector2[ShipSegments];
            float theta = MathHelper.ToRadians(40f);
            float radius = GameConstants.PlayerRadius;

            // 1. Parte circular exterior (24 segmentos, de theta a 2pi - theta)
            int outerSegments = 24;
            float startAngle = theta;
            float endAngle = MathHelper.TwoPi - theta;
            for (int i = 0; i <= outerSegments; i++)
            {
                float angle = startAngle + (endAngle - startAngle) * i / outerSegments;
                Vector2 pt = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * radius;
                if (i < outerSegments)
                {
                    _localShipPoints[i] = pt;
                }
            }

            // 2. Hendidura semicircular (8 segmentos, de 3pi/2 a pi/2 en el círculo de la hendidura)
            int innerSegments = 8;
            float xc = radius * (float)Math.Cos(theta);
            float r = radius * (float)Math.Sin(theta);
            for (int i = 0; i < innerSegments; i++)
            {
                float phi = 1.5f * MathHelper.Pi - (MathHelper.Pi * i / innerSegments);
                Vector2 pt = new Vector2(
                    xc + r * (float)Math.Cos(phi),
                    r * (float)Math.Sin(phi)
                );
                _localShipPoints[outerSegments + i] = pt;
            }
        }

        public void Draw(GraphicsDevice graphicsDevice, Player[] players, Matrix viewMatrix)
        {
            _effect.View = viewMatrix;

            int shipVertexCount = 0;
            int shieldVertexCount = 0;

            for (int p = 0; p < players.Length; p++)
            {
                Player player = players[p];
                if (!player.IsActive) continue;

                Color shipColor = player.Color;

                // Dibujar el cuerpo de la nave usando los puntos locales rotados
                float cos = (float)Math.Cos(player.FacingAngle);
                float sin = (float)Math.Sin(player.FacingAngle);

                for (int s = 0; s < ShipSegments; s++)
                {
                    Vector2 p0 = _localShipPoints[s];
                    Vector2 p1 = _localShipPoints[(s + 1) % ShipSegments];

                    Vector2 r0 = new Vector2(
                        p0.X * cos - p0.Y * sin,
                        p0.X * sin + p0.Y * cos
                    );
                    Vector2 r1 = new Vector2(
                        p1.X * cos - p1.Y * sin,
                        p1.X * sin + p1.Y * cos
                    );

                    _shipVertices[shipVertexCount++] = new VertexPositionColor(new Vector3(player.Position, 0f), shipColor);
                    _shipVertices[shipVertexCount++] = new VertexPositionColor(new Vector3(player.Position + r0, 0f), shipColor);
                    _shipVertices[shipVertexCount++] = new VertexPositionColor(new Vector3(player.Position + r1, 0f), shipColor);
                }

                if (player.ShieldActive)
                {
                    float radius = GameConstants.PlayerRadius;
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
