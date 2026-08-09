using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TwinStickShooter.Core;
using TwinStickShooter.Entities;

namespace TwinStickShooter.Rendering
{
    /// <summary>
    /// Dibuja partículas activas como cuadrados pequeños con alpha que decae
    /// según su vida restante (Life / MaxLife). Buffer pre-alocado a
    /// MaxParticles * 6: cero allocations en Draw().
    /// </summary>
    public class ParticleRenderer
    {
        private readonly BasicEffect _effect;
        private readonly VertexPositionColor[] _vertices;

        public ParticleRenderer(GraphicsDevice graphicsDevice)
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

            _vertices = new VertexPositionColor[GameConstants.MaxParticles * 6];
        }

        public void Draw(GraphicsDevice graphicsDevice, ParticleSystem.Particle[] particles, Matrix viewMatrix)
        {
            _effect.View = viewMatrix;

            int vertexCount = 0;

            for (int i = 0; i < particles.Length; i++)
            {
                ParticleSystem.Particle particle = particles[i];
                if (!particle.Active)
                {
                    continue;
                }

                float lifeRatio = particle.MaxLife > 0f ? particle.Life / particle.MaxLife : 0f;
                Color color = particle.Color * lifeRatio;
                float halfSize = particle.Size * 0.5f;

                Vector2 p = particle.Position;
                Vector3 topLeft = new Vector3(p.X - halfSize, p.Y - halfSize, 0f);
                Vector3 topRight = new Vector3(p.X + halfSize, p.Y - halfSize, 0f);
                Vector3 bottomLeft = new Vector3(p.X - halfSize, p.Y + halfSize, 0f);
                Vector3 bottomRight = new Vector3(p.X + halfSize, p.Y + halfSize, 0f);

                _vertices[vertexCount++] = new VertexPositionColor(topLeft, color);
                _vertices[vertexCount++] = new VertexPositionColor(topRight, color);
                _vertices[vertexCount++] = new VertexPositionColor(bottomLeft, color);

                _vertices[vertexCount++] = new VertexPositionColor(topRight, color);
                _vertices[vertexCount++] = new VertexPositionColor(bottomRight, color);
                _vertices[vertexCount++] = new VertexPositionColor(bottomLeft, color);
            }

            if (vertexCount == 0)
            {
                return;
            }

            foreach (EffectPass pass in _effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                graphicsDevice.DrawUserPrimitives(
                    PrimitiveType.TriangleList, _vertices, 0, vertexCount / 3);
            }
        }
    }
}
