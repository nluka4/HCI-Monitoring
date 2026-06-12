using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace NetworkService.Views
{
    public partial class EntitiesUi : UserControl
    {
        public EntitiesUi()
        {
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(delegate
            {
                FocusSearchTextBox();
            }));
        }

        private void UserControl_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.F)
            {
                FocusSearchTextBox();
                e.Handled = true;
                return;
            }

            if (Keyboard.Modifiers == ModifierKeys.Alt && e.Key == Key.A)
            {
                FocusAddFormFirstField();
                e.Handled = true;
            }
        }

        private void FocusAddButton_Click(object sender, RoutedEventArgs e)
        {
            FocusAddFormFirstField();
        }

        private void AddFormControl_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter)
            {
                return;
            }

            if (Keyboard.Modifiers != ModifierKeys.None)
            {
                return;
            }

            FrameworkElement currentElement = sender as FrameworkElement;

            if (currentElement == null)
            {
                return;
            }

            currentElement.MoveFocus(
                new TraversalRequest(FocusNavigationDirection.Next));

            e.Handled = true;
        }

        private void FocusSearchTextBox()
        {
            SearchTextBox.Focus();
            Keyboard.Focus(SearchTextBox);

            SearchTextBox.CaretIndex = SearchTextBox.Text == null
                ? 0
                : SearchTextBox.Text.Length;
        }

        private void FocusAddFormFirstField()
        {
            AddIdTextBox.Focus();
            Keyboard.Focus(AddIdTextBox);

            AddIdTextBox.SelectAll();
        }
    }
}