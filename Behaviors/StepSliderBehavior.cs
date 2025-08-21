using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MirroRehab.Behaviors
{
    public class StepSliderBehavior : Behavior<Slider>
    {
        public double Step { get; set; } = 5d;

        protected override void OnAttachedTo(Slider slider)
        {
            base.OnAttachedTo(slider);
            slider.ValueChanged += OnValueChanged;
        }
        protected override void OnDetachingFrom(Slider slider)
        {
            slider.ValueChanged -= OnValueChanged;
            base.OnDetachingFrom(slider);
        }

        private void OnValueChanged(object? sender, ValueChangedEventArgs e)
        {
            if (sender is not Slider s || Step <= 0) return;
            var snapped = Math.Round(e.NewValue / Step) * Step;
            if (Math.Abs(snapped - e.NewValue) > double.Epsilon)
                s.Value = snapped;
        }
    }
}
