namespace ShadowShift.Model
{
    // Главная модель игры (слой Model в MVC): отвечает за физику, коллизии, скорость и прогресс
    public class GameModel
    {
        public const float GroundY = 560f;          // Высота линии пола
        private const float PlayerStartX = 180f;    // Начальная координата X игрока
        private const float BaseRunSpeed = 480f;    // Базовая скорость бега (пикс/сек)
        private const float Gravity = 2400f;        // Ускорение свободного падения
        private const float JumpImpulse = -780f;    // Начальная скорость прыжка вверх

        public Player Player { get; } = new Player();
        public List<Obstacle> Obstacles { get; } = new List<Obstacle>();
        public List<LevelInfo> AvailableLevels { get; }
        public int SelectedLevelIndex { get; private set; }
        public GameState State { get; private set; } = GameState.MainMenu;

        public int Attempt { get; private set; } = 1;
        public float LevelLength { get; private set; }
        public float ElapsedTime { get; private set; }
        public float CurrentSpeedMultiplier { get; private set; } = 1.0f;

        public LevelInfo CurrentLevel => AvailableLevels[SelectedLevelIndex];

        // Процент прохождения текущего уровня
        public int ProgressPercent =>
            LevelLength <= 0 ? 0 : (int)Math.Clamp((Player.X / LevelLength) * 100f, 0f, 100f);

        // События для слоя View (чтобы проигрывать звуки без привязки Model к Windows Forms)
        public event Action? MenuNavigated;
        public event Action? Jumped;
        public event Action? PhaseSwitched;
        public event Action? PlayerCrashed;
        public event Action? LevelCompleted;
        public event Action<float>? SpeedChanged;

        public GameModel()
        {
            AvailableLevels = LevelLoader.EnsureDefaultLevels();
        }

        // Переключает выбранный пункт в главном меню вверх или вниз по кругу
        public void ChangeMenuSelection(int delta)
        {
            if (State != GameState.MainMenu || AvailableLevels.Count == 0) return;
            SelectedLevelIndex = (SelectedLevelIndex + delta + AvailableLevels.Count) % AvailableLevels.Count;
            MenuNavigated?.Invoke();
        }

        // Загружает выбранный уровень из файла и начинает забег
        public void StartSelectedLevel()
        {
            var (loadedObstacles, length) = LevelLoader.LoadFromFile(CurrentLevel.FileName, GroundY);
            Obstacles.Clear();
            Obstacles.AddRange(loadedObstacles);
            LevelLength = length;

            Attempt = 1;
            RestartAttempt(incrementAttempt: false);
            State = GameState.Playing;
        }

        // Возвращает игрока из забега в главное меню
        public void ReturnToMenu()
        {
            State = GameState.MainMenu;
        }

        // Запускает прыжок, если куб находится на платформе
        public void Jump()
        {
            if (State != GameState.Playing || !Player.IsGrounded) return;
            Player.VelocityY = JumpImpulse;
            Player.IsGrounded = false;
            Jumped?.Invoke();
        }

        // Меняет цветовую фазу игрока
        public void SwitchPhase()
        {
            if (State != GameState.Playing) return;
            Player.SwitchPhase();
            PhaseSwitched?.Invoke();
        }

        // Обновляет физику и проверяет столкновения за один кадр
        public void Update(float deltaTime)
        {
            if (State != GameState.Playing) return;

            ElapsedTime += deltaTime;
            // Двигаем игрока вправо с учётом текущего множителя скорости
            Player.X += BaseRunSpeed * CurrentSpeedMultiplier * deltaTime;
            ApplyVerticalPhysics(deltaTime);

            // Если добежали до финиша — завершаем уровень
            if (Player.X >= LevelLength)
            {
                Player.X = LevelLength;
                State = GameState.Completed;
                LevelCompleted?.Invoke();
                return;
            }

            ProcessCollisions();
        }

        // Сбрасывает позицию игрока в начало уровня при смерти или ручном рестарте
        public void RestartAttempt(bool incrementAttempt)
        {
            if (incrementAttempt) Attempt++;
            ElapsedTime = 0f;
            CurrentSpeedMultiplier = 1.0f;
            Player.Reset(PlayerStartX, GroundY);
            if (State == GameState.Completed)
                State = GameState.Playing;
        }

        // Считает вертикальное движение под действием гравитации
        private void ApplyVerticalPhysics(float deltaTime)
        {
            Player.VelocityY += Gravity * deltaTime;
            Player.Y += Player.VelocityY * deltaTime;

            var floorTop = GroundY - Player.Size;
            if (Player.Y >= floorTop)
            {
                Player.Y = floorTop;
                Player.VelocityY = 0f;
                Player.IsGrounded = true;
            }
        }

        // Проверяет столкновения игрока с шипами, барьерами и порталами
        private void ProcessCollisions()
        {
            foreach (var obstacle in Obstacles)
            {
                if (!Intersects(Player, obstacle)) continue;

                // Портал скорости не убивает, а меняет темп игры
                if (obstacle.Type == ObstacleType.SpeedPortal)
                {
                    if (Math.Abs(CurrentSpeedMultiplier - obstacle.SpeedMultiplier) > 0.01f)
                    {
                        CurrentSpeedMultiplier = obstacle.SpeedMultiplier;
                        SpeedChanged?.Invoke(CurrentSpeedMultiplier);
                    }
                    continue;
                }

                // Нижние и верхние шипы смертельны всегда
                if (obstacle.Type == ObstacleType.Spike || obstacle.Type == ObstacleType.CeilingSpike)
                {
                    PlayerCrashed?.Invoke();
                    RestartAttempt(incrementAttempt: true);
                    return;
                }

                // Фазовый барьер убивает, только если фаза игрока не совпадает с фазой барьера
                if (obstacle.Type == ObstacleType.PhaseBarrier && obstacle.Phase != Player.CurrentPhase)
                {
                    PlayerCrashed?.Invoke();
                    RestartAttempt(incrementAttempt: true);
                    return;
                }
            }
        }

        // Проверка пересечения двух прямоугольников с небольшим отступом (4 px) для честных хитбоксов
        private static bool Intersects(Player p, Obstacle o)
        {
            const float padding = 4f;
            return p.X + padding < o.X + o.Width &&
                   p.X + p.Size - padding > o.X &&
                   p.Y + padding < o.Y + o.Height &&
                   p.Y + p.Size - padding > o.Y;
        }
    }
}