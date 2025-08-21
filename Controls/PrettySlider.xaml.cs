using Microsoft.Maui.Controls;

namespace MirroRehab.Controls
{
    public partial class PrettySlider : ContentView
    {
        public PrettySlider()
        {
            InitializeComponent();
            SizeChanged += (_, __) => UpdateVisuals();
            NativeSlider.ValueChanged += (_, e) =>
            {
                if (Step > 0)
                {
                    var snapped = Math.Round(e.NewValue / Step) * Step;
                    if (Math.Abs(snapped - e.NewValue) > double.Epsilon)
                        Value = snapped; // триггерит UpdateVisuals через BP
                    else
                        UpdateVisuals();
                }
                else
                {
                    UpdateVisuals();
                }
            };
        }

        // ==== Bindable Properties ====
        public static readonly BindableProperty MinimumProperty =
            BindableProperty.Create(nameof(Minimum), typeof(double), typeof(PrettySlider), 0d);

        public static readonly BindableProperty MaximumProperty =
            BindableProperty.Create(nameof(Maximum), typeof(double), typeof(PrettySlider), 100d);

        public static readonly BindableProperty ValueProperty =
            BindableProperty.Create(nameof(Value), typeof(double), typeof(PrettySlider), 0d,
                propertyChanged: (b, o, n) => ((PrettySlider)b).OnValueChanged());

        public static readonly BindableProperty StepProperty =
            BindableProperty.Create(nameof(Step), typeof(double), typeof(PrettySlider), 0d);

        public static readonly BindableProperty ShowTicksProperty =
            BindableProperty.Create(nameof(ShowTicks), typeof(bool), typeof(PrettySlider), false,
                propertyChanged: (b, o, n) => ((PrettySlider)b).BuildTicks());

        public static readonly BindableProperty TickEveryProperty =
            BindableProperty.Create(nameof(TickEvery), typeof(double), typeof(PrettySlider), 10d,
                propertyChanged: (b, o, n) => ((PrettySlider)b).BuildTicks());

        public static readonly BindableProperty ShowValueBubbleProperty =
            BindableProperty.Create(nameof(ShowValueBubble), typeof(bool), typeof(PrettySlider), true);

        public double Minimum { get => (double)GetValue(MinimumProperty); set => SetValue(MinimumProperty, value); }
        public double Maximum { get => (double)GetValue(MaximumProperty); set => SetValue(MaximumProperty, value); }
        public double Value { get => (double)GetValue(ValueProperty); set => SetValue(ValueProperty, value); }
        public double Step { get => (double)GetValue(StepProperty); set => SetValue(StepProperty, value); }

        public bool ShowTicks { get => (bool)GetValue(ShowTicksProperty); set => SetValue(ShowTicksProperty, value); }
        public double TickEvery { get => (double)GetValue(TickEveryProperty); set => SetValue(TickEveryProperty, value); }

        public bool ShowValueBubble { get => (bool)GetValue(ShowValueBubbleProperty); set => SetValue(ShowValueBubbleProperty, value); }

        // ==== Logic ====
        void OnValueChanged()
        {
            // держим нативный и визуал синхронными
            if (Math.Abs(NativeSlider.Value - Value) > double.Epsilon)
                NativeSlider.Value = Value;
            UpdateVisuals();
        }

        void UpdateVisuals()
        {
            if (TrackHost.Width <= 0) return;

            var range = Math.Max(0.0001, Maximum - Minimum);
            var t = (Value - Minimum) / range;           // 0..1
            t = Math.Clamp(t, 0, 1);

            var trackWidth = TrackHost.Width - 0;        // можно учесть отступы
            var x = t * trackWidth;

            Progress.WidthRequest = x;
            // thumb по центру прогресса:
            Thumb.TranslationX = x - (Thumb.WidthRequest / 2.0);

            if (ShowValueBubble)
            {
                Bubble.TranslationX = x - (Bubble.Width / 2.0);
                BubbleText.Text = Math.Round(Value).ToString();
            }
        }

        void BuildTicks()
        {
            TicksLayer.Children.Clear();
            if (!ShowTicks || TickEvery <= 0 || Maximum <= Minimum) return;

            var total = Maximum - Minimum;
            var count = (int)(total / TickEvery);

            // Колонки = count + 1, ставим тонкие BoxView
            var grid = new Grid { ColumnSpacing = 0 };
            for (int i = 0; i <= count; i++)
                grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));

            for (int i = 0; i <= count; i++)
            {
                var tick = new BoxView
                {
                    WidthRequest = 1,
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Fill,
                    Color = Colors.Silver,
                    Opacity = 0.7
                };
                grid.Children.Add(tick);
                Grid.SetColumn(tick, i);
            }

            TicksLayer.Children.Add(grid);
        }
    }
}
