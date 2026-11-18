using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace TwinStickShooter.Core
{
    public class DebugConsole
    {
        private const int MaxMessages = 15;
        private const int LineHeight = 12;
        private const int Padding = 10;
        private const int CharWidth = 6;
        private const int CharHeight = 8;

        private readonly List<string> _messages = new List<string>();
        private SpriteBatch _spriteBatch;
        private Texture2D _pixel;
        private Texture2D _glyphAtlas;
        private Dictionary<char, Rectangle> _glyphRectangles;
        private bool _isVisible = true;

        public readonly struct GlyphAtlasData
        {
            public int Width { get; }
            public int Height { get; }
            public Color[] Pixels { get; }
            public Dictionary<char, Rectangle> SourceRectangles { get; }

            public GlyphAtlasData(int width, int height, Color[] pixels, Dictionary<char, Rectangle> sourceRectangles)
            {
                Width = width;
                Height = height;
                Pixels = pixels;
                SourceRectangles = sourceRectangles;
            }
        }

        // Mini font 5x7 definida por bitmasks (5 columnas de 7 bits)
        internal static readonly Dictionary<char, uint[]> FontData = new Dictionary<char, uint[]>
        {
            {'A', new uint[]{0x7E, 0x09, 0x09, 0x09, 0x7E}},
            {'B', new uint[]{0x7F, 0x49, 0x49, 0x49, 0x36}},
            {'C', new uint[]{0x3E, 0x41, 0x41, 0x41, 0x22}},
            {'D', new uint[]{0x7F, 0x41, 0x41, 0x22, 0x1C}},
            {'E', new uint[]{0x7F, 0x49, 0x49, 0x49, 0x41}},
            {'F', new uint[]{0x7F, 0x09, 0x09, 0x09, 0x01}},
            {'G', new uint[]{0x3E, 0x41, 0x49, 0x49, 0x7A}},
            {'H', new uint[]{0x7F, 0x08, 0x08, 0x08, 0x7F}},
            {'I', new uint[]{0x00, 0x41, 0x7F, 0x41, 0x00}},
            {'J', new uint[]{0x20, 0x40, 0x41, 0x3F, 0x01}},
            {'K', new uint[]{0x7F, 0x08, 0x14, 0x22, 0x41}},
            {'L', new uint[]{0x7F, 0x40, 0x40, 0x40, 0x40}},
            {'M', new uint[]{0x7F, 0x02, 0x0C, 0x02, 0x7F}},
            {'N', new uint[]{0x7F, 0x04, 0x08, 0x10, 0x7F}},
            {'O', new uint[]{0x3E, 0x41, 0x41, 0x41, 0x3E}},
            {'P', new uint[]{0x7F, 0x09, 0x09, 0x09, 0x06}},
            {'Q', new uint[]{0x3E, 0x41, 0x51, 0x21, 0x5E}},
            {'R', new uint[]{0x7F, 0x09, 0x19, 0x29, 0x46}},
            {'S', new uint[]{0x46, 0x49, 0x49, 0x49, 0x31}},
            {'T', new uint[]{0x01, 0x01, 0x7F, 0x01, 0x01}},
            {'U', new uint[]{0x3F, 0x40, 0x40, 0x40, 0x3F}},
            {'V', new uint[]{0x1F, 0x20, 0x40, 0x20, 0x1F}},
            {'W', new uint[]{0x3F, 0x40, 0x38, 0x40, 0x3F}},
            {'X', new uint[]{0x63, 0x14, 0x08, 0x14, 0x63}},
            {'Y', new uint[]{0x07, 0x08, 0x70, 0x08, 0x07}},
            {'Z', new uint[]{0x61, 0x51, 0x49, 0x45, 0x43}},
            {'0', new uint[]{0x3E, 0x51, 0x49, 0x45, 0x3E}},
            {'1', new uint[]{0x00, 0x42, 0x7F, 0x40, 0x00}},
            {'2', new uint[]{0x42, 0x61, 0x51, 0x49, 0x46}},
            {'3', new uint[]{0x21, 0x41, 0x45, 0x4B, 0x31}},
            {'4', new uint[]{0x18, 0x14, 0x12, 0x7F, 0x10}},
            {'5', new uint[]{0x27, 0x45, 0x45, 0x45, 0x39}},
            {'6', new uint[]{0x3C, 0x4A, 0x49, 0x49, 0x30}},
            {'7', new uint[]{0x01, 0x71, 0x09, 0x05, 0x03}},
            {'8', new uint[]{0x36, 0x49, 0x49, 0x49, 0x36}},
            {'9', new uint[]{0x06, 0x49, 0x49, 0x29, 0x1E}},
            {':', new uint[]{0x00, 0x36, 0x36, 0x00, 0x00}},
            {'.', new uint[]{0x00, 0x60, 0x60, 0x00, 0x00}},
            {',', new uint[]{0x00, 0x80, 0x70, 0x00, 0x00}},
            {'!', new uint[]{0x00, 0x00, 0x5F, 0x00, 0x00}},
            {'?', new uint[]{0x02, 0x01, 0x51, 0x09, 0x06}},
            {'[', new uint[]{0x00, 0x7F, 0x41, 0x41, 0x00}},
            {']', new uint[]{0x00, 0x41, 0x41, 0x7F, 0x00}},
            {'(', new uint[]{0x00, 0x1C, 0x22, 0x41, 0x00}},
            {')', new uint[]{0x00, 0x41, 0x22, 0x1C, 0x00}},
            {'/', new uint[]{0x20, 0x10, 0x08, 0x04, 0x02}},
            {'-', new uint[]{0x08, 0x08, 0x08, 0x08, 0x08}},
            {'*', new uint[]{0x14, 0x08, 0x3E, 0x08, 0x14}},
            {' ', new uint[]{0x00, 0x00, 0x00, 0x00, 0x00}},
        };

        public void LoadContent(GraphicsDevice graphicsDevice)
        {
            _spriteBatch = new SpriteBatch(graphicsDevice);
            _pixel = new Texture2D(graphicsDevice, 1, 1);
            _pixel.SetData(new[] { Color.White });

            GlyphAtlasData atlasData = BuildAtlasData();
            _glyphAtlas = new Texture2D(graphicsDevice, atlasData.Width, atlasData.Height);
            _glyphAtlas.SetData(atlasData.Pixels);
            _glyphRectangles = atlasData.SourceRectangles;
        }

        internal static GlyphAtlasData BuildAtlasData()
        {
            const int glyphWidth = 5;
            const int glyphHeight = 7;
            int totalGlyphs = FontData.Count;
            int atlasWidth = totalGlyphs * glyphWidth;
            int atlasHeight = glyphHeight;
            var pixels = new Color[atlasWidth * atlasHeight];
            var rects = new Dictionary<char, Rectangle>(totalGlyphs);

            int index = 0;
            foreach (var kvp in FontData)
            {
                char c = kvp.Key;
                uint[] glyph = kvp.Value;
                int startX = index * glyphWidth;
                rects[c] = new Rectangle(startX, 0, glyphWidth, glyphHeight);

                for (int col = 0; col < glyphWidth; col++)
                {
                    uint colMask = glyph[col];
                    for (int row = 0; row < glyphHeight; row++)
                    {
                        bool on = (colMask & (1u << row)) != 0;
                        int pixelX = startX + col;
                        int pixelY = row;
                        pixels[pixelY * atlasWidth + pixelX] = on ? Color.White : Color.Transparent;
                    }
                }
                index++;
            }

            return new GlyphAtlasData(atlasWidth, atlasHeight, pixels, rects);
        }

        public void AddMessage(string message)
        {
            Console.WriteLine("[DEBUG] " + message);
            _messages.Add(message.ToUpper()); // La mini-font es solo mayúsculas
            if (_messages.Count > MaxMessages) _messages.RemoveAt(0);
        }

        public void Draw(GameTime gameTime)
        {
            if (!_isVisible || _messages.Count == 0) return;

            _spriteBatch.Begin();
            
            // Fondo
            _spriteBatch.Draw(_pixel, new Rectangle(Padding, Padding, 400, _messages.Count * LineHeight + Padding), new Color(0, 0, 0, 180));

            for (int i = 0; i < _messages.Count; i++)
            {
                DrawString(_messages[i], new Vector2(Padding + 5, Padding + 5 + (i * LineHeight)), Color.LimeGreen);
            }

            _spriteBatch.End();
        }

        public void DrawDeveloperPanel(IReadOnlyList<string> lines)
        {
            if (lines == null || lines.Count == 0) return;

            const int panelWidth = 440;
            const int lineHeight = 12;
            int visibleLines = Math.Min(lines.Count, 20);
            int x = GameConstants.ScreenWidth - panelWidth - Padding;
            int height = visibleLines * lineHeight + Padding * 2;

            _spriteBatch.Begin();
            _spriteBatch.Draw(_pixel, new Rectangle(x, Padding, panelWidth, height), new Color(0, 0, 0, 220));
            for (int i = 0; i < visibleLines; i++)
            {
                Color color = i == 0 ? Color.Cyan : Color.White;
                DrawString(lines[i], new Vector2(x + Padding, Padding + 5 + i * lineHeight), color);
            }
            _spriteBatch.End();
        }

        private void DrawString(string text, Vector2 pos, Color color)
        {
            if (_glyphAtlas == null || _glyphRectangles == null) return;

            foreach (char c in text)
            {
                if (_glyphRectangles.TryGetValue(c, out Rectangle srcRect))
                {
                    _spriteBatch.Draw(_glyphAtlas, new Rectangle((int)pos.X, (int)pos.Y, 5, 7), srcRect, color);
                }
                pos.X += CharWidth;
            }
        }
    }
}
