using Microsoft.Xaml.Behaviors;
using System.Windows.Controls;
using System.Windows.Input;

namespace MHRUnpack.Behaviors
{
    /// <summary>
    /// 鼠标滚轮
    /// </summary>
    public class MouseWheelBehavior : Behavior<TextBox>
    {
        public int MaxValue { get; set; } = 100;
        public int MinValue { get; set; } = 0;
        public int Scale { get; set; } = 1;
        protected override void OnAttached()
        {
            AssociatedObject.TextChanged += AssociatedObject_TextChanged;
            AssociatedObject.MouseWheel += MouseWheel;
        }

        private void AssociatedObject_TextChanged(object sender, TextChangedEventArgs e)
        {
            var value = MinValue;
            if (int.TryParse(AssociatedObject.Text, out value))
            {
                if(Compare(ref value))
                {
                    AssociatedObject.Text = value.ToString();
                }
            }else
            {
                AssociatedObject.Text = value.ToString();
            }
        }

        private void MouseWheel(object sender, MouseWheelEventArgs e)
        {
            var value = MinValue;
            if (int.TryParse(AssociatedObject.Text, out value))
            {
                if (e.Delta > 0)
                {
                    value += Scale;
                }
                else
                {
                    value -= Scale;
                }
                Compare(ref value);
            }
            AssociatedObject.Text = value.ToString();
        }
        private bool Compare(ref int value)
        {
            if (value < MinValue)
            {
                value = MinValue;
                return true;
            }
            if (value > MaxValue)
            {
                value = MaxValue;
                return true;
            }
            return false;
        }
    }
}
