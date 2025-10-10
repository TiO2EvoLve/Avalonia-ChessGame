using CommunityToolkit.Mvvm.ComponentModel;
using Avalonia.Media;
using System.Windows.Input;

namespace Chess.ViewModels
{
    public partial class SquareViewModel : ObservableObject
    {
        [ObservableProperty]
        private string pieceSymbol;

        [ObservableProperty]
        private IBrush background;

        [ObservableProperty]
        private IBrush foreground;

        [ObservableProperty]
        private bool isHighlighted;

        public int Row { get; set; }
        public int Col { get; set; }

        public ICommand SelectCommand { get; set; }
    }
}
