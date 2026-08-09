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
        private BulletRenderer _bulletRenderer;
        private ParticleRenderer _particleRenderer;

        private readonly Player[] _players = new Player[GameConstants.MaxPlayers];
        private BulletManager _bulletManager;
        private ParticleSystem _particleSystem;

        // Timers por jugador. Arrays fijos (MaxPlayers): cero allocations en Update().
        private readonly float[] _shootCooldown = new float[GameConstants.MaxPlayers];
        private readonly float[] _thrusterTimer = new float[GameConstants.MaxPlayers];

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
            _bulletManager = new BulletManager();
            _particleSystem = new ParticleSystem(GameConstants.MaxParticles);

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
            _bulletRenderer = new BulletRenderer(GraphicsDevice);
            _particleRenderer = new ParticleRenderer(GraphicsDevice);
        }

        protected override void Update(GameTime gameTime)
        {
            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

            _inputManager.Update();

            for (int i = 0; i < GameConstants.MaxPlayers; i++)
            {
                PlayerInputState input = _inputManager.GetState(i);
                Player player = _players[i];

                player.Update(in input, deltaTime);

                if (!input.IsConnected)
                {
                    continue;
                }

                UpdateShooting(player, in input, i, deltaTime);
                UpdateThruster(player, in input, i, deltaTime);
            }

            _bulletManager.Update(deltaTime);
            _particleSystem.Update(deltaTime);

            UpdateDebugTitle(gameTime);

            base.Update(gameTime);
        }

        /// <summary>
        /// Cooldown de disparo por jugador. Al disparar: spawnea una bala
        /// pooled y un pequeño flash de partículas en la boca del cañón.
        /// </summary>
        private void UpdateShooting(Player player, in PlayerInputState input, int playerIndex, float deltaTime)
        {
            _shootCooldown[playerIndex] -= deltaTime;

            if (!input.IsShooting || _shootCooldown[playerIndex] > 0f)
            {
                return;
            }

            _shootCooldown[playerIndex] = GameConstants.ShootCooldownSeconds;

            Vector2 facing = new Vector2(
                (float)Math.Cos(player.FacingAngle),
                (float)Math.Sin(player.FacingAngle));

            Vector2 muzzlePosition = player.Position + facing * GameConstants.PlayerRadius;

            _bulletManager.Spawn(muzzlePosition, player.FacingAngle, playerIndex, player.Color);

            _particleSystem.Emit(
                muzzlePosition,
                facing * 120f,
                GameConstants.MuzzleParticleLifeSeconds,
                GameConstants.MuzzleParticleSize,
                player.Color);
        }

        /// <summary>
        /// Emite partículas de estela detrás de la nave mientras se mueve,
        /// a intervalos fijos (no todos los frames, para no saturar el pool).
        /// </summary>
        private void UpdateThruster(Player player, in PlayerInputState input, int playerIndex, float deltaTime)
        {
            if (input.MoveDirection == Vector2.Zero)
            {
                return;
            }

            _thrusterTimer[playerIndex] -= deltaTime;
            if (_thrusterTimer[playerIndex] > 0f)
            {
                return;
            }

            _thrusterTimer[playerIndex] = GameConstants.ThrusterEmitIntervalSeconds;

            Vector2 backwards = -input.MoveDirection;
            Vector2 spawnPosition = player.Position + backwards * GameConstants.PlayerRadius * 0.8f;
            Vector2 velocity = backwards * 80f;

            _particleSystem.Emit(
                spawnPosition,
                velocity,
                GameConstants.ThrusterParticleLifeSeconds,
                GameConstants.ThrusterParticleSize,
                player.Color);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(new Color(8, 8, 16)); // fondo oscuro, estilo neón

            // Orden de dibujo: partículas atrás, naves en medio, balas encima.
            _particleRenderer.Draw(GraphicsDevice, _particleSystem.Particles);
            _shipRenderer.Draw(GraphicsDevice, _players);
            _bulletRenderer.Draw(GraphicsDevice, _bulletManager.Bullets);

            base.Draw(gameTime);
        }

        /// <summary>
        /// HUD de depuración mínimo: FPS, mandos conectados, y conteo de
        /// balas/partículas activas (útil para verificar que el pooling
        /// no está creciendo sin límite). Evita depender de SpriteFont.
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

                int activeBullets = CountActiveBullets(_bulletManager.Bullets);
                int activeParticles = CountActiveParticles(_particleSystem.Particles);

                float fps = _frameCount / _fpsTimer;
                Window.Title =
                    $"Twin-Stick Shooter | FPS: {fps:0} | Mandos: {connected}/{GameConstants.MaxPlayers} " +
                    $"| Balas: {activeBullets}/{GameConstants.MaxBullets} " +
                    $"| Partículas: {activeParticles}/{GameConstants.MaxParticles}";

                _fpsTimer = 0f;
                _frameCount = 0;
            }
        }

        private static int CountActiveBullets(Bullet[] bullets)
        {
            int count = 0;
            for (int i = 0; i < bullets.Length; i++)
            {
                if (bullets[i].Active) count++;
            }
            return count;
        }

        private static int CountActiveParticles(ParticleSystem.Particle[] particles)
        {
            int count = 0;
            for (int i = 0; i < particles.Length; i++)
            {
                if (particles[i].Active) count++;
            }
            return count;
        }
    }
}
