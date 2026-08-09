using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TwinStickShooter.Core;
using TwinStickShooter.Entities;
using TwinStickShooter.Input;
using TwinStickShooter.Rendering;

namespace TwinStickShooter
{
    public class Game1 : Game
    {
        private readonly GraphicsDeviceManager _graphics;

        private InputManager _inputManager;
        private ShipRenderer _shipRenderer;
        private readonly Player[] _players = new Player[GameConstants.MaxPlayers];

        // HUD de debug: acumuladores para no recalcular FPS cada frame (evita
        // formatear strings 60 veces por segundo).
        private float _fpsTimer;
        private int _frameCount;

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this)
            {
                PreferredBackBufferWidth = GameConstants.ScreenWidth,
                PreferredBackBufferHeight = GameConstants.ScreenHeight,
                SynchronizeWithVerticalRetrace = true
            };

            Content.RootDirectory = "Content";
            IsMouseVisible = true;

            // --- Pilar: Game Loop a 60 FPS fijos ---
            IsFixedTimeStep = true;
            TargetElapsedTime = TimeSpan.FromSeconds(1.0 / GameConstants.TargetFps);
        }

        protected override void Initialize()
        {
            _inputManager = new InputManager();

            // Spawns iniciales repartidos en el centro de la pantalla (placeholder;
            // en Fase 4 esto vendrá del LevelManager / mapa).
            Vector2 center = new Vector2(GameConstants.ScreenWidth / 2f, GameConstants.ScreenHeight / 2f);
            Vector2[] offsets =
            {
                new Vector2(-60, -40), new Vector2(60, -40),
                new Vector2(-60, 40),  new Vector2(60, 40),
            };

            for (int i = 0; i < GameConstants.MaxPlayers; i++)
            {
                _players[i] = new Player(i, center + offsets[i]);
            }

            base.Initialize();
        }

        protected override void LoadContent()
        {
            _shipRenderer = new ShipRenderer(GraphicsDevice);
        }

        protected override void Update(GameTime gameTime)
        {
            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

            _inputManager.Update();

            for (int i = 0; i < GameConstants.MaxPlayers; i++)
            {
                PlayerInputState input = _inputManager.GetState(i);
                _players[i].Update(in input, deltaTime);
            }

            UpdateDebugTitle(gameTime);

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(new Color(8, 8, 16)); // fondo oscuro, estilo neón

            _shipRenderer.Draw(GraphicsDevice, _players);

            base.Draw(gameTime);
        }

        /// <summary>
        /// HUD de depuración mínimo para Fase 1: FPS y mandos conectados en el
        /// título de la ventana. Evita depender de SpriteFont/Content Pipeline.
        /// </summary>
        private void UpdateDebugTitle(GameTime gameTime)
        {
            _frameCount++;
            _fpsTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (_fpsTimer >= 0.5f)
            {
                int connected = 0;
                for (int i = 0; i < GameConstants.MaxPlayers; i++)
                {
                    if (_inputManager.GetState(i).IsConnected) connected++;
                }

                float fps = _frameCount / _fpsTimer;
                Window.Title = $"Twin-Stick Shooter | FPS: {fps:0} | Mandos: {connected}/{GameConstants.MaxPlayers}";

                _fpsTimer = 0f;
                _frameCount = 0;
            }
        }
    }
}
