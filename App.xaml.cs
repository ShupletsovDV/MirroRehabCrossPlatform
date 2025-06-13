using Microsoft.Maui.Controls;
using System.Diagnostics;

namespace MirroRehab
{
    public partial class App : Application
    {
        private readonly AppShell _appShell;

        public App(AppShell appShell)
        {
            InitializeComponent();
            _appShell = appShell;
            MainPage = _appShell;

            // Глобальный обработчик исключений
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                var exception = e.ExceptionObject as Exception;
                Debug.WriteLine($"[Global] Необработанное исключение: {exception?.Message}\n{exception?.StackTrace}");
            };
            TaskScheduler.UnobservedTaskException += (s, e) =>
            {
                Debug.WriteLine($"[Global] Необработанное исключение в задаче: {e.Exception?.Message}\n{e.Exception?.StackTrace}");
                e.SetObserved();
            };
        }
    }
}