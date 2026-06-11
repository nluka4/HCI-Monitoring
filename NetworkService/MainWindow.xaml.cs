using System.Windows;
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
    }
}