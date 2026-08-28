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
        private EnemyBulletManager _enemyBulletManager;
        private ParticleSystem _particleSystem;
        private LevelManager _levelManager;
        private EnemyManager _enemyManager;
        private EnemyRenderer _enemyRenderer;
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
            InitializePlayers();

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
            for (int i = 0; i < GameConstants.MaxPlayers; i++)
            {
                if (_players[i].IsActive && _inputManager.GetState(i).IsConnected)
                {
                    // Verifica si el jugador ha alcanzado el marcador de salida
                    if (_levelManager.CheckExitReached(_players[i].Position))
                    {
                        SetDebugMessage("¡Nivel completado!");
                        // Aquí puedes agregar lógica adicional para finalizar el nivel
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

            _bulletManager.Update(deltaTime, _enemyManager.Enemies);
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
            _shipRenderer.Draw(GraphicsDevice, _players, viewMatrix);
            _bulletRenderer.Draw(GraphicsDevice, _bulletManager.Bullets, viewMatrix);
            _bulletRenderer.Draw(GraphicsDevice, _enemyBulletManager.Bullets, viewMatrix);

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
        /// Busca una posición válida cerca de la posición original para spawnear entidades.
        /// </summary>
        private Vector2 FindValidSpawnPosition(Vector2 originalPosition, Vector2 spawnRoomCenter, float radius = GameConstants.PlayerRadius)
        {
            // Buscar en un radio creciente alrededor de la posición original
            for (int searchRadius = 1; searchRadius < 20; searchRadius++)
            {
                for (int angle = 0; angle < 360; angle += 15)
                {
                    float radians = MathHelper.ToRadians(angle);
                    Vector2 testPosition = originalPosition + new Vector2(
                        (float)Math.Cos(radians) * searchRadius * GameConstants.GridCellSize,
                        (float)Math.Sin(radians) * searchRadius * GameConstants.GridCellSize
                    );
                    if (_levelManager.IsWalkable(testPosition, radius))
                    {
                        return testPosition;
                    }
                }
            }
            // Si no se encuentra una posición válida, hacer clamp hacia el centro de la sala de spawn
            Console.WriteLine("[Game1] ADVERTENCIA: No se encontró una posición válida para spawnear. Haciendo clamp al centro de la sala.");
            return spawnRoomCenter;
        }

        /// <summary>
        /// Spawnea enemigos de prueba de tipos configurables alrededor de un punto base.
        /// Sirve para validar comportamientos sin sobre-diseñar la infraestructura.
        /// </summary>
        private void SpawnDebugEnemies(SpawnSpec[] specs, Vector2 basePosition)
        {
            foreach (SpawnSpec spec in specs)
            {
                Vector2 candidate = basePosition + spec.Offset;
                Vector2 safePosition = _levelManager.IsWalkable(candidate, GameConstants.EnemyRadius)
                    ? candidate
                    : FindValidSpawnPosition(candidate, basePosition, GameConstants.EnemyRadius);

                if (_levelManager.IsWalkable(safePosition, GameConstants.EnemyRadius))
                {
                    _enemyManager.Spawn(safePosition, Vector2.Zero, spec.Type);
                }
            }
        }

        private readonly struct SpawnSpec
        {
            public SpawnSpec(EnemyType type, Vector2 offset)
            {
                Type = type;
                Offset = offset;
            }

            public EnemyType Type { get; }
            public Vector2 Offset { get; }
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
            SetDebugMessage($"Mapa generado: {wallCount} colisiones");
        }

        /// <summary>
        /// Inicializa los managers del juego (input, level, bullet, particle, camera, debug).
        /// </summary>
        private void InitializeManagers()
        {
            _inputManager = new InputManager();
            _levelManager = new LevelManager(GameConstants.GridWidth, GameConstants.GridHeight, GameConstants.GridCellSize);
            _bulletManager = new BulletManager(_levelManager);
            _enemyBulletManager = new EnemyBulletManager(_levelManager);
            _particleSystem = new ParticleSystem(GameConstants.MaxParticles);
            _enemyManager = new EnemyManager(_levelManager);
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
            // Cargar el mapa de prueba ANTES de crear el renderer
            // Generar un mapa procedural
            MapLoader.GenerateProceduralMap(_levelManager);
            Console.WriteLine($"[Game1] Puntos de spawn de enemigos en salas: {_levelManager.MapGenerator.RoomEnemySpawnPoints.Count}");
            SpawnTestEnemies();
        }

        /// <summary>
        /// Spawnea enemigos de prueba en posiciones válidas dentro del mapa.
        /// </summary>
        private void SpawnTestEnemies()
        {
            if (!GameConstants.UseRoomTemplates)
            {
                // Comportamiento original (hardcodeado)
                Vector2 spawnPosition = _levelManager.GetSpawnPosition();
                Console.WriteLine($"[Game1] SpawnPosition: {spawnPosition}");
                Vector2[] enemyPositions = 
                {
                    spawnPosition + new Vector2(50, 50),
                    spawnPosition + new Vector2(100, 100)
                };

                for (int i = 0; i < 2; i++)
                {
                    Vector2 position = enemyPositions[i];
                    Console.WriteLine($"[Game1] Intentando spawnear enemigo {i} en posición: {position}");
                    if (_levelManager.IsWalkable(position, GameConstants.EnemyRadius))
                    {
                        Console.WriteLine($"[Game1] Posición válida para enemigo {i}. Spawneando...");
                        _enemyManager.Spawn(position, new Vector2(10f, 10f));
                    }
                    else
                    {
                        Console.WriteLine($"[Game1] ADVERTENCIA: Posición no válida para enemigo {i}. Buscando alternativa...");
                        Vector2 alternativePosition = FindValidSpawnPosition(position, spawnPosition);
                        Console.WriteLine($"[Game1] Posición alternativa para enemigo {i}: {alternativePosition}");
                        _enemyManager.Spawn(alternativePosition, new Vector2(10f, 10f));
                    }
                }

                // TODO: remover cuando el sistema de spawns por plantilla (Etapa 5) esté listo.
                SpawnDebugEnemies(new[]
                {
                    new SpawnSpec(EnemyType.Roamer, new Vector2(150f, 150f)),
                    new SpawnSpec(EnemyType.Roamer, new Vector2(210f, 120f)),
                    new SpawnSpec(EnemyType.Swarmer, new Vector2(260f, 180f)),
                    new SpawnSpec(EnemyType.Turret, new Vector2(120f, 80f))
                }, spawnPosition + new Vector2(100f, 100f));
                return;
            }

            // Nuevo comportamiento basado en RoomEnemySpawnPoints y salas
            Console.WriteLine("[Game1] Spawneando enemigos usando plantillas de salas y reglas generales...");
            
            // 1. Iterar los puntos de spawn reales de las plantillas (RoomEnemySpawnPoints)
            if (_levelManager.MapGenerator.RoomEnemySpawnPoints != null && _levelManager.MapGenerator.RoomEnemySpawnPoints.Count > 0)
            {
                var spawnPoints = _levelManager.MapGenerator.RoomEnemySpawnPoints;
                for (int i = 0; i < spawnPoints.Count; i++)
                {
                    if (_enemyManager.ActiveCount >= GameConstants.MaxEnemies)
                    {
                        Console.WriteLine($"[Game1] Límite MaxEnemies alcanzado. Se omitieron {spawnPoints.Count - i} spawns de plantillas.");
                        goto EndSpawning;
                    }

                    Vector2 worldPos = spawnPoints[i];
                    if (_levelManager.IsWalkable(worldPos, GameConstants.EnemyRadius))
                    {
                        _enemyManager.Spawn(worldPos, new Vector2(10f, 10f), EnemyType.Swarmer);
                        Console.WriteLine($"[Game1] Enemigo spawneado en RoomEnemySpawnPoint: {worldPos}");
                    }
                    else
                    {
                        Vector2 validPos = FindValidSpawnPosition(worldPos, _levelManager.GetSpawnPosition());
                        _enemyManager.Spawn(validPos, new Vector2(10f, 10f), EnemyType.Swarmer);
                        Console.WriteLine($"[Game1] Enemigo spawneado en fallback para RoomEnemySpawnPoint: {validPos}");
                    }
                }
            }

            // TODO: remover cuando el sistema de spawns por plantilla (Etapa 5) esté listo.
            SpawnDebugEnemies(new[]
            {
                new SpawnSpec(EnemyType.Roamer, new Vector2(200f, 200f)),
                new SpawnSpec(EnemyType.Roamer, new Vector2(260f, 160f)),
                new SpawnSpec(EnemyType.Swarmer, new Vector2(210f, 240f)),
                new SpawnSpec(EnemyType.Turret, new Vector2(120f, 80f))
            }, _levelManager.GetSpawnPosition());

            // 2. Regla para el resto (salas sin plantilla):
            // 1 enemigo cada N celdas² de área de sala, colocado en un punto random walkable dentro del Rectangle de la sala.
            if (_levelManager.MapGenerator.Rooms != null)
            {
                Random rand = new Random(42);
                var rooms = _levelManager.MapGenerator.Rooms;
                for (int i = 0; i < rooms.Count; i++)
                {
                    var room = rooms[i];
                    int area = room.Width * room.Height;
                    if (area <= 0) continue;

                    int enemyCount = area / 300;
                    if (enemyCount < 1 && area > 150) enemyCount = 1;

                    for (int c = 0; c < enemyCount; c++)
                    {
                        if (_enemyManager.ActiveCount >= GameConstants.MaxEnemies)
                        {
                            int remainingInRoom = enemyCount - c;
                            int remainingInOtherRooms = 0;
                            for (int j = i + 1; j < rooms.Count; j++)
                            {
                                int a = rooms[j].Width * rooms[j].Height;
                                if (a <= 0) continue;
                                int ec = a / 300;
                                if (ec < 1 && a > 150) ec = 1;
                                remainingInOtherRooms += ec;
                            }
                            Console.WriteLine($"[Game1] Límite MaxEnemies alcanzado. Se omitieron {remainingInRoom + remainingInOtherRooms} spawns de regla de área.");
                            goto EndSpawning;
                        }

                        for (int attempt = 0; attempt < 10; attempt++)
                        {
                            int rx = rand.Next(room.X + 1, room.X + room.Width - 1);
                            int ry = rand.Next(room.Y + 1, room.Y + room.Height - 1);
                            
                            Vector2 worldPos = _levelManager.GridToWorld(new Point(rx, ry));
                            
                            if (_levelManager.IsWalkable(worldPos, GameConstants.EnemyRadius))
                            {
                                // Tarea 2: Verificar distancia a RoomEnemySpawnPoints
                                bool tooClose = false;
                                if (_levelManager.MapGenerator.RoomEnemySpawnPoints != null)
                                {
                                    float minDistanceSq = (GameConstants.EnemyRadius * 3) * (GameConstants.EnemyRadius * 3);
                                    foreach (var sp in _levelManager.MapGenerator.RoomEnemySpawnPoints)
                                    {
                                        if (Vector2.DistanceSquared(worldPos, sp) < minDistanceSq)
                                        {
                                            tooClose = true;
                                            break;
                                        }
                                    }
                                }

                                if (!tooClose)
                                {
                                    _enemyManager.Spawn(worldPos, new Vector2(10f, 10f));
                                    Console.WriteLine($"[Game1] Enemigo spawneado por regla general en sala ({room.X},{room.Y}): {worldPos}");
                                    break;
                                }
                            }
                        }
                    }
                }
            }

        EndSpawning:
            Console.WriteLine($"[Game1] Spawn total: {_enemyManager.ActiveCount}/{GameConstants.MaxEnemies}");
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
            
            // Calcular el centro de la sala de spawn (asumiendo que spawnPosition es la esquina superior izquierda)
            Vector2 spawnRoomCenter = spawnPosition + new Vector2(18, 18);

            for (int i = 0; i < GameConstants.MaxPlayers; i++)
            {
                Vector2 playerSpawnPosition = spawnPosition + offsets[i];
                // Validar que la posición de spawn sea transitable
                if (!_levelManager.IsWalkable(playerSpawnPosition, GameConstants.PlayerRadius))
                {
                    // Si no es transitable, buscar una posición cercana válida
                    playerSpawnPosition = FindValidSpawnPosition(playerSpawnPosition, spawnRoomCenter);
                }
                _players[i] = new Player(i, playerSpawnPosition);
                _players[i].IsActive = (i == 0); // Solo el jugador 0 está activo por defecto
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
