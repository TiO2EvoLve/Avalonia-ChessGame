using Avalonia.Controls;

namespace Chess.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new Chess.ViewModels.MainWindowViewModel();
    }
}