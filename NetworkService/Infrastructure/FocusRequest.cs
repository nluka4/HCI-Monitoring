using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace NetworkService.Infrastructure
{
    public static class FocusRequest
    {
        public static readonly DependencyProperty TokenProperty =
            DependencyProperty.RegisterAttached(
                "Token",
                typeof(int),
                typeof(FocusRequest),
                new PropertyMetadata(0, OnTokenChanged));

        public static int GetToken(DependencyObject obj)
        {
            return (int)obj.GetValue(TokenProperty);
        }

        public static void SetToken(DependencyObject obj, int value)
        {
            obj.SetValue(TokenProperty, value);
        }

        public static readonly DependencyProperty FocusOnLoadedProperty =
            DependencyProperty.RegisterAttached(
                "FocusOnLoaded",
                typeof(bool),
                typeof(FocusRequest),
                new PropertyMetadata(false, OnFocusOnLoadedChanged));

        public static bool GetFocusOnLoaded(DependencyObject obj)
        {
            return (bool)obj.GetValue(FocusOnLoadedProperty);
        }

        public static void SetFocusOnLoaded(DependencyObject obj, bool value)
        {
            obj.SetValue(FocusOnLoadedProperty, value);
        }

        public static readonly DependencyProperty SelectAllProperty =
            DependencyProperty.RegisterAttached(
                "SelectAll",
                typeof(bool),
                typeof(FocusRequest),
                new PropertyMetadata(false));

        public static bool GetSelectAll(DependencyObject obj)
        {
            return (bool)obj.GetValue(SelectAllProperty);
        }

        public static void SetSelectAll(DependencyObject obj, bool value)
        {
            obj.SetValue(SelectAllProperty, value);
        }

        public static readonly DependencyProperty CaretToEndProperty =
            DependencyProperty.RegisterAttached(
                "CaretToEnd",
                typeof(bool),
                typeof(FocusRequest),
                new PropertyMetadata(false));

        public static bool GetCaretToEnd(DependencyObject obj)
        {
            return (bool)obj.GetValue(CaretToEndProperty);
        }

        public static void SetCaretToEnd(DependencyObject obj, bool value)
        {
            obj.SetValue(CaretToEndProperty, value);
        }

        private static void OnTokenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            FrameworkElement element = d as FrameworkElement;

            if (element == null)
            {
                return;
            }

            if ((int)e.NewValue == 0)
            {
                return;
            }

            FocusLater(element);
        }

        private static void OnFocusOnLoadedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            FrameworkElement element = d as FrameworkElement;

            if (element == null)
            {
                return;
            }

            if ((bool)e.NewValue)
            {
                element.Loaded += ElementLoaded;
            }
            else
            {
                element.Loaded -= ElementLoaded;
            }
        }

        private static void ElementLoaded(object sender, RoutedEventArgs e)
        {
            FocusLater(sender as FrameworkElement);
        }

        private static void FocusLater(FrameworkElement element)
        {
            if (element == null)
            {
                return;
            }

            element.Dispatcher.BeginInvoke(new Action(delegate
            {
                element.Focus();
                Keyboard.Focus(element);

                TextBox textBox = element as TextBox;

                if (textBox == null)
                {
                    return;
                }

                if (GetSelectAll(textBox))
                {
                    textBox.SelectAll();
                    return;
                }

                if (GetCaretToEnd(textBox))
                {
                    textBox.CaretIndex = textBox.Text == null ? 0 : textBox.Text.Length;
                }
            }), DispatcherPriority.Input);
        }
    }
}