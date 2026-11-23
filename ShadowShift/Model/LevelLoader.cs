using System.Globalization;

namespace ShadowShift.Model
{
    // Создаёт шаблонные файлы уровней и загружает их в список препятствий
    public static class LevelLoader
    {
        private const string LevelsFolder = "Levels";
        private const string LevelVersionTag = "# v2";

        // Создаёт папку Levels и записывает три стандартных уровня, если их ещё нет
        public static List<LevelInfo> EnsureDefaultLevels()
        {
            Directory.CreateDirectory(LevelsFolder);

            var levels = new List<LevelInfo>
            {
                new LevelInfo("01. FIREWALL BYPASS", "Обучение", "level1.txt"),
                new LevelInfo("02. PACKET INJECTION", "Средний", "level2.txt"),
                new LevelInfo("03. KERNEL PANIC", "Хардкор", "level3.txt")
            };

            CreateOrUpdateTemplate(Path.Combine(LevelsFolder, "level1.txt"), GetLevel1Template());
            CreateOrUpdateTemplate(Path.Combine(LevelsFolder, "level2.txt"), GetLevel2Template());
            CreateOrUpdateTemplate(Path.Combine(LevelsFolder, "level3.txt"), GetLevel3Template());

            return levels;
        }

        // Читает файл уровня с диска
        public static (List<Obstacle> Obstacles, float Length) LoadFromFile(string fileName, float groundY)
        {
            var fullPath = Path.Combine(LevelsFolder, fileName);
            var lines = File.ReadAllLines(fullPath);
            return ParseLines(lines, groundY);
        }

        // Разбирает текстовые команды уровня (LENGTH, SPIKE, CEILING_SPIKE, BARRIER, SPEED)
        public static (List<Obstacle> Obstacles, float Length) ParseLines(IEnumerable<string> lines, float groundY)
        {
            var obstacles = new List<Obstacle>();
            var length = 3500f;

            foreach (var rawLine in lines)
            {
                var line = rawLine.Trim();
                // Пропускаем пустые строки и комментарии
                if (string.IsNullOrEmpty(line) || line.StartsWith("#")) continue;

                var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var command = parts[0].ToUpperInvariant();

                if (command == "LENGTH" && parts.Length >= 2)
                {
                    length = float.Parse(parts[1], CultureInfo.InvariantCulture);
                }
                else if (command == "SPIKE" && parts.Length >= 2)
                {
                    var x = float.Parse(parts[1], CultureInfo.InvariantCulture);
                    obstacles.Add(new Obstacle(x, groundY - 40f, 40f, 40f, ObstacleType.Spike));
                }
                else if (command == "CEILING_SPIKE" && parts.Length >= 3)
                {
                    // clearance — расстояние от пола до острия верхнего шипа
                    var x = float.Parse(parts[1], CultureInfo.InvariantCulture);
                    var clearance = float.Parse(parts[2], CultureInfo.InvariantCulture);
                    var spikeBottomY = groundY - clearance;
                    obstacles.Add(new Obstacle(x, spikeBottomY - 40f, 40f, 40f, ObstacleType.CeilingSpike));
                }
                else if (command == "BARRIER" && parts.Length >= 4)
                {
                    var x = float.Parse(parts[1], CultureInfo.InvariantCulture);
                    var height = float.Parse(parts[2], CultureInfo.InvariantCulture);
                    var phase = Enum.Parse<Phase>(parts[3], ignoreCase: true);
                    obstacles.Add(new Obstacle(x, groundY - height, 45f, height, ObstacleType.PhaseBarrier, phase));
                }
                else if (command == "SPEED" && parts.Length >= 3)
                {
                    var x = float.Parse(parts[1], CultureInfo.InvariantCulture);
                    var multiplier = float.Parse(parts[2], CultureInfo.InvariantCulture);
                    obstacles.Add(new Obstacle(x, groundY - 220f, 36f, 220f, ObstacleType.SpeedPortal, Phase.Light, multiplier));
                }
            }

            return (obstacles, length);
        }

        private static void CreateOrUpdateTemplate(string path, string content)
        {
            if (!File.Exists(path) || !File.ReadAllText(path).StartsWith(LevelVersionTag))
                File.WriteAllText(path, content);
        }

        private static string GetLevel1Template() =>
            $"{LevelVersionTag} — Уровень 1: базовые механики, потолочный ограничитель и легкий разгон\n" +
            "LENGTH 4600\n" +
            "SPIKE 750\n" +
            "BARRIER 1250 150 SHADOW\n" +
            "CEILING_SPIKE 1620 68\n" +
            "BARRIER 1750 150 LIGHT\n" +
            "SPEED 2150 1.25\n" +
            "SPIKE 2600\n" +
            "SPIKE 2640\n" +
            "BARRIER 3150 160 SHADOW\n" +
            "SPEED 3550 1.0\n" +
            "SPIKE 3950\n";

        private static string GetLevel2Template() =>
            $"{LevelVersionTag} — Уровень 2: коридоры из верхних/нижних шипов, слоу-мо и ускорение\n" +
            "LENGTH 5600\n" +
            "SPIKE 700\n" +
            "SPIKE 740\n" +
            "CEILING_SPIKE 1150 66\n" +
            "BARRIER 1230 160 SHADOW\n" +
            "SPEED 1600 0.75\n" +
            "SPIKE 1920\n" +
            "CEILING_SPIKE 2120 66\n" +
            "BARRIER 2180 170 LIGHT\n" +
            "SPIKE 2460\n" +
            "SPEED 2800 1.35\n" +
            "BARRIER 3300 170 SHADOW\n" +
            "SPIKE 3800\n" +
            "SPIKE 3840\n" +
            "BARRIER 4350 180 LIGHT\n" +
            "CEILING_SPIKE 4850 68\n" +
            "BARRIER 4930 160 SHADOW\n";

        private static string GetLevel3Template() =>
            $"{LevelVersionTag} — Уровень 3: режим OVERCLOCK (1.45x), тройные шипы и ловушки сверху\n" +
            "LENGTH 6800\n" +
            "SPEED 520 1.3\n" +
            "BARRIER 950 160 SHADOW\n" +
            "CEILING_SPIKE 1320 65\n" +
            "BARRIER 1380 170 LIGHT\n" +
            "SPIKE 1780\n" +
            "SPIKE 1820\n" +
            "SPIKE 1860\n" +
            "SPEED 2250 0.72\n" +
            "CEILING_SPIKE 2520 65\n" +
            "BARRIER 2570 170 SHADOW\n" +
            "SPIKE 2820\n" +
            "CEILING_SPIKE 3050 65\n" +
            "BARRIER 3100 170 LIGHT\n" +
            "SPEED 3450 1.45\n" +
            "SPIKE 4050\n" +
            "SPIKE 4090\n" +
            "SPIKE 4130\n" +
            "BARRIER 4700 180 SHADOW\n" +
            "CEILING_SPIKE 5250 65\n" +
            "BARRIER 5320 180 LIGHT\n" +
            "SPIKE 5850\n" +
            "SPIKE 5890\n" +
            "BARRIER 6300 180 SHADOW\n";
    }
}