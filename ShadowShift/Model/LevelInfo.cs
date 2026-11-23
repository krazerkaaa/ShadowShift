namespace ShadowShift.Model
{
    // Информация об уровне для отображения в главном меню
    public class LevelInfo
    {
        public string Title { get; }        // Название уровня
        public string Difficulty { get; }   // Уровень сложности
        public string FileName { get; }     // Имя файла в папке Levels

        public LevelInfo(string title, string difficulty, string fileName)
        {
            Title = title;
            Difficulty = difficulty;
            FileName = fileName;
        }
    }
}