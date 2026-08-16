using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
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
        private ArenaRenderer _arenaRenderer;
        private Camera _camera;
        private DebugConsole _debugConsole;

        private readonly Player[] _players = new Player[GameConstants.MaxPlayers];
        private BulletManager _bulletManager;
        private ParticleSystem _particleSystem;
        private LevelManager _levelManager;

        // Estado del juego
        private GameState _currentGameState = GameState.SinglePlayer;

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
            _camera = new Camera();
            _levelManager = new LevelManager(GameConstants.GridWidth, GameConstants.GridHeight, GameConstants.GridCellSize);
            _debugConsole = new DebugConsole();

            // Cargar el mapa de prueba ANTES de crear el renderer
            try
            {
                MapLoader.LoadMap(_levelManager, Content, "Maps/test_map");
                // Nota: _arenaRenderer aún no se ha creado aquí (se crea en LoadContent)
            }
            catch (System.Exception e)
            {
                Console.WriteLine("Error: " + e.Message);
            }

            // Generar un mapa procedural si se desea
            // MapLoader.GenerateProceduralMap(_levelManager);

            // Spawns iniciales en el marcador de spawn del mapa
            Vector2 spawnPosition = _levelManager.GetSpawnPosition();
            Vector2[] offsets =
            {
                new Vector2(-18, -18), new Vector2(18, -18),
                new Vector2(-18, 18),  new Vector2(18, 18),
            };

            for (int i = 0; i < GameConstants.MaxPlayers; i++)
            {
                _players[i] = new Player(i, spawnPosition + offsets[i]);
                _players[i].IsActive = (i == 0); // Solo el jugador 0 está activo por defecto
            }

            // Contar paredes para depuración
            int wallCount = CountWalls();
            Console.WriteLine($"[Game1] Paredes en el mapa: {wallCount}");

            base.Initialize();
        }

        protected override void LoadContent()
        {
            _shipRenderer = new ShipRenderer(GraphicsDevice);
            _bulletRenderer = new BulletRenderer(GraphicsDevice);
            _particleRenderer = new ParticleRenderer(GraphicsDevice);
            _arenaRenderer = new ArenaRenderer(GraphicsDevice, _levelManager);
            _arenaRenderer.RebuildGeometry(); // Reconstruir geometría con el mapa cargado
            _debugConsole.LoadContent(GraphicsDevice);
        }

        protected override void Update(GameTime gameTime)
        {
            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

            _inputManager.Update();

            // Manejo de teclas para cambiar entre modos de juego
            var keyboardState = Keyboard.GetState();
            if (keyboardState.IsKeyDown(Keys.F1))
            {
                SetGameMode(GameState.SinglePlayer);
            }
            else if (keyboardState.IsKeyDown(Keys.F2))
            {
                SetGameMode(GameState.Multiplayer);
            }

            // Actualiza la cámara con los jugadores activos
            var activePlayers = new List<Player>();
            for (int i = 0; i < GameConstants.MaxPlayers; i++)
            {
                if (_inputManager.GetState(i).IsConnected)
                {
                    activePlayers.Add(_players[i]);
                    // Verifica si el jugador ha alcanzado el marcador de salida
                    if (_levelManager.CheckExitReached(_players[i].Position))
                    {
                        SetDebugMessage("¡Nivel completado!");
                        // Aquí puedes agregar lógica adicional para finalizar el nivel
                    }
                }
            }
            _camera.Update(activePlayers.ToArray());

            for (int i = 0; i < GameConstants.MaxPlayers; i++)
            {
                PlayerInputState input = _inputManager.GetState(i);
                Player player = _players[i];

                if (!player.IsActive)
                {
                    continue;
                }

                player.Update(in input, deltaTime, _levelManager);

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

            Matrix viewMatrix = _camera.ViewMatrix;

            // Orden de dibujo: arena atrás, partículas, naves en medio, balas encima.
            _arenaRenderer.Draw(viewMatrix);
            _particleRenderer.Draw(GraphicsDevice, _particleSystem.Particles, viewMatrix);
            _shipRenderer.Draw(GraphicsDevice, _players, viewMatrix);
            _bulletRenderer.Draw(GraphicsDevice, _bulletManager.Bullets, viewMatrix);

            // Dibujar la consola de depuración
            _debugConsole.Draw(gameTime);

            base.Draw(gameTime);
        }

        /// <summary>
        /// HUD de depuración mínimo: FPS, mandos conectados, y conteo de
        /// balas/partículas activas (útil para verificar que el pooling
        /// no está creciendo sin límite). Evita depender de SpriteFont.
        /// </summary>
        private string _debugMessage = "";

        /// <summary>
        /// Muestra un mensaje en la consola de depuración.
        /// </summary>
        public void SetDebugMessage(string message)
        {
            _debugConsole.AddMessage(message);
        }

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
                    $"Twin-Stick Shooter | FPS: {fps:0} | Modo: {_currentGameState} | Mandos: {connected}/{GameConstants.MaxPlayers} " +
                    $"| Balas: {activeBullets}/{GameConstants.MaxBullets} " +
                    $"| Partículas: {activeParticles}/{GameConstants.MaxParticles} " +
                    $"| Zoom: {_camera.ViewMatrix.M11:0.00} | {_debugMessage}";

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

        /// <summary>
        /// Cuenta cuántas celdas del LevelManager están marcadas como paredes.
        /// </summary>
        private int CountWalls()
        {
            int wallCount = 0;
            for (int x = 0; x < GameConstants.GridWidth; x++)
            {
                for (int y = 0; y < GameConstants.GridHeight; y++)
                {
                    Vector2 testPosition = new Vector2(x * GameConstants.GridCellSize + 1, y * GameConstants.GridCellSize + 1);
                    if (_levelManager.CheckCollision(testPosition, 1f))
                    {
                        wallCount++;
                    }
                }
            }
            return wallCount;
        }

        /// <summary>
        /// Genera un mapa procedural y actualiza la geometría del renderizador.
        /// </summary>
        public void GenerateProceduralMap()
        {
            MapLoader.GenerateProceduralMap(_levelManager);
            _arenaRenderer.RebuildGeometry();
            int wallCount = CountWalls();
            Console.WriteLine($"[Game1] Paredes en el mapa generado: {wallCount}");
        }

        /// <summary>
        /// Cambia el modo de juego y actualiza el estado de los jugadores.
        /// </summary>
        public void SetGameMode(GameState mode)
        {
            _currentGameState = mode;
            
            // Actualizar el estado de los jugadores según el modo
            for (int i = 0; i < GameConstants.MaxPlayers; i++)
            {
                if (mode == GameState.SinglePlayer)
                {
                    // Solo el jugador 0 está activo en modo un jugador
                    _players[i].IsActive = (i == 0);
                }
                else if (mode == GameState.Multiplayer)
                {
                    // Todos los jugadores están activos en modo multijugador
                    _players[i].IsActive = true;
                }
            }
        }
    }
}
