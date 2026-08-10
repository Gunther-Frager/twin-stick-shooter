using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using TwinStickShooter.Core;

namespace TwinStickShooter.Rendering
{
    /// <summary>
    /// Renderizador del mundo: dibuja la grilla de fondo y los bordes delimitadores del mapa.
    /// Geometría estática generada una sola vez.
    /// </summary>
    public class ArenaRenderer
    {
        private GraphicsDevice _graphicsDevice;
        private BasicEffect _effect;
        private VertexPositionColor[] _vertices;
        private int _vertexCount;
        private LevelManager _levelManager;

        public ArenaRenderer(GraphicsDevice graphicsDevice, LevelManager levelManager)
        {
            _graphicsDevice = graphicsDevice;
            _levelManager = levelManager;
            
            _effect = new BasicEffect(graphicsDevice)
            {
                VertexColorEnabled = true,
                World = Matrix.Identity,
                Projection = Matrix.CreateOrthographicOffCenter(0, GameConstants.ScreenWidth, GameConstants.ScreenHeight, 0, 0, -1)
            };

            BuildArenaGeometry();
        }

        private void BuildArenaGeometry()
        {
            System.Diagnostics.Debug.WriteLine("[ArenaRenderer] Reconstruyendo geometría...");
            List<VertexPositionColor> vertices = new List<VertexPositionColor>();

            float width = GameConstants.WorldWidth;
            float height = GameConstants.WorldHeight;
            float gridSize = 100f;

            Color gridColor = new Color(40, 45, 60);
            Color borderColor = new Color(0, 255, 220);
            Color wallColor = new Color(255, 255, 255);
            
            int wallCount = 0;

            // 1. Líneas de la grilla interna
            for (float x = 0; x <= width; x += gridSize)
            {
                vertices.Add(new VertexPositionColor(new Vector3(x, 0, 0), gridColor));
                vertices.Add(new VertexPositionColor(new Vector3(x, height, 0), gridColor));
            }

            for (float y = 0; y <= height; y += gridSize)
            {
                vertices.Add(new VertexPositionColor(new Vector3(0, y, 0), gridColor));
                vertices.Add(new VertexPositionColor(new Vector3(width, y, 0), gridColor));
            }

            // 2. Borde exterior del mundo
            Vector3 topLeft = new Vector3(0, 0, 0);
            Vector3 topRight = new Vector3(width, 0, 0);
            Vector3 bottomRight = new Vector3(width, height, 0);
            Vector3 bottomLeft = new Vector3(0, height, 0);

            vertices.Add(new VertexPositionColor(topLeft, borderColor));
            vertices.Add(new VertexPositionColor(topRight, borderColor));
            vertices.Add(new VertexPositionColor(topRight, borderColor));
            vertices.Add(new VertexPositionColor(bottomRight, borderColor));
            vertices.Add(new VertexPositionColor(bottomRight, borderColor));
            vertices.Add(new VertexPositionColor(bottomLeft, borderColor));
            vertices.Add(new VertexPositionColor(bottomLeft, borderColor));
            vertices.Add(new VertexPositionColor(topLeft, borderColor));

            // 3. Paredes del LevelManager
            for (int x = 0; x < GameConstants.GridWidth; x++)
            {
                for (int y = 0; y < GameConstants.GridHeight; y++)
                {
                    if (_levelManager.CheckCollision(new Vector2(x * GameConstants.GridCellSize + 1, y * GameConstants.GridCellSize + 1), 1f))
                    {
                        float px = x * GameConstants.GridCellSize;
                        float py = y * GameConstants.GridCellSize;
                        float s = GameConstants.GridCellSize;

                        vertices.Add(new VertexPositionColor(new Vector3(px, py, 0), wallColor));
                        vertices.Add(new VertexPositionColor(new Vector3(px + s, py, 0), wallColor));
                        vertices.Add(new VertexPositionColor(new Vector3(px + s, py, 0), wallColor));
                        vertices.Add(new VertexPositionColor(new Vector3(px + s, py + s, 0), wallColor));
                        vertices.Add(new VertexPositionColor(new Vector3(px + s, py + s, 0), wallColor));
                        vertices.Add(new VertexPositionColor(new Vector3(px, py + s, 0), wallColor));
                        vertices.Add(new VertexPositionColor(new Vector3(px, py + s, 0), wallColor));
                        vertices.Add(new VertexPositionColor(new Vector3(px, py, 0), wallColor));
                        
                        wallCount++;
                    }
                }
            }
            
            System.Diagnostics.Debug.WriteLine("[ArenaRenderer] Paredes detectadas: " + wallCount);

            _vertices = vertices.ToArray();
            _vertexCount = _vertices.Length;
        }

        public void RebuildGeometry()
        {
            BuildArenaGeometry();
        }

        public void Draw(Matrix viewMatrix)
        {
            _effect.View = viewMatrix;

            foreach (var pass in _effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                _graphicsDevice.DrawUserPrimitives(PrimitiveType.LineList, _vertices, 0, _vertexCount / 2);
            }
        }
    }
}
