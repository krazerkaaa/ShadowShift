using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace ShadowShift.View
{
    // Генерирует стартовые PNG-текстуры в папке Assets и загружает их в память
    public sealed class CyberTextureAtlas : IDisposable
    {
        private const string AssetsFolder = "Assets";

        public Bitmap PlayerLight { get; }
        public Bitmap PlayerShadow { get; }
        public Bitmap Spike { get; }
        public Bitmap CeilingSpike { get; }
        public Bitmap BarrierLight { get; }
        public Bitmap BarrierShadow { get; }
        public Bitmap FloorTile { get; }

        public CyberTextureAtlas()
        {
            Directory.CreateDirectory(AssetsFolder);

            PlayerLight = LoadOrGenerate("player_light.png", () => CreateExploitCubeSprite(
                Color.FromArgb(0, 240, 255), Color.FromArgb(12, 26, 36)));

            PlayerShadow = LoadOrGenerate("player_shadow.png", () => CreateExploitCubeSprite(
                Color.FromArgb(185, 65, 255), Color.FromArgb(26, 12, 38)));

            Spike = LoadOrGenerate("spike.png", () => CreateIdsSpikeSprite(inverted: false));
            CeilingSpike = LoadOrGenerate("ceiling_spike.png", () => CreateIdsSpikeSprite(inverted: true));

            BarrierLight = LoadOrGenerate("barrier_light.png", () => CreateFirewallSprite(
                Color.FromArgb(0, 240, 255)));

            BarrierShadow = LoadOrGenerate("barrier_shadow.png", () => CreateFirewallSprite(
                Color.FromArgb(185, 65, 255)));

            FloorTile = LoadOrGenerate("floor_tile.png", CreateServerFloorTile);
        }

        // Загружает PNG с диска или рисует новый шаблон, если файла ещё нет
        private static Bitmap LoadOrGenerate(string fileName, Func<Bitmap> generator)
        {
            var path = Path.Combine(AssetsFolder, fileName);
            if (!File.Exists(path))
            {
                using var generated = generator();
                generated.Save(path, ImageFormat.Png);
            }

            // Читаем через массив байтов, чтобы не блокировать файл на диске при работе игры
            var bytes = File.ReadAllBytes(path);
            using var ms = new MemoryStream(bytes);
            return new Bitmap(ms);
        }

        // Рисует спрайт куба-эксплойта с дорожками платы и пиксельными глазами (44x44)
        private static Bitmap CreateExploitCubeSprite(Color neonColor, Color coreColor)
        {
            var bmp = new Bitmap(44, 44, PixelFormat.Format32bppArgb);
            using var g = Graphics.FromImage(bmp);
            g.Clear(coreColor);

            using var borderPen = new Pen(neonColor, 3);
            using var tracePen = new Pen(Color.FromArgb(150, neonColor), 2);
            using var eyeBrush = new SolidBrush(neonColor);
            using var pupilBrush = new SolidBrush(Color.White);

            g.DrawRectangle(borderPen, 1, 1, 41, 41);
            g.DrawLine(tracePen, 6, 2, 6, 10);
            g.DrawLine(tracePen, 37, 2, 37, 12);
            g.DrawLine(tracePen, 2, 34, 12, 34);
            g.DrawLine(tracePen, 32, 36, 41, 36);

            g.FillRectangle(eyeBrush, 10, 14, 8, 8);
            g.FillRectangle(eyeBrush, 26, 14, 8, 8);
            g.FillRectangle(pupilBrush, 14, 16, 3, 3);
            g.FillRectangle(pupilBrush, 30, 16, 3, 3);
            g.FillRectangle(eyeBrush, 13, 29, 18, 3);

            return bmp;
        }

        // Рисует спрайт шипа (обычного или перевёрнутого потолочного)
        private static Bitmap CreateIdsSpikeSprite(bool inverted)
        {
            var bmp = new Bitmap(40, 40, PixelFormat.Format32bppArgb);
            using var g = Graphics.FromImage(bmp);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);

            var crimson = Color.FromArgb(255, 55, 95);
            var darkCore = Color.FromArgb(45, 10, 20);

            var points = inverted
                ? new[] { new PointF(2, 1), new PointF(20, 38), new PointF(38, 1) }
                : new[] { new PointF(2, 39), new PointF(20, 2), new PointF(38, 39) };

            using var fillBrush = new SolidBrush(darkCore);
            using var edgePen = new Pen(crimson, 2.5f);
            using var markBrush = new SolidBrush(crimson);

            g.FillPolygon(fillBrush, points);
            g.DrawPolygon(edgePen, points);

            if (inverted)
            {
                g.FillRectangle(markBrush, 19, 12, 3, 12);
                g.FillRectangle(markBrush, 19, 6, 3, 3);
            }
            else
            {
                g.FillRectangle(markBrush, 19, 16, 3, 12);
                g.FillRectangle(markBrush, 19, 31, 3, 3);
            }

            return bmp;
        }

        // Рисует текстуру фазового барьера-фаервола (45x180)
        private static Bitmap CreateFirewallSprite(Color neonColor)
        {
            var bmp = new Bitmap(45, 180, PixelFormat.Format32bppArgb);
            using var g = Graphics.FromImage(bmp);
            g.Clear(Color.FromArgb(140, neonColor.R / 4, neonColor.G / 4, neonColor.B / 4));

            using var scanPen = new Pen(Color.FromArgb(110, neonColor), 1);
            using var diagPen = new Pen(Color.FromArgb(70, neonColor), 2);
            using var borderPen = new Pen(neonColor, 2);

            for (var y = 4; y < 180; y += 6)
                g.DrawLine(scanPen, 2, y, 42, y);

            for (var y = -40; y < 180; y += 24)
                g.DrawLine(diagPen, 0, y, 45, y + 30);

            g.DrawRectangle(borderPen, 1, 1, 42, 177);
            return bmp;
        }

        // Рисует повторяющийся тайл пола в виде серверной платы (80x160)
        private static Bitmap CreateServerFloorTile()
        {
            var bmp = new Bitmap(80, 160, PixelFormat.Format32bppArgb);
            using var g = Graphics.FromImage(bmp);
            g.Clear(Color.FromArgb(22, 22, 34));

            using var topGlowPen = new Pen(Color.FromArgb(120, 130, 190), 3);
            using var tracePen = new Pen(Color.FromArgb(42, 45, 68), 2);
            using var nodeBrush = new SolidBrush(Color.FromArgb(65, 70, 105));

            g.DrawLine(topGlowPen, 0, 1, 80, 1);

            g.DrawLine(tracePen, 0, 28, 35, 28);
            g.DrawLine(tracePen, 35, 28, 50, 44);
            g.DrawLine(tracePen, 50, 44, 80, 44);
            g.FillEllipse(nodeBrush, 32, 25, 6, 6);

            g.DrawLine(tracePen, 15, 75, 65, 75);
            g.FillRectangle(nodeBrush, 12, 72, 6, 6);
            g.FillRectangle(nodeBrush, 62, 72, 6, 6);

            g.DrawLine(tracePen, 79, 0, 79, 160);
            return bmp;
        }

        public void Dispose()
        {
            PlayerLight.Dispose();
            PlayerShadow.Dispose();
            Spike.Dispose();
            CeilingSpike.Dispose();
            BarrierLight.Dispose();
            BarrierShadow.Dispose();
            FloorTile.Dispose();
        }
    }
}