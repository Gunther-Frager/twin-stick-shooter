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
        private const int ShipSegments = 32;
        private const int ShieldSegments = 24;

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

            // Círculo (ShipSegments * 3) + Marca de dirección (3)
            _shipVertices = new VertexPositionColor[GameConstants.MaxPlayers * (ShipSegments * 3 + 3)];
            _shieldVertices = new VertexPositionColor[GameConstants.MaxPlayers * ShieldSegments * 2];
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

                float radius = GameConstants.PlayerRadius;
                Color shipColor = player.Color;

                // 1. Dibujar el círculo del cuerpo
                for (int s = 0; s < ShipSegments; s++)
                {
                    float a0 = MathHelper.TwoPi * s / ShipSegments;
                    float a1 = MathHelper.TwoPi * (s + 1) / ShipSegments;

                    Vector2 p0 = new Vector2((float)Math.Cos(a0), (float)Math.Sin(a0)) * radius;
                    Vector2 p1 = new Vector2((float)Math.Cos(a1), (float)Math.Sin(a1)) * radius;

                    _shipVertices[shipVertexCount++] = new VertexPositionColor(new Vector3(player.Position, 0f), shipColor);
                    _shipVertices[shipVertexCount++] = new VertexPositionColor(new Vector3(player.Position + p0, 0f), shipColor);
                    _shipVertices[shipVertexCount++] = new VertexPositionColor(new Vector3(player.Position + p1, 0f), shipColor);
                }

                // 2. Dibujar la marca de dirección (un triángulo pequeño que sobresale)
                // La marca apunta hacia FacingAngle
                float cos = (float)Math.Cos(player.FacingAngle);
                float sin = (float)Math.Sin(player.FacingAngle);
                
                // Definimos la marca localmente (apuntando a +X) y la rotamos
                Vector2 mTip = new Vector2(radius * 1.3f, 0f);
                Vector2 mSide1 = new Vector2(radius * 0.8f, radius * 0.4f);
                Vector2 mSide2 = new Vector2(radius * 0.8f, -radius * 0.4f);

                Vector2[] markPoints = { mTip, mSide1, mSide2 };
                foreach (var pt in markPoints)
                {
                    Vector2 rotated = new Vector2(
                        pt.X * cos - pt.Y * sin,
                        pt.X * sin + pt.Y * cos);
                    
                    _shipVertices[shipVertexCount++] = new VertexPositionColor(
                        new Vector3(player.Position + rotated, 0f), 
                        Color.White); // Marca blanca para que resalte
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
