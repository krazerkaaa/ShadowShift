using ShadowShift.View;

namespace ShadowShift
{
    // Точка входа в приложение Windows Forms
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            ApplicationConfiguration.Initialize();
            Application.Run(new MainForm());
        }
    }
}