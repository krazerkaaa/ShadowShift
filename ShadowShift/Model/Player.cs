namespace ShadowShift.Model
{
    // Модель игрока (куба-эксплойта): хранит позицию, скорость и активную фазу
    public class Player
    {
        public float X { get; set; }                                    // Горизонтальная координата на трассе
        public float Y { get; set; }                                    // Вертикальная координата (верхний край куба)
        public float Size { get; } = 44f;                               // Размер стороны куба в пикселях
        public float VelocityY { get; set; }                            // Текущая вертикальная скорость
        public Phase CurrentPhase { get; private set; } = Phase.Light;  // Активная полярность
        public bool IsGrounded { get; set; } = true;                    // Стоит ли куб на земле (можно ли прыгнуть)

        // Переключает фазу на противоположную
        public void SwitchPhase()
        {
            CurrentPhase = CurrentPhase == Phase.Light ? Phase.Shadow : Phase.Light;
        }

        // Возвращает игрока на старт уровня в исходное состояние
        public void Reset(float startX, float groundY)
        {
            X = startX;
            Y = groundY - Size;
            VelocityY = 0f;
            CurrentPhase = Phase.Light;
            IsGrounded = true;
        }
    }
}