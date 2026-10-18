using Microsoft.Xna.Framework;
using TwinStickShooter.Core;
using TwinStickShooter.Input;
using Xunit;

namespace TwinStickShooter.Tests;

public class InputDeadzoneTests
{
    [Fact]
    public void ApplyRadialDeadzone_WhenMagnitudeLessThanOrEqualToDeadzone_ReturnsZero()
    {
        float deadzone = 0.25f;

        Vector2 small = new Vector2(0.1f, 0.1f); // len = ~0.1414 <= 0.25
        Vector2 exact = new Vector2(0.25f, 0f); // len = 0.25

        Assert.Equal(Vector2.Zero, InputManager.ApplyRadialDeadzone(small, deadzone));
        Assert.Equal(Vector2.Zero, InputManager.ApplyRadialDeadzone(exact, deadzone));
    }

    [Fact]
    public void ApplyRadialDeadzone_WhenMagnitudeIsOne_ReturnsUnitLength()
    {
        float deadzone = 0.2f;
        Vector2 full = Vector2.Normalize(new Vector2(0.6f, 0.8f)); // len = 1

        Vector2 result = InputManager.ApplyRadialDeadzone(full, deadzone);

        Assert.Equal(1f, result.Length(), precision: 5);
    }

    [Fact]
    public void ApplyRadialDeadzone_WhenMagnitudeIsIntermediate_RemapsCorrectly()
    {
        float deadzone = 0.2f;
        float rawLength = 0.6f;
        Vector2 stick = new Vector2(rawLength, 0f);

        Vector2 result = InputManager.ApplyRadialDeadzone(stick, deadzone);

        float expectedLength = (rawLength - deadzone) / (1f - deadzone);
        Assert.Equal(expectedLength, result.Length(), precision: 5);
    }

    [Fact]
    public void ApplyRadialDeadzone_PreservesDirection()
    {
        float deadzone = 0.15f;
        Vector2 stick = new Vector2(0.3f, -0.4f); // 3-4-5 triangle, len = 0.5
        Vector2 expectedDirection = Vector2.Normalize(stick);

        Vector2 result = InputManager.ApplyRadialDeadzone(stick, deadzone);
        Vector2 resultDirection = Vector2.Normalize(result);

        Assert.Equal(expectedDirection.X, resultDirection.X, precision: 5);
        Assert.Equal(expectedDirection.Y, resultDirection.Y, precision: 5);
    }

    [Theory]
    [InlineData(GameConstants.LeftStickDeadzone)]
    [InlineData(GameConstants.RightStickDeadzone)]
    public void ApplyRadialDeadzone_WithConfiguredDeadzoneConstants_RemapsExpectedly(float deadzone)
    {
        Vector2 inside = new Vector2(deadzone * 0.5f, 0f);
        Assert.Equal(Vector2.Zero, InputManager.ApplyRadialDeadzone(inside, deadzone));

        Vector2 full = new Vector2(0f, 1f);
        Vector2 fullResult = InputManager.ApplyRadialDeadzone(full, deadzone);
        Assert.Equal(1f, fullResult.Length(), precision: 5);

        float midLen = (deadzone + 1f) / 2f;
        Vector2 mid = new Vector2(midLen, 0f);
        Vector2 midResult = InputManager.ApplyRadialDeadzone(mid, deadzone);
        Assert.Equal(0.5f, midResult.Length(), precision: 5);
    }
}

