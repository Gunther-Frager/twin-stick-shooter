using Microsoft.Xna.Framework;
using TwinStickShooter.Core;
using TwinStickShooter.Entities;
using Xunit;

namespace TwinStickShooter.Tests
{
    public class EnemyManagerTests
    {
        [Fact]
        public void Spawn_AssignsDistinctBalanceAndColorToNewArchetypes()
        {
            LevelManager level = CreateArena();
            var manager = new EnemyManager(level);

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