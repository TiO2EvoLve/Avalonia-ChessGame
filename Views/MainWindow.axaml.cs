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
        // 在窗口加载完后设置监听
        this.Opened += (_, _) =>
        {
            if (DataContext is MainWindowViewModel vm)
            {
                // 监听 Moves 集合的变化
                vm.MoveRecords.CollectionChanged += Moves_CollectionChanged;
            }
        };
    }
    private void Moves_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // 等待 UI 更新后再滚动
        Dispatcher.UIThread.Post(() =>
        {
            if (MovesScrollViewer != null)
            {
                MovesScrollViewer.ScrollToEnd();
            }
        });
    }
}