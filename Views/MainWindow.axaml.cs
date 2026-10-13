using System.Collections.Specialized;
using Avalonia.Controls;
using Avalonia.Threading;
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