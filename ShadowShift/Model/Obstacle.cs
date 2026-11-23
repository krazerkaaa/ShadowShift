namespace ShadowShift.Model
{
    // Модель препятствия или портала на трассе
    public class Obstacle
    {
        public float X { get; }                 // Координата левого края
        public float Y { get; }                 // Координата верхнего края
        public float Width { get; }             // Ширина хитбокса
        public float Height { get; }            // Высота хитбокса
        public ObstacleType Type { get; }       // Тип объекта
        public Phase Phase { get; }             // Фаза (нужна для PhaseBarrier)
        public float SpeedMultiplier { get; }   // Множитель скорости (нужен для SpeedPortal)

        public Obstacle(
            float x,
            float y,
            float width,
            float height,
            ObstacleType type,
            Phase phase = Phase.Light,
            float speedMultiplier = 1.0f)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
            Type = type;
            Phase = phase;
            SpeedMultiplier = speedMultiplier;
        }
    }
}