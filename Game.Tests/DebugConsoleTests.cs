using Microsoft.Xna.Framework;
using TwinStickShooter.Core;
using Xunit;

namespace TwinStickShooter.Tests;

public class DebugConsoleTests
{
    [Fact]
    public void BuildAtlasData_GlyphA_MatchesFontDataColumnsExactly()
    {
        DebugConsole.GlyphAtlasData atlasData = DebugConsole.BuildAtlasData();
        Assert.True(atlasData.SourceRectangles.TryGetValue('A', out Rectangle rectA));
        Assert.Equal(5, rectA.Width);
        Assert.Equal(7, rectA.Height);

        uint[] glyphA = DebugConsole.FontData['A'];
        for (int col = 0; col < 5; col++)
        {
            uint mask = glyphA[col];
            for (int row = 0; row < 7; row++)
            {
                bool expectedLit = (mask & (1u << row)) != 0;
                int pixelIndex = row * atlasData.Width + (rectA.X + col);
                Color pixelColor = atlasData.Pixels[pixelIndex];

                Color expectedColor = expectedLit ? Color.White : Color.Transparent;
                Assert.Equal(expectedColor, pixelColor);
            }
        }
    }

    [Fact]
    public void BuildAtlasData_AllGlyphs_FitIn5x7AndDoNotOverlap()
    {
        DebugConsole.GlyphAtlasData atlasData = DebugConsole.BuildAtlasData();

        Assert.Equal(DebugConsole.FontData.Count, atlasData.SourceRectangles.Count);
        Assert.Equal(DebugConsole.FontData.Count * 5, atlasData.Width);
        Assert.Equal(7, atlasData.Height);

        foreach (var kvp in DebugConsole.FontData)
        {
            char c = kvp.Key;
            Assert.True(atlasData.SourceRectangles.TryGetValue(c, out Rectangle rect));
            Assert.Equal(5, rect.Width);
            Assert.Equal(7, rect.Height);
            Assert.Equal(0, rect.Y);
            Assert.True(rect.X >= 0);
            Assert.True(rect.Right <= atlasData.Width);
        }
    }

    [Fact]
    public void BuildAtlasData_Space_HasNoLitPixels()
    {
        DebugConsole.GlyphAtlasData atlasData = DebugConsole.BuildAtlasData();
        Assert.True(atlasData.SourceRectangles.TryGetValue(' ', out Rectangle rectSpace));

        for (int col = 0; col < 5; col++)
        {
            for (int row = 0; row < 7; row++)
            {
                int pixelIndex = row * atlasData.Width + (rectSpace.X + col);
                Assert.Equal(Color.Transparent, atlasData.Pixels[pixelIndex]);
            }
        }
    }
}
