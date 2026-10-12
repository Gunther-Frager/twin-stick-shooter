using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TwinStickShooter.Core;

namespace TwinStickShooter.Rendering
{
    public class SpawnerRenderer
    {
        private readonly BasicEffect _effect;
        private readonly VertexPositionColor[] _vertices;

        // La capacidad del buffer debe seguir a la del SpawnerManager para evitar
        // desbordamientos (IndexOutOfRangeException) cuando se manejan más de GameConstants.MaxSpawners.
        public SpawnerRenderer(GraphicsDevice graphicsDevice, int spawnerCapacity = GameConstants.MaxSpawners)
        {
            _effect = new BasicEffect(graphicsDevice)
            {
                VertexColorEnabled = true,
                World = Matrix.Identity,
                Projection = Matrix.CreateOrthographicOffCenter(
                    0, GameConstants.ScreenWidth,
                    GameConstants.ScreenHeight, 0, 0f, 1f)
            };
            _vertices = new VertexPositionColor[System.Math.Max(1, spawnerCapacity) * 6];
        }

        internal static int FillVertices(SpawnerManager.SpawnerData[] spawners, VertexPositionColor[] vertices)
        {
            int vertexCount = 0;
            for (int i = 0; i < spawners.Length; i++)
            {
                if (!spawners[i].Active)
                {
                    continue;
                }

                Vector2 p = spawners[i].Position;
                const float radius = GameConstants.SpawnerRadius;
                Color color = Color.Lerp(Color.DarkRed, Color.Yellow,
                    spawners[i].Health / GameConstants.SpawnerHealth);
                Vector3 topLeft = new Vector3(p.X - radius, p.Y - radius, 0f);
                Vector3 topRight = new Vector3(p.X + radius, p.Y - radius, 0f);
                Vector3 bottomLeft = new Vector3(p.X - radius, p.Y + radius, 0f);
                Vector3 bottomRight = new Vector3(p.X + radius, p.Y + radius, 0f);

                vertices[vertexCount++] = new VertexPositionColor(topLeft, color);
                vertices[vertexCount++] = new VertexPositionColor(topRight, color);
                vertices[vertexCount++] = new VertexPositionColor(bottomLeft, color);
                vertices[vertexCount++] = new VertexPositionColor(topRight, color);
                vertices[vertexCount++] = new VertexPositionColor(bottomRight, color);
                vertices[vertexCount++] = new VertexPositionColor(bottomLeft, color);
            }

            return vertexCount;
        }

        public void Draw(GraphicsDevice graphicsDevice, SpawnerManager.SpawnerData[] spawners, Matrix viewMatrix)
        {
            _effect.View = viewMatrix;
            int vertexCount = FillVertices(spawners, _vertices);

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