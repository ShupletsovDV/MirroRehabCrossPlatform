namespace MirroRehab
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            
        }
        private bool isLightTheme = true;
        private void OnThemeButtonClicked(object sender, EventArgs e)
        {
            isLightTheme = !isLightTheme;

            // Переключение темы
            if (isLightTheme)
            {
                Application.Current.UserAppTheme = AppTheme.Light;
                ThemeButton.Source = "sun_dark.png";
            }
            else
            {
                Application.Current.UserAppTheme = AppTheme.Dark;
                ThemeButton.Source = "sun_light.png";

            }
        }
    }
}
