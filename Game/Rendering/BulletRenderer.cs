using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TwinStickShooter.Core;
using TwinStickShooter.Entities;

namespace TwinStickShooter.Rendering
{
    /// <summary>
    /// Dibuja balas activas como pequeños cuadrados (2 triángulos c/u).
    /// Buffer de vértices pre-alocado a MaxBullets * 6, reescrito in-place
    /// cada frame: cero allocations en Draw().
    /// </summary>
    public class BulletRenderer
    {
        private readonly BasicEffect _effect;
        private readonly VertexPositionColor[] _vertices;

        public BulletRenderer(GraphicsDevice graphicsDevice)
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

            _vertices = new VertexPositionColor[GameConstants.MaxBullets * 6];
        }

        public void Draw(GraphicsDevice graphicsDevice, Bullet[] bullets)
        {
            int vertexCount = 0;
            float r = GameConstants.BulletRadius;

            for (int i = 0; i < bullets.Length; i++)
            {
                Bullet bullet = bullets[i];
                if (!bullet.Active)
                {
                    continue;
                }

                Vector2 p = bullet.Position;
                Vector3 topLeft = new Vector3(p.X - r, p.Y - r, 0f);
                Vector3 topRight = new Vector3(p.X + r, p.Y - r, 0f);
                Vector3 bottomLeft = new Vector3(p.X - r, p.Y + r, 0f);
                Vector3 bottomRight = new Vector3(p.X + r, p.Y + r, 0f);

                _vertices[vertexCount++] = new VertexPositionColor(topLeft, bullet.Color);
                _vertices[vertexCount++] = new VertexPositionColor(topRight, bullet.Color);
                _vertices[vertexCount++] = new VertexPositionColor(bottomLeft, bullet.Color);

                _vertices[vertexCount++] = new VertexPositionColor(topRight, bullet.Color);
                _vertices[vertexCount++] = new VertexPositionColor(bottomRight, bullet.Color);
                _vertices[vertexCount++] = new VertexPositionColor(bottomLeft, bullet.Color);
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
