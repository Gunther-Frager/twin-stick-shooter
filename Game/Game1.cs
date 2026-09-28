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
    /// <summary>Coordina la inicialización, simulación y presentación de una partida.</summary>
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
        private EnemyBulletManager _enemyBulletManager;
        private ParticleSystem _particleSystem;
        private LevelManager _levelManager;
        private EnemyManager _enemyManager;
        private SpawnerManager _spawnerManager;
        private EnemyRenderer _enemyRenderer;
        private SpawnerRenderer _spawnerRenderer;
        private List<RoomTemplateData> _roomTemplates;

        // Estado del juego
        private GameState _currentGameState = GameState.SinglePlayer;

        // Timers por jugador. Arrays fijos (MaxPlayers): cero allocations en Update().
        private readonly float[] _shootCooldown = new float[GameConstants.MaxPlayers];
        private readonly float[] _thrusterTimer = new float[GameConstants.MaxPlayers];

        // HUD de debug: acumuladores para no recalcular FPS cada frame (evita
        // formatear strings 60 veces por segundo).
        private float _fpsTimer;
        private int _frameCount;
        private bool _previousF3Down;
        private bool _previousF4Down;
        private bool _previousF5Down;
        private bool _previousF6Down;
        private bool _previousUpDown;
        private bool _previousDownDown;
        private bool _previousLeftDown;
        private bool _previousRightDown;
        private bool _developerPanelOpen;
        private int _developerPanelSelection;
        private bool _combatTestSceneActive;
        private bool _levelCompleted;
        private bool _exitWaitingMessageShown;

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
            InitializeManagers();
            InitializeLevel();

            // Contar paredes para depuración
            int wallCount = CountWalls();
            SetDebugMessage($"Mapa cargado: {wallCount} colisiones");

            base.Initialize();
        }

        protected override void LoadContent()
        {
            _shipRenderer = new ShipRenderer(GraphicsDevice);
            _bulletRenderer = new BulletRenderer(GraphicsDevice);
            _particleRenderer = new ParticleRenderer(GraphicsDevice);
            _arenaRenderer = new ArenaRenderer(GraphicsDevice, _levelManager);
            _arenaRenderer.RebuildGeometry(); // Reconstruir geometría con el mapa cargado
            _enemyRenderer = new EnemyRenderer(GraphicsDevice);
            _spawnerRenderer = new SpawnerRenderer(GraphicsDevice);
            _debugConsole.LoadContent(GraphicsDevice);
        }

        /// <summary>Actualiza input, jugadores, combate, efectos y HUD en cada tick.</summary>
        protected override void Update(GameTime gameTime)
        {
            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

            _inputManager.Update();

            // Manejo de teclas para cambiar entre modos de juego
            var keyboardState = Keyboard.GetState();
            bool f3Down = keyboardState.IsKeyDown(Keys.F3);
            if (GameConstants.EnableDebugHotkeys && f3Down && !_previousF3Down)
            {
                if (_combatTestSceneActive)
                {
                    LoadNormalScene();
                }
                else
                {
                    LoadCombatTestScene();
                }
            }
            _previousF3Down = f3Down;

            bool f4Down = keyboardState.IsKeyDown(Keys.F4);
            if (f4Down && !_previousF4Down) _developerPanelOpen = !_developerPanelOpen;
            _previousF4Down = f4Down;

            bool f5Down = keyboardState.IsKeyDown(Keys.F5);
            if (_developerPanelOpen && f5Down && !_previousF5Down) LoadNormalScene();
            _previousF5Down = f5Down;

            bool f6Down = keyboardState.IsKeyDown(Keys.F6);
            if (_developerPanelOpen && f6Down && !_previousF6Down)
            {
                _levelManager.MapGenerator.Settings.Seed = new Random().Next(1, int.MaxValue);
                LoadNormalScene();
            }
            _previousF6Down = f6Down;

            if (_developerPanelOpen)
            {
                UpdateDeveloperPanelInput(keyboardState);
                base.Update(gameTime);
                return;
            }

            if (keyboardState.IsKeyDown(Keys.F1))
            {
                SetGameMode(GameState.SinglePlayer);
            }
            else if (keyboardState.IsKeyDown(Keys.F2))
            {
                SetGameMode(GameState.Multiplayer);
            }

            // Actualiza la cámara con los jugadores activos
            for (int i = 0; i < GameConstants.MaxPlayers; i++)
            {
                if (_players[i].IsActive && _inputManager.GetState(i).IsConnected)
                {
                    // Verifica si el jugador ha alcanzado el marcador de salida
                    if (!_levelCompleted && _levelManager.CheckExitReached(_players[i].Position))
                    {
                        if (_enemyManager.ActiveCount == 0 && _spawnerManager.ActiveCount == 0)
                        {
                            _levelCompleted = true;
                            SetDebugMessage("Nivel completado");
                        }
                        else if (!_exitWaitingMessageShown)
                        {
                            _exitWaitingMessageShown = true;
                            SetDebugMessage("Elimina enemigos y generadores para abrir la salida");
                        }
                    }
                }
            }
            _camera.Update(_players, _inputManager);

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

            _bulletManager.Update(deltaTime, _enemyManager, _spawnerManager);
            _spawnerManager.Update(deltaTime);
            _enemyManager.Update(deltaTime, _players, _enemyBulletManager);
            _enemyBulletManager.Update(deltaTime, _players);
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
            GraphicsDevice.Clear(Color.Black); // fondo negro

            Matrix viewMatrix = _camera.ViewMatrix;

            // Orden de dibujo: arena atrás, partículas, enemigos, naves en medio, balas encima.
            _arenaRenderer.Draw(viewMatrix);
            _particleRenderer.Draw(GraphicsDevice, _particleSystem.Particles, viewMatrix);
            _enemyRenderer.Draw(GraphicsDevice, _enemyManager.Enemies, viewMatrix);
            _spawnerRenderer.Draw(GraphicsDevice, _spawnerManager.Spawners, viewMatrix);
            _shipRenderer.Draw(GraphicsDevice, _players, viewMatrix);
            _bulletRenderer.Draw(GraphicsDevice, _bulletManager.Bullets, viewMatrix);
            _bulletRenderer.Draw(GraphicsDevice, _enemyBulletManager.Bullets, viewMatrix);

            // Dibujar la consola de depuración
            _debugConsole.Draw(gameTime);
            if (_developerPanelOpen)
            {
                _debugConsole.DrawDeveloperPanel(BuildDeveloperPanelLines());
            }

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
        /// Busca una posición válida cerca de la posición original para spawnear entidades.
        /// </summary>
        private bool TryFindValidSpawnPosition(Vector2 originalPosition, float radius, out Vector2 validPosition)
        {
            if (_levelManager.IsPlayableAndWalkable(originalPosition, radius))
            {
                validPosition = originalPosition;
                return true;
            }

            Point origin = _levelManager.WorldToGrid(originalPosition);
            int maxRadius = Math.Max(GameConstants.GridWidth, GameConstants.GridHeight);
            for (int searchRadius = 1; searchRadius <= maxRadius; searchRadius++)
            {
                for (int x = origin.X - searchRadius; x <= origin.X + searchRadius; x++)
                {
                    for (int y = origin.Y - searchRadius; y <= origin.Y + searchRadius; y++)
                    {
                        if (x < 0 || x >= GameConstants.GridWidth || y < 0 || y >= GameConstants.GridHeight)
                        {
                            continue;
                        }

                        Vector2 testPosition = _levelManager.GridToWorld(new Point(x, y));
                        if (_levelManager.IsPlayableAndWalkable(testPosition, radius))
                        {
                            validPosition = testPosition;
                            return true;
                        }
                    }
                }
            }

            validPosition = Vector2.Zero;
            return false;
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
            LoadNormalScene();
        }

        /// <summary>
        /// Inicializa los managers del juego (input, level, bullet, particle, camera, debug).
        /// </summary>
        private void InitializeManagers()
        {
            _inputManager = new InputManager();
            _levelManager = new LevelManager(GameConstants.GridWidth, GameConstants.GridHeight, GameConstants.GridCellSize);
            _levelManager.UseRoundedContours = true;
            _bulletManager = new BulletManager(_levelManager);
            _enemyBulletManager = new EnemyBulletManager(_levelManager);
            _particleSystem = new ParticleSystem(GameConstants.MaxParticles, _levelManager);
            _enemyManager = new EnemyManager(_levelManager);
            _spawnerManager = new SpawnerManager(GameConstants.MaxSpawners, _enemyManager);
            _camera = new Camera();
            _debugConsole = new DebugConsole();

            // Cargar plantillas de sala para uso futuro
            _roomTemplates = RoomTemplateLoader.LoadAll(Content);
            Console.WriteLine($"[Game1] Plantillas de sala cargadas: {_roomTemplates.Count}");
            
            // Inyectar plantillas en el generador de mapas
            _levelManager.SetRoomTemplates(_roomTemplates);
        }

        /// <summary>
        /// Carga el nivel procedural y configura la geometría del mapa.
        /// </summary>
        private void InitializeLevel()
        {
            if (GameConstants.StartInCombatTestScene)
            {
                LoadCombatTestScene();
                return;
            }

            LoadNormalScene();
        }

        private void LoadCombatTestScene()
        {
            _combatTestSceneActive = true;
            _levelCompleted = false;
            _exitWaitingMessageShown = false;
            _levelManager.ConfigureCombatTestArena();
            _spawnerManager.Reset();
            _enemyManager.Clear();
            InitializePlayers();

            Vector2 center = _levelManager.GetSpawnPosition();
            SpawnCombatTestEnemy(center, new Vector2(260f, 0f), EnemyType.Swarmer);
            SpawnCombatTestEnemy(center, new Vector2(-260f, 0f), EnemyType.Roamer);
            SpawnCombatTestEnemy(center, new Vector2(0f, -260f), EnemyType.Turret);
            if (TryFindValidSpawnPosition(center + new Vector2(0f, 260f), GameConstants.SpawnerRadius, out Vector2 spawnerPosition))
            {
                _spawnerManager.Register(spawnerPosition);
            }
            _arenaRenderer?.RebuildGeometry();
            SetDebugMessage("Escena de combate: F3 reinicia");
        }

        private void SpawnCombatTestEnemy(Vector2 center, Vector2 offset, EnemyType type)
        {
            if (TryFindValidSpawnPosition(center + offset, GameConstants.EnemyRadius, out Vector2 position))
            {
                _enemyManager.Spawn(position, Vector2.Zero, type);
            }
        }

        private void LoadNormalScene()
        {
            try
            {
                _levelManager.GenerateProceduralMap();
            }
            catch (InvalidOperationException exception)
            {
                SetDebugMessage(exception.Message);
                if (!_levelManager.HasPrimitiveMapData()) throw;
                return;
            }

            _combatTestSceneActive = false;
            _levelCompleted = false;
            _exitWaitingMessageShown = false;
            _spawnerManager.Reset();
            _enemyManager.Clear();
            InitializePlayers();
            Console.WriteLine($"[Game1] Spawns de plantilla: {_levelManager.MapGenerator.RoomEnemySpawnPoints.Count}");
            SpawnEncounterPlan();
            _arenaRenderer?.RebuildGeometry();
            SetDebugMessage($"Mapa seed {_levelManager.MapGenerator.Settings.Seed}, ruta {_levelManager.MapGenerator.SpawnExitPathLength}");
        }

        private void SpawnEncounterPlan()
        {
            MapGenerator generator = _levelManager.MapGenerator;
            IList<RoomTemplateData.EnemySpawn> templateSpawns = generator.RoomEnemySpawns;
            for (int i = 0; i < templateSpawns.Count; i++)
            {
                RoomTemplateData.EnemySpawn spawn = templateSpawns[i];
                Vector2 worldPosition = new Vector2(spawn.Position.X, spawn.Position.Y);
                float radius = spawn.Type == EnemyType.Spawner
                    ? GameConstants.SpawnerRadius
                    : GameConstants.EnemyRadius;
                if (_levelManager.IsPlayableAndWalkable(worldPosition, radius))
                {
                    RegisterTemplateSpawn(worldPosition, spawn.Type);
                }
            }

            IReadOnlyList<EncounterSpawn> plan = EncounterDirector.Plan(
                _levelManager.GetCollisionGridSnapshot(),
                generator.Rooms,
                generator.SpawnPoint,
                GameConstants.GridCellSize,
                templateSpawns,
                generator.Settings);
            for (int i = 0; i < plan.Count; i++)
            {
                EncounterSpawn spawn = plan[i];
                Vector2 worldPosition = new Vector2(spawn.Position.X, spawn.Position.Y);
                if (_levelManager.IsPlayableAndWalkable(worldPosition, GameConstants.EnemyRadius))
                {
                    _enemyManager.Spawn(worldPosition, Vector2.Zero, spawn.Type);
                }
            }

            Console.WriteLine($"[Game1] Spawn total: {_enemyManager.ActiveCount}/{GameConstants.MaxEnemies}");
        }

        private void UpdateDeveloperPanelInput(KeyboardState keyboardState)
        {
            bool upDown = keyboardState.IsKeyDown(Keys.Up);
            bool downDown = keyboardState.IsKeyDown(Keys.Down);
            bool leftDown = keyboardState.IsKeyDown(Keys.Left);
            bool rightDown = keyboardState.IsKeyDown(Keys.Right);
            if (upDown && !_previousUpDown) _developerPanelSelection = (_developerPanelSelection + 12) % 13;
            if (downDown && !_previousDownDown) _developerPanelSelection = (_developerPanelSelection + 1) % 13;
            if (leftDown && !_previousLeftDown) AdjustDeveloperSetting(-1);
            if (rightDown && !_previousRightDown) AdjustDeveloperSetting(1);
            _previousUpDown = upDown;
            _previousDownDown = downDown;
            _previousLeftDown = leftDown;
            _previousRightDown = rightDown;
        }

        private void AdjustDeveloperSetting(int direction)
        {
            MapGenerationSettings settings = _levelManager.MapGenerator.Settings;
            switch (_developerPanelSelection)
            {
                case 0: settings.MinimumSpawnExitPathLength = MathHelper.Clamp(settings.MinimumSpawnExitPathLength + direction, 1, 80); break;
                case 1: settings.MaxRoomCount = Math.Max(settings.MinRoomCount + 1, MathHelper.Clamp(settings.MaxRoomCount + direction, 3, 20)); break;
                case 2: settings.CrawlerRoomProbability = MathHelper.Clamp((float)(settings.CrawlerRoomProbability + direction * 0.05), 0f, 1f); break;
                case 3: settings.CrawlerStepsPerRoomCell = MathHelper.Clamp(settings.CrawlerStepsPerRoomCell + direction, 0, 10); break;
                case 4: settings.EncounterSpawnSafeDistance = MathHelper.Clamp(settings.EncounterSpawnSafeDistance + direction, 0, 20); break;
                case 5: settings.EncounterDifficultyBudget = MathHelper.Clamp(settings.EncounterDifficultyBudget + direction, 0, 100); break;
                case 6: settings.EncounterMinGroupSize = MathHelper.Clamp(settings.EncounterMinGroupSize + direction, 1, settings.EncounterMaxGroupSize); break;
                case 7: settings.EncounterMaxGroupSize = MathHelper.Clamp(settings.EncounterMaxGroupSize + direction, settings.EncounterMinGroupSize, 12); break;
                case 8: settings.EncounterClusterRadius = MathHelper.Clamp(settings.EncounterClusterRadius + direction, 0, 12); break;
                case 9: settings.EncounterSwarmerCost = MathHelper.Clamp(settings.EncounterSwarmerCost + direction, 1, 20); break;
                case 10: settings.EncounterRoamerCost = MathHelper.Clamp(settings.EncounterRoamerCost + direction, 1, 20); break;
                case 11: settings.EncounterTurretCost = MathHelper.Clamp(settings.EncounterTurretCost + direction, 1, 20); break;
                case 12: settings.UseRoomTemplates = !settings.UseRoomTemplates; break;
            }
        }

        private List<string> BuildDeveloperPanelLines()
        {
            MapGenerator generator = _levelManager.MapGenerator;
            MapGenerationSettings settings = generator.Settings;
            string[] values =
            {
                $"RUTA MIN CELDAS: {settings.MinimumSpawnExitPathLength}",
                $"SALAS MAX: {settings.MaxRoomCount}",
                $"WALKER PROB: {settings.CrawlerRoomProbability:0.00}",
                $"WALKER PASOS: {settings.CrawlerStepsPerRoomCell}",
                $"SAFE RADIO CELDAS: {settings.EncounterSpawnSafeDistance}",
                $"PRESUPUESTO SALA: {settings.EncounterDifficultyBudget}",
                $"GRUPO MIN: {settings.EncounterMinGroupSize}",
                $"GRUPO MAX: {settings.EncounterMaxGroupSize}",
                $"RADIO CLUSTER CELDAS: {settings.EncounterClusterRadius}",
                $"COSTO SWARMER: {settings.EncounterSwarmerCost}",
                $"COSTO ROAMER: {settings.EncounterRoamerCost}",
                $"COSTO TURRET: {settings.EncounterTurretCost}",
                $"PLANTILLAS: {(settings.UseRoomTemplates ? "ON" : "OFF")}",
            };
            List<string> lines = new List<string>
            {
                "PANEL DEV F4 CERRAR F5 REGENERAR F6 NUEVA SEED",
                "ARRIBA ABAJO ELEGIR IZQ DER CAMBIAR",
                $"SEED {settings.Seed} RUTA {generator.SpawnExitPathLength} CELDAS SALAS {generator.Rooms.Count}",
            };
            for (int i = 0; i < values.Length; i++)
            {
                lines.Add($"{(i == _developerPanelSelection ? "* " : "  ")}{values[i]}");
            }
            lines.Add(GetDeveloperSettingHelp(_developerPanelSelection));

            return lines;
        }

        private static string GetDeveloperSettingHelp(int selection)
        {
            switch (selection)
            {
                case 0: return "MINIMO BFS EN CELDAS INICIO A SALIDA. SI NO CUMPLE, NO REGENERA.";
                case 1: return "LIMITE SUPERIOR DE SALAS. EL CAMBIO SE APLICA CON F5.";
                case 2: return "CHANCE DE WALKERS SOLO EN SALAS SIN PLANTILLA.";
                case 3: return "PASOS DE WALKERS POR CELDA. MAS PASOS ABREN MAS SUELO.";
                case 4: return "RADIO EN CELDAS SIN SPAWNS ALREDEDOR DEL INICIO.";
                case 5: return "PUNTOS POR SALA. NO EQUIVALE A CANTIDAD DE ENEMIGOS.";
                case 6: return "MINIMO EN CADA GRUPO SI CABEN EL RADIO Y EL PRESUPUESTO.";
                case 7: return "MAXIMO POR GRUPO. LA CANTIDAD REAL VARIA ENTRE MIN Y MAX.";
                case 8: return "DISTANCIA MAXIMA DESDE CENTRO DEL GRUPO EN CELDAS.";
                case 9:
                case 10:
                case 11: return "COSTO MENOR PERMITE MAS UNIDADES POR EL MISMO PRESUPUESTO.";
                default: return "ON USA PREFABS JSON Y SUS SPAWNS. OFF USA FORMAS PROCEDURALES.";
            }
        }

        private void RegisterTemplateSpawn(Vector2 position, EnemyType type)
        {
            if (type == EnemyType.Spawner)
            {
                if (_levelManager.IsPlayableAndWalkable(position, GameConstants.SpawnerRadius))
                {
                    _spawnerManager.Register(position);
                }
                return;
            }

            _enemyManager.Spawn(position, Vector2.Zero, type);
        }

        /// <summary>
        /// Inicializa los jugadores en sus posiciones de spawn.
        /// </summary>
        private void InitializePlayers()
        {
            // Spawns iniciales en el marcador de spawn del mapa
            Vector2 spawnPosition = _levelManager.GetSpawnPosition();
            Vector2[] offsets =
            {
                new Vector2(-18, -18), new Vector2(18, -18),
                new Vector2(-18, 18),  new Vector2(18, 18),
            };
            
            for (int i = 0; i < GameConstants.MaxPlayers; i++)
            {
                Vector2 requestedPosition = spawnPosition + offsets[i];
                bool hasValidSpawn = TryFindValidSpawnPosition(
                    requestedPosition,
                    GameConstants.PlayerRadius,
                    out Vector2 playerSpawnPosition);
                _players[i] = new Player(i, playerSpawnPosition);
                _players[i].IsActive = i == 0 && hasValidSpawn;
            }
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
                    _players[i].IsActive = i == 0 &&
                        _levelManager.IsPlayableAndWalkable(_players[i].Position, GameConstants.PlayerRadius);
                }
                else if (mode == GameState.Multiplayer)
                {
                    // Todos los jugadores están activos en modo multijugador
                    _players[i].IsActive = _levelManager.IsPlayableAndWalkable(
                        _players[i].Position,
                        GameConstants.PlayerRadius);
                }
            }
        }
    }
}
