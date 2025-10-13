using Avalonia.Controls;
using Chess.ViewModels;

namespace Chess.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainWindowViewModel();
    }
}