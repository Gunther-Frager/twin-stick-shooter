using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using TwinStickShooter.Core;
using TwinStickShooter.Entities;

public class Camera
{
    private Vector2 _position;
    private float _zoom;
    private Vector2 _targetPosition;
    private float _targetZoom;

    public Camera()
    {
        _position = Vector2.Zero;
        _zoom = 1.0f;
        _targetPosition = Vector2.Zero;
        _targetZoom = 1.0f;
    }

    public void Update(List<Player> players)
    {
        if (players.Count == 0)
        {
            return;
        }

        _targetPosition = GetCenterOfMass(players);
        _targetZoom = GetZoom(players);

        // Interpolación suave para posición y zoom
        _position = Vector2.Lerp(_position, _targetPosition, GameConstants.CameraLerpFactor);
        _zoom = MathHelper.Lerp(_zoom, _targetZoom, GameConstants.CameraLerpFactor);
    }

    private Vector2 GetCenterOfMass(List<Player> players)
    {
        Vector2 sum = Vector2.Zero;
        foreach (var player in players)
        {
            sum += player.Position;
        }
        return sum / players.Count;
    }

    private float GetZoom(List<Player> players)
    {
        if (players.Count == 1)
        {
            return GameConstants.CameraZoomMax;
        }

        // Encuentra los extremos de los jugadores
        float minX = float.MaxValue, maxX = float.MinValue;
        float minY = float.MaxValue, maxY = float.MinValue;

        foreach (var player in players)
        {
            minX = Math.Min(minX, player.Position.X);
            maxX = Math.Max(maxX, player.Position.X);
            minY = Math.Min(minY, player.Position.Y);
            maxY = Math.Max(maxY, player.Position.Y);
        }

        // Calcula el tamaño necesario para que todos los jugadores entren en pantalla
        float requiredWidth = (maxX - minX) + (GameConstants.CameraPadding * 2);
        float requiredHeight = (maxY - minY) + (GameConstants.CameraPadding * 2);

        // Calcula el zoom necesario para que el área requerida entre en la pantalla
        float zoomX = GameConstants.ScreenWidth / requiredWidth;
        float zoomY = GameConstants.ScreenHeight / requiredHeight;

        return MathHelper.Clamp(Math.Min(zoomX, zoomY), GameConstants.CameraZoomMin, GameConstants.CameraZoomMax);
    }

    public Matrix GetViewMatrix()
    {
        // Centra la cámara en la pantalla
        Vector3 translation = new Vector3(-_position.X, -_position.Y, 0);
        Vector3 scale = new Vector3(_zoom, _zoom, 1);
        Vector3 offset = new Vector3(GameConstants.ScreenWidth / 2f, GameConstants.ScreenHeight / 2f, 0);

        return Matrix.CreateTranslation(translation) * Matrix.CreateScale(scale) * Matrix.CreateTranslation(offset);
    }

    public float CurrentZoom => _zoom;