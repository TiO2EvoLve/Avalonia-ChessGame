using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Media;
using Chess.Manage;
using CommunityToolkit.Mvvm.Input;

namespace Chess.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    public ObservableCollection<SquareViewModel> BoardSquares { get; } = new();
    private SquareViewModel? selectedSquare;
    private List<SquareViewModel> highlightedSquares = new();
    private readonly StockfishService Engine = new();

    public MainWindowViewModel()
    {
        InitializeBoard();
    }

    private void InitializeBoard()
    {
        BoardSquares.Clear();
        for (int row = 0; row < 8; row++)
        {
            for (int col = 0; col < 8; col++)
            {
                var square = new SquareViewModel
                {
                    Row = row,
                    Col = col,
                    Background = ((row + col) % 2 == 0) ? Brushes.PapayaWhip : Brushes.YellowGreen,
                    Foreground = Brushes.Black,
                    PieceSymbol = GetInitialPieceSymbol(row, col),
                    IsHighlighted = false,
                    SelectCommand = SelectSquareCommand
                };
                BoardSquares.Add(square);
            }
        }
    }

    private string GetInitialPieceSymbol(int row, int col)
    {
        // 简单国际象棋初始布局
        string[] pieces = { "R", "N", "B", "Q", "K", "B", "N", "R" };
        if (row == 0) return pieces[col]; // 黑方主力
        if (row == 1) return "P";         // 黑方兵
        if (row == 6) return "p";         // 白方兵
        if (row == 7) return pieces[col].ToLower(); // 白方主力
        return "";
    }

    [RelayCommand]
    public void SelectSquare(SquareViewModel square)
    {
        // 如果当前没有选中棋子，选中棋子并高亮
        if (selectedSquare == null && !string.IsNullOrEmpty(square.PieceSymbol))
        {
            selectedSquare = square;
            square.IsHighlighted = true;
            HighlightMoves(square);
        }
        // 如果点击的是可移动的格子，移动棋子
        else if (selectedSquare != null && highlightedSquares.Contains(square))
        {
            square.PieceSymbol = selectedSquare.PieceSymbol;
            selectedSquare.PieceSymbol = "";
            ClearHighlights();
            selectedSquare = null;
        }
        // 取消选中
        else
        {
            ClearHighlights();
            selectedSquare = null;
        }
    }

    private void HighlightMoves(SquareViewModel square)
    {
        highlightedSquares.Clear();
        string piece = square.PieceSymbol;
        int row = square.Row;
        int col = square.Col;
        bool isWhite = char.IsLower(piece[0]);

        // 兵
        if (piece == "p" || piece == "P")
        {
            int dir = (piece == "p") ? -1 : 1;
            int nextRow = row + dir;
            if (nextRow >= 0 && nextRow < 8)
            {
                var target = BoardSquares.FirstOrDefault(s => s.Row == nextRow && s.Col == col && string.IsNullOrEmpty(s.PieceSymbol));
                if (target != null)
                {
                    target.IsHighlighted = true;
                    highlightedSquares.Add(target);
                }
                // 首步可前进两格
                if ((piece == "p" && row == 6) || (piece == "P" && row == 1))
                {
                    int twoRow = row + dir * 2;
                    var target2 = BoardSquares.FirstOrDefault(s => s.Row == twoRow && s.Col == col && string.IsNullOrEmpty(s.PieceSymbol));
                    if (target2 != null && string.IsNullOrEmpty(BoardSquares.First(s => s.Row == nextRow && s.Col == col).PieceSymbol))
                    {
                        target2.IsHighlighted = true;
                        highlightedSquares.Add(target2);
                    }
                }
            }
            // 吃子斜走
            foreach (int dc in new[] { -1, 1 })
            {
                int eatCol = col + dc;
                if (eatCol >= 0 && eatCol < 8 && nextRow >= 0 && nextRow < 8)
                {
                    var target = BoardSquares.FirstOrDefault(s => s.Row == nextRow && s.Col == eatCol && !string.IsNullOrEmpty(s.PieceSymbol) && char.IsLower(s.PieceSymbol[0]) != isWhite);
                    if (target != null)
                    {
                        target.IsHighlighted = true;
                        highlightedSquares.Add(target);
                    }
                }
            }
        }
        // 马
        else if (piece == "n" || piece == "N")
        {
            int[,] moves = { { -2, -1 }, { -2, 1 }, { -1, -2 }, { -1, 2 }, { 1, -2 }, { 1, 2 }, { 2, -1 }, { 2, 1 } };
            for (int i = 0; i < moves.GetLength(0); i++)
            {
                int nr = row + moves[i, 0];
                int nc = col + moves[i, 1];
                if (nr >= 0 && nr < 8 && nc >= 0 && nc < 8)
                {
                    var target = BoardSquares.FirstOrDefault(s => s.Row == nr && s.Col == nc && (string.IsNullOrEmpty(s.PieceSymbol) || char.IsLower(s.PieceSymbol[0]) != isWhite));
                    if (target != null)
                    {
                        target.IsHighlighted = true;
                        highlightedSquares.Add(target);
                    }
                }
            }
        }
        // 王
        else if (piece == "k" || piece == "K")
        {
            for (int dr = -1; dr <= 1; dr++)
                for (int dc = -1; dc <= 1; dc++)
                {
                    if (dr == 0 && dc == 0) continue;
                    int nr = row + dr;
                    int nc = col + dc;
                    if (nr >= 0 && nr < 8 && nc >= 0 && nc < 8)
                    {
                        var target = BoardSquares.FirstOrDefault(s => s.Row == nr && s.Col == nc && (string.IsNullOrEmpty(s.PieceSymbol) || char.IsLower(s.PieceSymbol[0]) != isWhite));
                        if (target != null)
                        {
                            target.IsHighlighted = true;
                            highlightedSquares.Add(target);
                        }
                    }
                }
        }
        // 车
        else if (piece == "r" || piece == "R")
        {
            HighlightLineMoves(row, col, isWhite, new[] { (0, 1), (0, -1), (1, 0), (-1, 0) });
        }
        // 象
        else if (piece == "b" || piece == "B")
        {
            HighlightLineMoves(row, col, isWhite, new[] { (1, 1), (1, -1), (-1, 1), (-1, -1) });
        }
        // 后
        else if (piece == "q" || piece == "Q")
        {
            HighlightLineMoves(row, col, isWhite, new[] { (0, 1), (0, -1), (1, 0), (-1, 0), (1, 1), (1, -1), (-1, 1), (-1, -1) });
        }
    }

    private void HighlightLineMoves(int row, int col, bool isWhite, (int dr, int dc)[] directions)
    {
        foreach (var (dr, dc) in directions)
        {
            int nr = row + dr;
            int nc = col + dc;
            while (nr >= 0 && nr < 8 && nc >= 0 && nc < 8)
            {
                var target = BoardSquares.FirstOrDefault(s => s.Row == nr && s.Col == nc);
                if (target == null) break;
                if (string.IsNullOrEmpty(target.PieceSymbol))
                {
                    target.IsHighlighted = true;
                    highlightedSquares.Add(target);
                }
                else
                {
                    if (char.IsLower(target.PieceSymbol[0]) != isWhite)
                    {
                        target.IsHighlighted = true;
                        highlightedSquares.Add(target);
                    }
                    break;
                }
                nr += dr;
                nc += dc;
            }
        }
    }

    private void ClearHighlights()
    {
        foreach (var sq in BoardSquares)
            sq.IsHighlighted = false;
        highlightedSquares.Clear();
    }

    [RelayCommand]
    private void GetBestMove(string moves)
    {
        Engine.SetPosition(moves);
        Console.WriteLine(Engine.GetBestMove());
    }
}