using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using ShadowShift.Model;
using Timer = System.Windows.Forms.Timer;

namespace ShadowShift.View
{
    // Главная форма приложения: отвечает за отрисовку кадров (View) и обработку ввода (Controller)
    public sealed class MainForm : Form
    {
        private readonly GameModel _model = new GameModel();
        private readonly Timer _gameTimer = new Timer();
        private readonly SynthAudioEngine _audio = new SynthAudioEngine();
        private readonly CyberTextureAtlas _textures = new CyberTextureAtlas();

        // Цветовая палитра интерфейса
        private static readonly Color BgColor = Color.FromArgb(14, 14, 22);
        private static readonly Color LightPhaseColor = Color.FromArgb(0, 240, 255);
        private static readonly Color ShadowPhaseColor = Color.FromArgb(185, 65, 255);
        private static readonly Color FastPortalColor = Color.FromArgb(255, 165, 0);
        private static readonly Color SlowPortalColor = Color.FromArgb(65, 145, 255);
        private static readonly Color NormalPortalColor = Color.FromArgb(80, 255, 160);

        public MainForm()
        {
            Text = "Shadow Shift";
            ClientSize = new Size(1280, 720);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            DoubleBuffered = true; // Включаем двойную буферизацию, чтобы экран не мерцал
            BackColor = BgColor;

            // Подписываем аудиодвижок на события игровой модели
            _model.MenuNavigated += _audio.PlayMenuTick;
            _model.Jumped += _audio.PlayJump;
            _model.PhaseSwitched += _audio.PlayPhaseSwitch;
            _model.PlayerCrashed += _audio.PlayCrash;
            _model.SpeedChanged += _audio.PlaySpeedChange;
            _model.LevelCompleted += () =>
            {
                _audio.StopBackgroundBeat();
                _audio.PlayWin();
            };

            _gameTimer.Interval = 16; // ~60 кадров в секунду
            _gameTimer.Tick += OnTimerTick;
            _gameTimer.Start();
        }

        // Маршрутизирует нажатия клавиш в зависимости от текущего экрана
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (e.KeyCode == Keys.M)
            {
                _audio.ToggleMute(_model.State == GameState.Playing);
                return;
            }

            if (_model.State == GameState.MainMenu)
                HandleMenuInput(e.KeyCode);
            else if (_model.State == GameState.Playing)
                HandleGameplayInput(e.KeyCode);
            else if (_model.State == GameState.Completed)
                HandleCompletedInput(e.KeyCode);
        }

        private void HandleMenuInput(Keys key)
        {
            if (key == Keys.Up || key == Keys.W)
                _model.ChangeMenuSelection(-1);
            else if (key == Keys.Down || key == Keys.S)
                _model.ChangeMenuSelection(1);
            else if (key == Keys.Enter || key == Keys.Space)
            {
                _model.StartSelectedLevel();
                _audio.StartBackgroundBeat();
            }
        }

        private void HandleGameplayInput(Keys key)
        {
            if (key == Keys.Space || key == Keys.W || key == Keys.Up)
                _model.Jump();
            else if (key == Keys.ShiftKey)
                _model.SwitchPhase();
            else if (key == Keys.R)
                _model.RestartAttempt(incrementAttempt: true);
            else if (key == Keys.Escape)
            {
                _model.ReturnToMenu();
                _audio.StopBackgroundBeat();
            }
        }

        private void HandleCompletedInput(Keys key)
        {
            if (key == Keys.R || key == Keys.Space)
            {
                _model.RestartAttempt(incrementAttempt: false);
                _audio.StartBackgroundBeat();
            }
            else if (key == Keys.Escape || key == Keys.Enter)
            {
                _model.ReturnToMenu();
            }
        }

        // Обновляет физику модели по таймеру и запрашивает перерисовку окна
        private void OnTimerTick(object? sender, EventArgs e)
        {
            _model.Update(0.016f);
            Invalidate();
        }

        // Главный метод отрисовки сцены
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            // Сохраняем чёткость пиксель-арт текстур без размытия
            g.InterpolationMode = InterpolationMode.NearestNeighbor;
            g.PixelOffsetMode = PixelOffsetMode.Half;

            if (_model.State == GameState.MainMenu)
            {
                DrawMainMenu(g);
                return;
            }

            // Камера следует за игроком, удерживая его в левой части экрана
            var cameraOffsetX = _model.Player.X - 220f;
            DrawBackgroundGrid(g, cameraOffsetX);
            DrawFloor(g, cameraOffsetX);
            DrawFinishLine(g, cameraOffsetX);
            DrawObstacles(g, cameraOffsetX);
            DrawPlayer(g, cameraOffsetX);
            DrawHud(g);

            if (_model.State == GameState.Completed)
                DrawCompletedOverlay(g);
        }

        private void DrawMainMenu(Graphics g)
        {
            DrawBackgroundGrid(g, 0);
            DrawFloor(g, 0);

            using var titleFont = new Font("Consolas", 48, FontStyle.Bold);
            using var subFont = new Font("Consolas", 14, FontStyle.Regular);
            using var itemFont = new Font("Consolas", 20, FontStyle.Bold);

            using var shadowBrush = new SolidBrush(ShadowPhaseColor);
            using var lightBrush = new SolidBrush(LightPhaseColor);

            g.DrawString("SHADOW SHIFT", titleFont, shadowBrush, 390, 75);
            g.DrawString("ВЫБЕРИТЕ УРОВЕНЬ ДЛЯ ПРОРЫВА СЕТИ", subFont, Brushes.LightGray, 435, 155);

            g.DrawImage(_textures.PlayerLight, 340, 92, 52, 52);
            g.DrawImage(_textures.PlayerShadow, 885, 92, 52, 52);

            for (var i = 0; i < _model.AvailableLevels.Count; i++)
            {
                var level = _model.AvailableLevels[i];
                var isSelected = i == _model.SelectedLevelIndex;
                var y = 230 + i * 82;

                var cardRect = new Rectangle(340, y, 600, 64);
                using var fillBrush = new SolidBrush(isSelected
                    ? Color.FromArgb(55, ShadowPhaseColor)
                    : Color.FromArgb(24, 24, 38));
                using var borderPen = new Pen(isSelected ? LightPhaseColor : Color.FromArgb(60, 60, 90), 2);

                g.FillRectangle(fillBrush, cardRect);
                g.DrawRectangle(borderPen, cardRect);

                var prefix = isSelected ? "> " : "  ";
                var textBrush = isSelected ? lightBrush : Brushes.WhiteSmoke;
                g.DrawString($"{prefix}{level.Title}", itemFont, textBrush, 360, y + 16);
                g.DrawString($"[{level.Difficulty}]", subFont, shadowBrush, 780, y + 22);
            }

            var soundStatus = _audio.IsMuted ? "ВЫКЛ" : "ВКЛ";
            g.DrawString($"[W/S или Стрелки] Выбор   [ENTER / SPACE] Старт   [M] Звук: {soundStatus}", subFont, Brushes.Gray, 300, 515);
        }

        private static void DrawBackgroundGrid(Graphics g, float cameraX)
        {
            using var gridPen = new Pen(Color.FromArgb(24, 24, 38), 1);
            var step = 80;
            // Фоновая сетка движется медленнее уровня для создания глубины (параллакс)
            var offset = -(int)(cameraX * 0.4f) % step;

            for (var x = offset; x < 1280; x += step)
                g.DrawLine(gridPen, x, 0, x, (int)GameModel.GroundY);
        }

        private void DrawFloor(Graphics g, float cameraX)
        {
            var tileW = _textures.FloorTile.Width;
            var tileH = _textures.FloorTile.Height;
            var offset = -(int)(cameraX % tileW);
            if (offset > 0) offset -= tileW;

            for (var x = offset; x < 1280; x += tileW)
                g.DrawImage(_textures.FloorTile, x, (int)GameModel.GroundY, tileW, tileH);
        }

        private void DrawFinishLine(Graphics g, float cameraX)
        {
            var screenX = _model.LevelLength - cameraX;
            if (screenX < -50 || screenX > 1330) return;

            using var finishPen = new Pen(Color.FromArgb(80, 255, 160), 4) { DashStyle = DashStyle.Dash };
            g.DrawLine(finishPen, screenX, 120, screenX, GameModel.GroundY);
        }

        private void DrawObstacles(Graphics g, float cameraX)
        {
            foreach (var obs in _model.Obstacles)
            {
                var screenX = obs.X - cameraX;
                // Не рисуем объекты, которые ещё не вошли в экран или уже ушли за него
                if (screenX + obs.Width < -50 || screenX > 1330) continue;

                if (obs.Type == ObstacleType.Spike)
                    g.DrawImage(_textures.Spike, screenX, obs.Y, obs.Width, obs.Height);
                else if (obs.Type == ObstacleType.CeilingSpike)
                    DrawCeilingSpike(g, screenX, obs);
                else if (obs.Type == ObstacleType.SpeedPortal)
                    DrawSpeedPortal(g, screenX, obs);
                else
                    DrawPhaseBarrier(g, screenX, obs);
            }
        }

        private void DrawCeilingSpike(Graphics g, float screenX, Obstacle obs)
        {
            var centerX = screenX + obs.Width / 2f;
            using var beamPen = new Pen(Color.FromArgb(110, 255, 55, 95), 4);
            using var coreBeamPen = new Pen(Color.FromArgb(220, 255, 130, 160), 1.5f);

            // Лазерный подвес от верхнего края окна до основания шипа
            g.DrawLine(beamPen, centerX, 0, centerX, obs.Y + 2);
            g.DrawLine(coreBeamPen, centerX, 0, centerX, obs.Y + 2);
            g.DrawImage(_textures.CeilingSpike, screenX, obs.Y, obs.Width, obs.Height);
        }

        private static void DrawSpeedPortal(Graphics g, float screenX, Obstacle obs)
        {
            var portalColor = obs.SpeedMultiplier > 1.05f
                ? FastPortalColor
                : obs.SpeedMultiplier < 0.95f ? SlowPortalColor : NormalPortalColor;

            var symbol = obs.SpeedMultiplier > 1.05f ? ">>" : obs.SpeedMultiplier < 0.95f ? "<<" : "==";

            using var fillBrush = new SolidBrush(Color.FromArgb(40, portalColor));
            using var borderPen = new Pen(portalColor, 2) { DashStyle = DashStyle.Dot };
            using var textBrush = new SolidBrush(portalColor);
            using var font = new Font("Consolas", 12, FontStyle.Bold);

            g.FillRectangle(fillBrush, screenX, obs.Y, obs.Width, obs.Height);
            g.DrawRectangle(borderPen, screenX, obs.Y, obs.Width, obs.Height);

            for (var y = obs.Y + 25; y < obs.Y + obs.Height - 20; y += 45)
                g.DrawString(symbol, font, textBrush, screenX + 6, y);
        }

        private void DrawPhaseBarrier(Graphics g, float screenX, Obstacle obs)
        {
            var texture = obs.Phase == Phase.Light ? _textures.BarrierLight : _textures.BarrierShadow;
            var isPassable = obs.Phase == _model.Player.CurrentPhase;
            var destRect = new Rectangle((int)screenX, (int)obs.Y, (int)obs.Width, (int)obs.Height);

            if (!isPassable)
            {
                g.DrawImage(texture, destRect);
                return;
            }

            // Проницаемый барьер своей фазы делаем полупрозрачным (25%)
            var matrix = new ColorMatrix { Matrix33 = 0.25f };
            using var attributes = new ImageAttributes();
            attributes.SetColorMatrix(matrix, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);

            g.DrawImage(texture, destRect, 0, 0, texture.Width, texture.Height, GraphicsUnit.Pixel, attributes);
        }

        private void DrawPlayer(Graphics g, float cameraX)
        {
            var p = _model.Player;
            var screenX = p.X - cameraX;
            var sprite = p.CurrentPhase == Phase.Light ? _textures.PlayerLight : _textures.PlayerShadow;

            // Лёгкий наклон куба в воздухе в зависимости от вертикальной скорости
            var state = g.Save();
            var centerX = screenX + p.Size / 2f;
            var centerY = p.Y + p.Size / 2f;
            var tiltAngle = p.IsGrounded ? 0f : Math.Clamp(p.VelocityY * 0.025f, -18f, 18f);

            g.TranslateTransform(centerX, centerY);
            g.RotateTransform(tiltAngle);
            g.DrawImage(sprite, -p.Size / 2f, -p.Size / 2f, p.Size, p.Size);
            g.Restore(state);
        }

        private void DrawHud(Graphics g)
        {
            using var font = new Font("Consolas", 14, FontStyle.Bold);
            var phaseName = _model.Player.CurrentPhase == Phase.Light ? "СВЕТЛАЯ (CYAN)" : "ТЕНЕВАЯ (PURPLE)";
            var phaseColor = _model.Player.CurrentPhase == Phase.Light ? LightPhaseColor : ShadowPhaseColor;
            var soundStatus = _audio.IsMuted ? "ВЫКЛ" : "ВКЛ";

            var speedTag = _model.CurrentSpeedMultiplier > 1.05f
                ? " [OVERCLOCK]"
                : _model.CurrentSpeedMultiplier < 0.95f ? " [SLOW-MO]" : "";

            var speedColor = _model.CurrentSpeedMultiplier > 1.05f
                ? FastPortalColor
                : _model.CurrentSpeedMultiplier < 0.95f ? SlowPortalColor : Color.WhiteSmoke;

            using var phaseBrush = new SolidBrush(phaseColor);
            using var speedBrush = new SolidBrush(speedColor);

            g.DrawString($"УРОВЕНЬ: {_model.CurrentLevel.Title}", font, Brushes.LightGray, 24, 20);
            g.DrawString($"ФАЗА: {phaseName}", font, phaseBrush, 24, 48);
            g.DrawString($"ПРОГРЕСС: {_model.ProgressPercent}%  |  ПОПЫТКА: {_model.Attempt}", font, Brushes.WhiteSmoke, 24, 76);
            g.DrawString($"СКОРОСТЬ: {_model.CurrentSpeedMultiplier:F2}x{speedTag}", font, speedBrush, 24, 104);
            g.DrawString($"[SPACE] Прыжок  [SHIFT] Фаза  [R] Рестарт  [M] Звук: {soundStatus}  [ESC] Меню", font, Brushes.Gray, 24, 675);
        }

        private void DrawCompletedOverlay(Graphics g)
        {
            using var dimBrush = new SolidBrush(Color.FromArgb(190, 10, 10, 18));
            g.FillRectangle(dimBrush, 0, 0, 1280, 720);

            using var titleFont = new Font("Consolas", 32, FontStyle.Bold);
            using var statsFont = new Font("Consolas", 18, FontStyle.Bold);
            using var lightBrush = new SolidBrush(LightPhaseColor);

            g.DrawString("СИСТЕМА ВЗЛОМАНА!", titleFont, lightBrush, 410, 230);
            g.DrawString($"Потрачено попыток: {_model.Attempt}", statsFont, Brushes.WhiteSmoke, 470, 310);
            g.DrawString($"Время забега: {_model.ElapsedTime:F2} сек.", statsFont, Brushes.WhiteSmoke, 470, 350);
            g.DrawString("[R] Пройти заново   [ENTER / ESC] В главное меню", statsFont, Brushes.Gray, 330, 430);
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _textures.Dispose();
            _audio.Dispose();
            base.OnFormClosed(e);
        }
    }
}