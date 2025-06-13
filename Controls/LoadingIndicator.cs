using Microsoft.Maui.Controls;

namespace MirroRehab.Controls
{
    public class LoadingIndicator : ContentView
    {
        public LoadingIndicator()
        {
            var activityIndicator = new ActivityIndicator
            {
                IsRunning = true,
                Color = Colors.Blue,
                Scale = 1.5
            };

            var dotsAnimation = new Label
            {
                Text = ".",
                FontSize = 24,
                HorizontalOptions = LayoutOptions.Center
            };

            // Создаем анимацию
            var animation = new Animation(v =>
            {
                dotsAnimation.Text = new string('.', (int)(v % 3) + 1);
            }, 0, 3);

            // Запускаем анимацию с повторением
            animation.Commit(
                owner: dotsAnimation,
                name: "DotsAnimation",
                rate: 16,
                length: 500,
                easing: Easing.Linear,
                finished: (v, wasCancelled) =>
                {
                    if (!wasCancelled)
                    {
                        dotsAnimation.Text = ".";
                        animation.Commit(dotsAnimation, "DotsAnimation", 16, 500, Easing.Linear);
                    }
                });

            Content = new VerticalStackLayout
            {
                Spacing = 10,
                Children = { activityIndicator, dotsAnimation }
            };
        }
    }
}