namespace ShadowShift.Model
{
    // Текущий экран / состояние игрового цикла
    public enum GameState
    {
        MainMenu,   // Главное меню с выбором уровня
        Playing,    // Активный забег
        Completed   // Уровень пройден (экран статистики)
    }
}