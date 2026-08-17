using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TwinStickShooter.Core;
using TwinStickShooter.Entities;

namespace TwinStickShooter.Rendering
{
    /// <summary>
    /// Dibuja enemigos activos como pequeños cuadrados (2 triángulos c/u).
    /// Buffer de vértices pre-alocado a MaxEnemies * 6, reescrito in-place
    /// cada frame: cero allocations en Draw().
    /// </summary>
    public class EnemyRenderer
    {
        private readonly BasicEffect _effect;
        private readonly VertexPositionColor[] _vertices;

        public EnemyRenderer(GraphicsDevice graphicsDevice)
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

            _vertices = new VertexPositionColor[GameConstants.MaxEnemies * 6];
        }

        public void Draw(GraphicsDevice graphicsDevice, Enemy[] enemies, Matrix viewMatrix)
        {
            _effect.View = viewMatrix;

            int vertexCount = 0;
            int activeEnemies = 0;

            for (int i = 0; i < enemies.Length; i++)
            {
                Enemy enemy = enemies[i];
                if (!enemy.Active)
                {
                    continue;
                }
                activeEnemies++;
                Console.WriteLine($"[EnemyRenderer] Dibujando enemigo en posición: {enemy.Position}");

                Vector2 p = enemy.Position;
                float r = enemy.Radius;
                Vector3 topLeft = new Vector3(p.X - r, p.Y - r, 0f);
                Vector3 topRight = new Vector3(p.X + r, p.Y - r, 0f);
                Vector3 bottomLeft = new Vector3(p.X - r, p.Y + r, 0f);
                Vector3 bottomRight = new Vector3(p.X + r, p.Y + r, 0f);

                _vertices[vertexCount++] = new VertexPositionColor(topLeft, enemy.Color);
                _vertices[vertexCount++] = new VertexPositionColor(topRight, enemy.Color);
                _vertices[vertexCount++] = new VertexPositionColor(bottomLeft, enemy.Color);

                _vertices[vertexCount++] = new VertexPositionColor(topRight, enemy.Color);
                _vertices[vertexCount++] = new VertexPositionColor(bottomRight, enemy.Color);
                _vertices[vertexCount++] = new VertexPositionColor(bottomLeft, enemy.Color);
            }

            Console.WriteLine($"[EnemyRenderer] Enemigos activos para dibujar: {activeEnemies}");
            if (vertexCount == 0)
            {
                Console.WriteLine("[EnemyRenderer] No hay enemigos para dibujar.");
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