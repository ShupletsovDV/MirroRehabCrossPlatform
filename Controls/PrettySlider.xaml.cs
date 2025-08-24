using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;

namespace MirroRehab.Controls
{
    public partial class PrettySlider : ContentView
    {
        private readonly List<BoxView> _ticks = new();

        public PrettySlider()
        {
            InitializeComponent();
            SizeChanged += (_, __) => UpdateVisuals();
            NativeSlider.ValueChanged += (_, e) =>
            {
                var newValue = e.NewValue;
                if (Step > 0)
                {
                    var snapped = Math.Round(newValue / Step) * Step;
                    if (Math.Abs(snapped - newValue) > double.Epsilon)
                    {
                        Value = snapped; // Триггерит OnValueChanged
                        return;
                    }
                }
                UpdateVisuals();
            };
            // Начальная сборка тиков
            BuildTicks();
        }

        // ==== Bindable Properties ====
        public static readonly BindableProperty MinimumProperty =
            BindableProperty.Create(nameof(Minimum), typeof(double), typeof(PrettySlider), 0d,
                propertyChanged: (b, o, n) => ((PrettySlider)b).OnMinimumMaximumChanged());

        public static readonly BindableProperty MaximumProperty =
            BindableProperty.Create(nameof(Maximum), typeof(double), typeof(PrettySlider), 100d,
                propertyChanged: (b, o, n) => ((PrettySlider)b).OnMinimumMaximumChanged());

        public static readonly BindableProperty ValueProperty =
            BindableProperty.Create(nameof(Value), typeof(double), typeof(PrettySlider), 0d,
                propertyChanged: (b, o, n) => ((PrettySlider)b).OnValueChanged());

        public static readonly BindableProperty StepProperty =
            BindableProperty.Create(nameof(Step), typeof(double), typeof(PrettySlider), 0d);

        public static readonly BindableProperty ShowTicksProperty =
            BindableProperty.Create(nameof(ShowTicks), typeof(bool), typeof(PrettySlider), true,
                propertyChanged: (b, o, n) => ((PrettySlider)b).BuildTicks());

        public static readonly BindableProperty TickEveryProperty =
            BindableProperty.Create(nameof(TickEvery), typeof(double), typeof(PrettySlider), 10d,
                propertyChanged: (b, o, n) => ((PrettySlider)b).BuildTicks());

        public static readonly BindableProperty ShowValueBubbleProperty =
            BindableProperty.Create(nameof(ShowValueBubble), typeof(bool), typeof(PrettySlider), true);

        public double Minimum
        {
            get => (double)GetValue(MinimumProperty);
            set => SetValue(MinimumProperty, value);
        }
        public double Maximum
        {
            get => (double)GetValue(MaximumProperty);
            set => SetValue(MaximumProperty, value);
        }
        public double Value
        {
            get => (double)GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }
        public double Step
        {
            get => (double)GetValue(StepProperty);
            set => SetValue(StepProperty, value);
        }
        public bool ShowTicks
        {
            get => (bool)GetValue(ShowTicksProperty);
            set => SetValue(ShowTicksProperty, value);
        }
        public double TickEvery
        {
            get => (double)GetValue(TickEveryProperty);
            set => SetValue(TickEveryProperty, value);
        }
        public bool ShowValueBubble
        {
            get => (bool)GetValue(ShowValueBubbleProperty);
            set => SetValue(ShowValueBubbleProperty, value);
        }

        // ==== Логика ====
        private void OnMinimumMaximumChanged()
        {
            BuildTicks();
            UpdateVisuals();
        }

        private void OnValueChanged()
        {
            // Синхронизация нативного слайдера и визуала
            if (Math.Abs(NativeSlider.Value - Value) > double.Epsilon)
                NativeSlider.Value = Value;
            UpdateVisuals();
        }

        private void UpdateVisuals()
        {
            if (TrackHost.Width <= 0) return;

            var range = Math.Max(0.0001, Maximum - Minimum);
            var t = (Value - Minimum) / range; // 0..1
            t = Math.Clamp(t, 0, 1);

            var trackWidth = TrackHost.Width;
            var x = t * trackWidth;

            Progress.WidthRequest = x;

            // Thumb центрируется на конце прогресса
            Thumb.TranslationX = x - (Thumb.WidthRequest / 2.0);

            if (ShowValueBubble)
            {
                Bubble.TranslationX = x - (Bubble.WidthRequest / 2.0);
                BubbleText.Text = Math.Round(Value).ToString();
            }

            UpdateTickPositions();
        }

        private void BuildTicks()
        {
            // Очистка коллекции Children в Grid
            TicksLayer.Children.Clear();
            _ticks.Clear();

            if (!ShowTicks || TickEvery <= 0 || Maximum <= Minimum) return;

            double v = Minimum;
            while (v <= Maximum + double.Epsilon)
            {
                var tick = new BoxView
                {
                    WidthRequest = 1, // ticks-thickness
                    HeightRequest = 5, // ticks-height
                    HorizontalOptions = LayoutOptions.Start,
                    VerticalOptions = LayoutOptions.Center,
                    Color = Color.FromArgb("#AAAAAA"), // ticks-color
                    Opacity = 0.7
                };
                TicksLayer.Children.Add(tick);
                _ticks.Add(tick);
                v += TickEvery;
            }

            UpdateTickPositions();
        }

        private void UpdateTickPositions()
        {
            if (_ticks.Count == 0 || TrackHost.Width <= 0) return;

            var range = Maximum - Minimum;
            var trackWidth = TrackHost.Width;

            double v = Minimum;
            for (int i = 0; i < _ticks.Count; i++)
            {
                var t = (v - Minimum) / range;
                var x = t * trackWidth;
                _ticks[i].TranslationX = x - 0.5; // Центрируем тики шириной 1px
                v += TickEvery;
            }
        }
    }
}