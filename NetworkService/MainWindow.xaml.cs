using System.Windows;

namespace NetworkService
{
    public partial class MainWindow : Window
    {
        private int previousTabIndex = 0;

        public MainWindow()
        {
            InitializeComponent();

        }

        private void EntitiesNav_Checked(object sender, RoutedEventArgs e)
        {
            ChangeTab(0);
        }

        private void DisplayNav_Checked(object sender, RoutedEventArgs e)
        {
            ChangeTab(1);
        }

        private void GraphNav_Checked(object sender, RoutedEventArgs e)
        {
            ChangeTab(2);
        }

        private void ChangeTab(int newIndex)
        {
            if (MainTabs == null)
                return;

            previousTabIndex = MainTabs.SelectedIndex;
            MainTabs.SelectedIndex = newIndex;
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (previousTabIndex == 0)
                EntitiesNav.IsChecked = true;
            else if (previousTabIndex == 1)
                DisplayNav.IsChecked = true;
            else if (previousTabIndex == 2)
                GraphNav.IsChecked = true;
        }

        private void UndoButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Undo command clicked.");
        }

        private void ShortcutsButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Shortcuts:\n\nAlt + 1 - Entities\nAlt + 2 - Display\nAlt + 3 - Graph\nEsc - Back\nCtrl + Z - Undo\nF1 - Shortcuts\nCtrl + ` - Terminal");
        }

        private void TerminalButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Terminal command clicked.");
        }

        private void MainTabs_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {

        }

        public class Kurac { 
            public string Name { get; set; } = "Kurac";
        }

    }
}