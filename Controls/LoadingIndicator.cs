using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using System.Threading.Tasks;

namespace MirroRehab.Controls
{
    public class LoadingIndicator : ContentView
    {
        private readonly ContentView _rotator;
        private bool _isAnimating;

        public LoadingIndicator()
        {
            WidthRequest = 60;
            HeightRequest = 60;

            var container = new Grid
            {
                WidthRequest = 60,
                HeightRequest = 60
            };

            container.RowDefinitions.Add(new RowDefinition());
            container.RowDefinitions.Add(new RowDefinition());
            container.ColumnDefinitions.Add(new ColumnDefinition());
            container.ColumnDefinitions.Add(new ColumnDefinition());

            for (int i = 0; i < 4; i++)
            {
                var circle = new Frame
                {
                    WidthRequest = 16,
                    HeightRequest = 16,
                    CornerRadius = 8,
                    BackgroundColor = Colors.Blue,
                    HasShadow = false,
                    Padding = 0,
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Center
                };

                container.Children.Add(circle);
                Grid.SetRow(circle, i / 2);
                Grid.SetColumn(circle, i % 2);
            }

            _rotator = new ContentView
            {
                WidthRequest = 60,
                HeightRequest = 60,
                Content = container
            };

            Content = _rotator;
        }

        protected override void OnParentSet()
        {
            base.OnParentSet();

            if (Parent != null && !_isAnimating)
                StartAnimation();
        }

        private async void StartAnimation()
        {
            _isAnimating = true;

            while (_isAnimating)
            {
                for (int i = 0; i < 4; i++)
                {
                    await Task.WhenAll(
                        _rotator.RotateTo(_rotator.Rotation + 45, 300, Easing.SinInOut),
                        _rotator.ScaleTo(i % 2 == 0 ? 1.15 : 1.0, 300, Easing.SinInOut)
                    );
                }

                // Легкая пауза между оборотами
                await Task.Delay(100);
            }
        }
    }

}
