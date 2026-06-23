using System.Windows;
using System.Windows.Input;

namespace NetworkService.Infrastructure
{
    public static class EnterKeyNavigation
    {
        public static readonly DependencyProperty MoveFocusOnEnterProperty =
            DependencyProperty.RegisterAttached(
                "MoveFocusOnEnter",
                typeof(bool),
                typeof(EnterKeyNavigation),
                new PropertyMetadata(false, OnMoveFocusOnEnterChanged));

        public static bool GetMoveFocusOnEnter(DependencyObject obj)
        {
            return (bool)obj.GetValue(MoveFocusOnEnterProperty);
        }

        public static void SetMoveFocusOnEnter(DependencyObject obj, bool value)
        {
            obj.SetValue(MoveFocusOnEnterProperty, value);
        }

        private static void OnMoveFocusOnEnterChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            UIElement element = d as UIElement;

            if (element == null)
            {
                return;
            }

            if ((bool)e.NewValue)
            {
                element.PreviewKeyDown += ElementPreviewKeyDown;
            }
            else
            {
                element.PreviewKeyDown -= ElementPreviewKeyDown;
            }
        }

        private static void ElementPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter || Keyboard.Modifiers != ModifierKeys.None)
            {
                return;
            }

            FrameworkElement element = sender as FrameworkElement;

            if (element == null)
            {
                return;
            }

            element.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
            e.Handled = true;
        }
    }
}