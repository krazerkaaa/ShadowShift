using NUnit.Framework;
using ShadowShift.Model;

namespace ShadowShift.Tests
{
    // Модульные тесты для проверки физики, фаз, скорости и загрузчика уровней без запуска окна
    [TestFixture]
    public class GameModelTests
    {
        [Test]
        public void Jump_WhenGrounded_LiftsPlayerUpAndGravityPullsBack()
        {
            var model = new GameModel();
            model.StartSelectedLevel();
            model.Obstacles.Clear();
            var initialY = model.Player.Y;

            // Прыгаем и проверяем, что куб поднялся вверх
            model.Jump();
            model.Update(0.1f);
            Assert.That(model.Player.Y, Is.LessThan(initialY));
            Assert.That(model.Player.IsGrounded, Is.False);

            // Прокручиваем 60 кадров и проверяем приземление на землю
            for (var i = 0; i < 60; i++)
                model.Update(0.016f);

            Assert.That(model.Player.Y, Is.EqualTo(initialY).Within(0.01f));
            Assert.That(model.Player.IsGrounded, Is.True);
        }

        [Test]
        public void SwitchPhase_TogglesBetweenLightAndShadow()
        {
            var model = new GameModel();
            model.StartSelectedLevel();
            Assert.That(model.Player.CurrentPhase, Is.EqualTo(Phase.Light));

            model.SwitchPhase();
            Assert.That(model.Player.CurrentPhase, Is.EqualTo(Phase.Shadow));

            model.SwitchPhase();
            Assert.That(model.Player.CurrentPhase, Is.EqualTo(Phase.Light));
        }

        [Test]
        public void Collision_WithMatchingPhaseBarrier_DoesNotRestart()
        {
            var model = new GameModel();
            model.StartSelectedLevel();
            model.Obstacles.Clear();
            model.Obstacles.Add(new Obstacle(model.Player.X + 10, GameModel.GroundY - 100, 40, 100, ObstacleType.PhaseBarrier, Phase.Light));

            model.Update(0.05f);

            // Фазы совпадают — игрок проходит сквозь барьер без смерти
            Assert.That(model.Attempt, Is.EqualTo(1));
        }

        [Test]
        public void Collision_WithOppositePhaseBarrier_RestartsAttempt()
        {
            var model = new GameModel();
            model.StartSelectedLevel();
            model.Obstacles.Clear();
            model.Obstacles.Add(new Obstacle(model.Player.X + 10, GameModel.GroundY - 100, 40, 100, ObstacleType.PhaseBarrier, Phase.Shadow));

            model.Update(0.05f);

            // Фазы разные — происходит рестарт попытки
            Assert.That(model.Attempt, Is.EqualTo(2));
        }

        [Test]
        public void Collision_WithSpike_RestartsAttemptRegardlessOfPhase()
        {
            var model = new GameModel();
            model.StartSelectedLevel();
            model.Obstacles.Clear();
            model.Obstacles.Add(new Obstacle(model.Player.X + 10, GameModel.GroundY - 40, 40, 40, ObstacleType.Spike));

            model.SwitchPhase();
            model.Update(0.05f);

            Assert.That(model.Attempt, Is.EqualTo(2));
        }

        [Test]
        public void CeilingSpike_AllowsGroundedPass_ButRestartsOnJump()
        {
            var model = new GameModel();
            model.StartSelectedLevel();
            model.Obstacles.Clear();
            // Шип висит на высоте 65 px от пола: по земле куб проезжает, а в прыжке врезается
            model.Obstacles.Add(new Obstacle(model.Player.X + 15, GameModel.GroundY - 65 - 40, 40, 40, ObstacleType.CeilingSpike));

            model.Jump();
            model.Update(0.08f);

            Assert.That(model.Attempt, Is.EqualTo(2));
        }

        [Test]
        public void SpeedPortal_ChangesPlayerSpeedMultiplier()
        {
            var model = new GameModel();
            model.StartSelectedLevel();
            model.Obstacles.Clear();
            model.Obstacles.Add(new Obstacle(model.Player.X + 10, GameModel.GroundY - 200, 36, 200, ObstacleType.SpeedPortal, Phase.Light, 1.4f));

            model.Update(0.05f);

            Assert.That(model.CurrentSpeedMultiplier, Is.EqualTo(1.4f));
            Assert.That(model.Attempt, Is.EqualTo(1));
        }

        [Test]
        public void LevelLoader_ParsesAllCommandsCorrectly()
        {
            var lines = new[]
            {
                "# Комментарий",
                "LENGTH 3000",
                "SPIKE 500",
                "CEILING_SPIKE 800 65",
                "BARRIER 1100 140 SHADOW",
                "SPEED 1500 1.35"
            };

            var (obstacles, length) = LevelLoader.ParseLines(lines, GameModel.GroundY);

            Assert.That(length, Is.EqualTo(3000f));
            Assert.That(obstacles.Count, Is.EqualTo(4));
            Assert.That(obstacles[0].Type, Is.EqualTo(ObstacleType.Spike));
            Assert.That(obstacles[1].Type, Is.EqualTo(ObstacleType.CeilingSpike));
            Assert.That(obstacles[2].Type, Is.EqualTo(ObstacleType.PhaseBarrier));
            Assert.That(obstacles[2].Phase, Is.EqualTo(Phase.Shadow));
            Assert.That(obstacles[3].Type, Is.EqualTo(ObstacleType.SpeedPortal));
            Assert.That(obstacles[3].SpeedMultiplier, Is.EqualTo(1.35f));
        }

        [Test]
        public void ReachingLevelLength_CompletesLevel()
        {
            var model = new GameModel();
            model.StartSelectedLevel();
            model.Obstacles.Clear();

            for (var i = 0; i < 1000 && model.State == GameState.Playing; i++)
                model.Update(0.016f);

            Assert.That(model.State, Is.EqualTo(GameState.Completed));
            Assert.That(model.ProgressPercent, Is.EqualTo(100));
        }
    }
}