using Microsoft.Xna.Framework;
using System.Linq;
using TwinStickShooter.Core;
using TwinStickShooter.Entities;
using TwinStickShooter.Input;
using Xunit;

namespace TwinStickShooter.Tests
{
    public class EnemyManagerTests
    {
        [Fact]
        public void Spawn_AssignsDistinctBalanceAndColorToNewArchetypes()
        {
            LevelManager level = CreateArena();
            level.MapGenerator.Settings.EnemyPoolCapacity = 128;
            var manager = new EnemyManager(level);
            Assert.Equal(128, manager.Capacity);

            Assert.True(manager.Spawn(new Vector2(1000f, 1000f), Vector2.Zero, EnemyType.Rusher));
            Assert.True(manager.Spawn(new Vector2(1100f, 1000f), Vector2.Zero, EnemyType.StaticShooter));
            Assert.True(manager.Spawn(new Vector2(1200f, 1000f), Vector2.Zero, EnemyType.MobileGenerator));

            Enemy rusher = Find(manager, EnemyType.Rusher);
            Enemy shooter = Find(manager, EnemyType.StaticShooter);
            Enemy generator = Find(manager, EnemyType.MobileGenerator);

            Assert.Equal(GameConstants.RusherRadius, rusher.Radius);
            Assert.Equal(GameConstants.RusherHealth, rusher.MaxHealth);
            Assert.Equal(GameConstants.StaticShooterRadius, shooter.Radius);
            Assert.Equal(GameConstants.StaticShooterHealth, shooter.MaxHealth);
            Assert.Equal(GameConstants.MobileGeneratorRadius, generator.Radius);
            Assert.Equal(GameConstants.MobileGeneratorHealth, generator.MaxHealth);
            Assert.NotEqual(rusher.Color, shooter.Color);
            Assert.NotEqual(rusher.Color, generator.Color);
            Assert.NotEqual(shooter.Color, generator.Color);
        }

        [Fact]
        public void Rusher_MovesTowardNearestActivePlayer()
        {
            LevelManager level = CreateArena();
            var manager = new EnemyManager(level);
            var bullets = new EnemyBulletManager(level);
            var player = new Player(0, new Vector2(1700f, 1200f)) { IsActive = true };
            var players = new[] { player };
            Vector2 start = new Vector2(1200f, 1200f);
            Assert.True(manager.Spawn(start, Vector2.Zero, EnemyType.Rusher));

            manager.Update(0.5f, players, bullets);

            Enemy rusher = Find(manager, EnemyType.Rusher);
            Assert.True(rusher.Position.X > start.X);
            Assert.Equal(start.Y, rusher.Position.Y);
        }

        [Fact]
        public void StaticShooter_FiresWithoutChangingPosition()
        {
            LevelManager level = CreateArena();
            var manager = new EnemyManager(level);
            var bullets = new EnemyBulletManager(level);
            var player = new Player(0, new Vector2(1600f, 1200f)) { IsActive = true };
            var players = new[] { player };
            Vector2 start = new Vector2(1200f, 1200f);
            Assert.True(manager.Spawn(start, Vector2.Zero, EnemyType.StaticShooter));

            manager.Update(GameConstants.StaticShooterShootCooldown + 0.01f, players, bullets);

            Assert.Equal(start, Find(manager, EnemyType.StaticShooter).Position);
            Assert.Contains(bullets.Bullets, bullet => bullet.Active);
        }

        [Fact]
        public void MobileGenerator_MovesAndDeploysARusher()
        {
            LevelManager level = CreateArena();
            var manager = new EnemyManager(level);
            var bullets = new EnemyBulletManager(level);
            var player = new Player(0, new Vector2(1800f, 1200f)) { IsActive = true };
            var players = new[] { player };
            Vector2 start = new Vector2(1200f, 1200f);
            Assert.True(manager.Spawn(start, Vector2.Zero, EnemyType.MobileGenerator));

            manager.Update(GameConstants.MobileGeneratorSpawnInterval, players, bullets);

            Assert.True(Find(manager, EnemyType.MobileGenerator).Position.X > start.X);
            Assert.Contains(manager.Enemies, enemy => enemy.Active && enemy.Type == EnemyType.Rusher);
        }

        [Fact]
        public void RegionOwnedStaticShooter_DoesNotFireUntilItsRegionActivates()
        {
            LevelManager level = CreateArena();
            var manager = new EnemyManager(level);
            var bullets = new EnemyBulletManager(level);
            var player = new Player(0, new Vector2(1600f, 1200f)) { IsActive = true };
            var players = new[] { player };
            Vector2 start = new Vector2(1200f, 1200f);
            Assert.True(manager.Spawn(start, Vector2.Zero, EnemyType.StaticShooter, 3));

            manager.Update(GameConstants.StaticShooterShootCooldown + 0.1f, players, bullets);

            Enemy shooter = Find(manager, EnemyType.StaticShooter);
            Assert.Equal(EnemyAwarenessState.Dormant, shooter.AwarenessState);
            Assert.DoesNotContain(bullets.Bullets, bullet => bullet.Active);

            Assert.True(manager.ActivateRegion(3));
            Assert.False(manager.ActivateRegion(3));
            manager.Update(GameConstants.StaticShooterShootCooldown + 0.1f, players, bullets);

            Assert.Equal(EnemyAwarenessState.Active, shooter.AwarenessState);
            Assert.Contains(bullets.Bullets, bullet => bullet.Active);
        }

        [Fact]
        public void ActivatedMobileGenerator_ChildrenInheritRegionAndAwareness()
        {
            LevelManager level = CreateArena();
            var manager = new EnemyManager(level);
            var bullets = new EnemyBulletManager(level);
            var player = new Player(0, new Vector2(1800f, 1200f)) { IsActive = true };
            Assert.True(manager.Spawn(new Vector2(1200f, 1200f), Vector2.Zero, EnemyType.MobileGenerator, 7));

            Assert.True(manager.ActivateRegion(7));
            manager.Update(GameConstants.MobileGeneratorSpawnInterval, new[] { player }, bullets);

            Enemy child = Find(manager, EnemyType.Rusher);
            Assert.Equal(7, child.RegionId);
            Assert.Equal(EnemyAwarenessState.Active, child.AwarenessState);
            Assert.Equal(2, manager.GetAliveCountInRegion(7));
            Assert.Equal(0, manager.GetAliveCountInRegion(8));
        }

        [Fact]
        public void StaticShooter_FiresOnlyWhenPlayerIsInsideViewportAndHasLineOfSight()
        {
            LevelManager level = CreateArena();
            var manager = new EnemyManager(level);
            var bullets = new EnemyBulletManager(level);
            var player = new Player(0, new Vector2(1600f, 1200f)) { IsActive = true };
            var players = new[] { player };
            Assert.True(manager.Spawn(new Vector2(1200f, 1200f), Vector2.Zero, EnemyType.StaticShooter));

            manager.Update(2f, players, bullets, new Rectangle(0, 0, 1400, 1400));
            Assert.DoesNotContain(bullets.Bullets, bullet => bullet.Active);

            manager.Update(2f, players, bullets, new Rectangle(0, 0, 1800, 1400));
            Assert.Contains(bullets.Bullets, bullet => bullet.Active);

            LevelManager blockedLevel = CreateArena();
            blockedLevel.SetCollision(17, 14, true);
            blockedLevel.SetCollision(17, 15, true);
            blockedLevel.SetCollision(17, 16, true);
            blockedLevel.RebuildPrimitiveMapFromGrid();
            Assert.False(blockedLevel.HasLineOfSight(new Vector2(1200f, 1200f), new Vector2(1600f, 1200f)));
            var blockedManager = new EnemyManager(blockedLevel);
            var blockedBullets = new EnemyBulletManager(blockedLevel);
            Assert.True(blockedManager.Spawn(new Vector2(1200f, 1200f), Vector2.Zero, EnemyType.StaticShooter));
            blockedManager.Update(2f, players, blockedBullets, new Rectangle(0, 0, 1800, 1400));

            Assert.DoesNotContain(blockedBullets.Bullets, bullet => bullet.Active);
        }

        [Fact]
        public void DormantRoamer_PatrolsWithoutLeavingItsOwnedRegion()
        {
            var level = new LevelManager(30, 30, 80);
            MapGenerationSettings settings = level.MapGenerator.Settings;
            settings.Seed = 818;
            settings.CrawlerCount = 4;
            settings.CrawlerMaxSteps = 300;
            settings.CrawlerRoomInterval = 8;
            settings.MinArenaCount = 1;
            settings.MaxArenaCount = 1;
            settings.MinimumSpawnExitPathLength = 12;
            settings.PocketChance = 0f;
            settings.MaxPocketCount = 0;
            settings.ArenaObstacleMinSeeds = 0;
            settings.ArenaObstacleMaxSeeds = 0;
            level.GenerateProceduralMap();

            MapRegionDefinition arena = level.MapGenerator.Regions.First(region => region.Kind == MapRegionKind.Arena);
            int[,] grid = level.GetCollisionGridSnapshot();
            Point spawnCell = Point.Zero;
            bool foundCell = false;
            for (int x = arena.Bounds.Left; x < arena.Bounds.Right && !foundCell; x++)
            {
                for (int y = arena.Bounds.Top; y < arena.Bounds.Bottom; y++)
                {
                    if (grid[x, y] != 0 || level.MapGenerator.RegionIdMap[x, y] != arena.Id) continue;
                    spawnCell = new Point(x, y);
                    foundCell = true;
                    break;
                }
            }
            Assert.True(foundCell);

            var manager = new EnemyManager(level);
            var bullets = new EnemyBulletManager(level);
            Vector2 start = level.GridToWorld(spawnCell);
            Assert.True(manager.Spawn(start, Vector2.Zero, EnemyType.Roamer, arena.Id));
            Player distantPlayer = new Player(0, level.GetSpawnPosition()) { IsActive = true };

            manager.Update(8f, new[] { distantPlayer }, bullets);

            Enemy roamer = Find(manager, EnemyType.Roamer);
            Assert.Equal(EnemyAwarenessState.Dormant, roamer.AwarenessState);
            Assert.Equal(arena.Id, level.GetRegionIdAt(roamer.Position));
        }

        [Fact]
        public void PlayerEnemyContact_SeparatesBodiesAndRefreshesSlowWithoutDamage()
        {
            LevelManager level = CreateArena();
            var manager = new EnemyManager(level);
            var player = new Player(0, new Vector2(1200f, 1200f)) { IsActive = true };
            Assert.True(manager.Spawn(new Vector2(1220f, 1200f), Vector2.Zero, EnemyType.Rusher));

            Assert.Equal(1, manager.ResolvePlayerContacts(new[] { player }));

            Enemy rusher = Find(manager, EnemyType.Rusher);
            Assert.True(Vector2.Distance(player.Position, rusher.Position) >= player.Radius + rusher.Radius);
            Assert.Equal(GameConstants.ContactSlowDurationSeconds, player.ContactSlowTimer);
            Assert.Equal(GameConstants.PlayerMaxHealth, player.Health);
            Assert.Equal(0f, player.InvulnerabilityTimer);
        }

        [Fact]
        public void ContactSlow_UsesFixedMultiplierRefreshesTimerAndExpires()
        {
            var player = new Player(0, new Vector2(500f, 500f));
            var input = new PlayerInputState
            {
                IsConnected = true,
                MoveDirection = Vector2.UnitX,
            };

            player.ApplyContactSlow();
            player.Update(in input, 0.1f);
            Assert.Equal(GameConstants.PlayerSpeed * GameConstants.ContactSlowMultiplier * 0.1f,
                player.Position.X - 500f,
                3);
            Assert.Equal(0.3f, player.ContactSlowTimer, 3);

            player.ApplyContactSlow();
            Assert.Equal(GameConstants.ContactSlowDurationSeconds, player.ContactSlowTimer);
            Assert.Equal(GameConstants.ContactSlowMultiplier, player.MovementSpeedMultiplier);

            player.Update(in input, GameConstants.ContactSlowDurationSeconds);
            Assert.Equal(0f, player.ContactSlowTimer);
            Assert.Equal(1f, player.MovementSpeedMultiplier);
            Assert.Equal(GameConstants.PlayerMaxHealth, player.Health);
        }

        [Fact]
        public void Spawner_GeneratesBeyondOldLocalLimitAndChildrenCountTowardRegion()
        {
            LevelManager level = CreateArena();
            var enemyManager = new EnemyManager(level);
            var spawnerManager = new SpawnerManager(2, enemyManager);
            Assert.True(spawnerManager.Register(new Vector2(1200f, 1200f), 9));
            Assert.Equal(1, spawnerManager.GetAliveCountInRegion(9));

            for (int i = 0; i < 2; i++)
                spawnerManager.Update(GameConstants.SpawnerInterval);
            Assert.Equal(0, enemyManager.GetAliveCountInRegion(9));

            enemyManager.ActivateRegion(9);
            spawnerManager.ActivateRegion(9);
            for (int i = 0; i < 8; i++)
                spawnerManager.Update(GameConstants.SpawnerInterval);

            Assert.True(enemyManager.GetAliveCountInRegion(9) > GameConstants.SpawnerMaxConcurrentChildren);
            Assert.Equal(1, spawnerManager.GetAliveCountInRegion(9));
        }

        private static LevelManager CreateArena()
        {
            var level = new LevelManager(GameConstants.GridWidth, GameConstants.GridHeight, 80);
            level.ConfigureCombatTestArena();
            return level;
        }

        private static Enemy Find(EnemyManager manager, EnemyType type)
        {
            foreach (Enemy enemy in manager.Enemies)
            {
                if (enemy.Active && enemy.Type == type) return enemy;
            }

            Assert.Fail($"Expected active enemy of type {type}.");
            return null;
        }
    }
}