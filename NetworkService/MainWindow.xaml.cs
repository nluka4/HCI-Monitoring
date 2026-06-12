using System;
using System.Windows;
using System.Windows.Input;
using NetworkService.ViewModel;

namespace NetworkService
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainWindowViewModel();
        }

        private MainWindowViewModel ViewModel
        {
            get { return DataContext as MainWindowViewModel; }
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Alt) &&
                e.Key == Key.T)
            {
                OpenTerminalAndFocusInput();
                e.Handled = true;
            }
        }

        private void OpenTerminalAndFocusInput()
        {
            if (ViewModel == null)
            {
                return;
            }

            ViewModel.IsTerminalExpanded = true;

            Dispatcher.BeginInvoke(new Action(delegate
            {
                FocusTerminalInput();
            }));
        }

        private void FocusTerminalInput()
        {
            TerminalInputTextBox.Focus();
            Keyboard.Focus(TerminalInputTextBox);

            TerminalInputTextBox.CaretIndex = TerminalInputTextBox.Text == null
                ? 0
                : TerminalInputTextBox.Text.Length;
        }

        private void TerminalInputTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (ViewModel == null)
            {
                return;
            }

            if (e.Key == Key.Up)
            {
                ViewModel.UsePreviousTerminalCommand();
                FocusTerminalInput();
                e.Handled = true;
                return;
            }

            if (e.Key == Key.Down)
            {
                ViewModel.UseNextTerminalCommand();
                FocusTerminalInput();
                e.Handled = true;
                return;
            }

            if (e.Key == Key.Tab)
            {
                ViewModel.AutocompleteTerminalCommand();
                FocusTerminalInput();
                e.Handled = true;
            }
        }
    }
}