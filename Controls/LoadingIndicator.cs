using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace MirroRehab.Controls;

public class LoadingIndicator : ContentView
{
    readonly BoxView[] _dots = new BoxView[3];
    bool _isAnimating;

    // Настройки
    public double DotSize { get; set; } = 15;
    public double Offset { get; set; } = 10;         // высота прыжка
    public uint PhaseMs { get; set; } = 250;        // 4 * 250 = 1000ms как в CSS
    public Color DotColor { get; set; } = Colors.Blue;

    Grid _layout;

    public LoadingIndicator()
    {
        // Гарантированно даём место по Y
        var totalHeight = DotSize + 2 * Offset;

        _layout = new Grid
        {
            WidthRequest = 60,
            HeightRequest = totalHeight,               // важно!
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,
            RowDefinitions =
            {
                new RowDefinition { Height = new GridLength(totalHeight, GridUnitType.Absolute) } // важно!
            },
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Star },
            }
        };

        for (int i = 0; i < 3; i++)
        {
            var dot = new BoxView
            {
                WidthRequest = DotSize,
                HeightRequest = DotSize,
                CornerRadius = (float)(DotSize / 2),
                BackgroundColor = DotColor,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center,
                TranslationY = 0 // MID
            };
            _dots[i] = dot;
            _layout.Children.Add(dot);
            Grid.SetRow(dot, 0);
            Grid.SetColumn(dot, i);
        }

        // Чтобы не подрезались вылеты (обычно не требуется, но на всякий случай)
        _layout.IsClippedToBounds = false;
        this.IsClippedToBounds = false;

        Content = _layout;
    }

    protected override void OnParentSet()
    {
        base.OnParentSet();
        if (Parent != null && !_isAnimating)
            StartAnimation();
    }

    async void StartAnimation()
    {
        _isAnimating = true;

        // В MAUI: Y вниз = положительно, вверх = отрицательно
        double TOP = -Offset; // вверх
        double MID = 0;
        double BOT = +Offset; // вниз

        // Начальное состояние: как 0% (все MID), сразу перейдём к 20%
        foreach (var d in _dots) d.TranslationY = MID;

        while (_isAnimating)
        {
            // 20%: left=TOP, center=MID, right=MID
            await Task.WhenAll(
                _dots[0].TranslateTo(0, MID, PhaseMs, Easing.Linear),
                _dots[1].TranslateTo(0, MID, PhaseMs, Easing.Linear),
                _dots[2].TranslateTo(0, MID, PhaseMs, Easing.Linear)
            );

            // 40%: left=BOT, center=TOP, right=MID
            await Task.WhenAll(
                _dots[0].TranslateTo(0, BOT, PhaseMs, Easing.Linear), // ВНИЗ
                _dots[1].TranslateTo(0, TOP, PhaseMs, Easing.Linear), // ВВЕРХ
                _dots[2].TranslateTo(0, MID, PhaseMs, Easing.Linear)
            );

            // 60%: left=MID, center=BOT, right=TOP
            await Task.WhenAll(
                _dots[0].TranslateTo(0, MID, PhaseMs, Easing.Linear),
                _dots[1].TranslateTo(0, BOT, PhaseMs, Easing.Linear), // ВНИЗ
                _dots[2].TranslateTo(0, TOP, PhaseMs, Easing.Linear)  // ВВЕРХ
            );

            // 80%: left=MID, center=MID, right=BOT
            await Task.WhenAll(
                _dots[0].TranslateTo(0, MID, PhaseMs, Easing.Linear),
                _dots[1].TranslateTo(0, MID, PhaseMs, Easing.Linear),
                _dots[2].TranslateTo(0, BOT, PhaseMs, Easing.Linear)  // ВНИЗ
            );

            // 100% -> цикл повторяется (снова к 20%)
        }
    }

    protected override void OnBindingContextChanged()
    {
        base.OnBindingContextChanged();
        if (!IsVisible)
            _isAnimating = false;
    }
}
