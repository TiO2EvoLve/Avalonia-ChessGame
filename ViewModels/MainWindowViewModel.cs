using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Media;
using Chess.Manage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Threading.Tasks;
using Avalonia.Threading;
using Chess.Models;

namespace Chess.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    public ObservableCollection<SquareViewModel> BoardSquares { get; } = new(); //整个格子
    private SquareViewModel? selectedSquare; //选中的格子
    private List<SquareViewModel> highlightedSquares = new(); //高亮的格子
    private readonly StockfishService Engine = new(); //鳕鱼引擎
    private bool isWhiteTurn = true; // true = 白方回合, false = 黑方回合
    [ObservableProperty] private string moves = "";
    public ObservableCollection<ChessMoveRecord> MoveRecords { get; } = new();

    private bool isEngineThinking;

    public MainWindowViewModel()
    {
        InitializeBoard();
    }

    //初始化棋盘格
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
        string[] pieces = ["R", "N", "B", "Q", "K", "B", "N", "R"];
        if (row == 0) return pieces[col]; // 黑方主力
        if (row == 1) return "P"; // 黑方兵
        if (row == 6) return "p"; // 白方兵
        if (row == 7) return pieces[col].ToLower(); // 白方主力
        return "";
    }


    [RelayCommand]
    public void SelectSquare(SquareViewModel square)
    {
        // 如果引擎正在思考或不是当前回合，阻止操作
        if (isEngineThinking) return;

        // 如果当前没有选中棋子，选中棋子并高亮
        if (selectedSquare == null && !string.IsNullOrEmpty(square.PieceSymbol))
        {
            bool isWhitePiece = char.IsLower(square.PieceSymbol[0]); // 小写是白棋

            // 检查是否当前回合
            if (isWhitePiece != isWhiteTurn)
                return; // 不允许选中对方棋子
            selectedSquare = square;
            square.IsHighlighted = true;
            HighlightMoves(square);
        }
        // 如果点击的是可移动的格子，移动棋子
        else if (selectedSquare != null && highlightedSquares.Contains(square))
        {
            // 记录移动之前的坐标
            int fromRow = selectedSquare.Row;
            int fromCol = selectedSquare.Col;
            int toRow = square.Row;
            int toCol = square.Col;

            // 执行移动（立即在 UI 上反映）
            square.PieceSymbol = selectedSquare.PieceSymbol;
            selectedSquare.PieceSymbol = "";

            // 构造 UCI 移动字符串并追加到 Moves
            string uci = ToUci(fromRow, fromCol, toRow, toCol);
            if (string.IsNullOrEmpty(Moves))
                Moves = uci;
            else
                Moves = Moves + " " + uci;

            // 将当前走法列表发送给引擎（保持引擎局面同步）
            Engine.SetPosition(Moves);

            ClearHighlights();
            selectedSquare = null;

            // 切换回合：现在轮到另一方
            isWhiteTurn = !isWhiteTurn;

            // 如果现在是黑方回合，让引擎思考并自动走子（异步）
            if (!isWhiteTurn)
            {
                isEngineThinking = true; // 阻止用户在思考期间再次操作

                Task.Run(() =>
                {
                    try
                    {
                        // 调整深度以平衡速度/质量
                        string best = Engine.GetBestMove(10); // 例如深度 10
                        if (!string.IsNullOrEmpty(best))
                        {
                            // 在 UI 线程上应用引擎走法
                            Dispatcher.UIThread.Post(() => { ApplyUciMove(best); });
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Engine error: " + ex.Message);
                    }
                    finally
                    {
                        // 一定在 UI 线程清除思考标志（防止竞态）
                        Dispatcher.UIThread.Post(() => { isEngineThinking = false; });
                    }
                });
            }
        }
        // 取消选中
        else
        {
            ClearHighlights();
            selectedSquare = null;
        }
    }

    private void ApplyUciMove(string uci)
    {
        if (string.IsNullOrEmpty(uci) || uci.Length < 4) return;

        string fromSq = uci.Substring(0, 2);
        string toSq = uci.Substring(2, 2);
        char promotion = uci.Length >= 5 ? uci[4] : '\0';

        if (!TryParseSquare(fromSq, out int fromRow, out int fromCol)) return;
        if (!TryParseSquare(toSq, out int toRow, out int toCol)) return;

        var fromSquare = BoardSquares.FirstOrDefault(s => s.Row == fromRow && s.Col == fromCol);
        var toSquare = BoardSquares.FirstOrDefault(s => s.Row == toRow && s.Col == toCol);
        if (fromSquare == null || toSquare == null) return;

        string moving = fromSquare.PieceSymbol;
        if (string.IsNullOrEmpty(moving)) return; // nothing to move

        bool movingIsWhite = char.IsLower(moving[0]);

        // 处理 en-passant：如果是兵斜走到空格，应该吃掉目标列上的兵
        bool isPawn = moving.Equals("p", StringComparison.OrdinalIgnoreCase);
        if (isPawn && fromCol != toCol && string.IsNullOrEmpty(toSquare.PieceSymbol))
        {
            // 捕获的兵位于 toCol 的相邻行
            int capturedRow = movingIsWhite ? toRow + 1 : toRow - 1;
            var captured = BoardSquares.FirstOrDefault(s => s.Row == capturedRow && s.Col == toCol);
            if (captured != null && !string.IsNullOrEmpty(captured.PieceSymbol))
            {
                captured.PieceSymbol = "";
            }
        }

        // 处理王车易位：如果移动的是王且水平移动两格
        bool isKing = moving.Equals("k", StringComparison.OrdinalIgnoreCase);
        if (isKing && Math.Abs(toCol - fromCol) == 2)
        {
            // 标准易位：当 toCol==6 (g) 表示短易位，toCol==2 (c) 表示长易位
            int row = fromRow;
            if (toCol == 6)
            {
                // rook 从 h(7) -> f(5)
                var rookFrom = BoardSquares.FirstOrDefault(s => s.Row == row && s.Col == 7);
                var rookTo = BoardSquares.FirstOrDefault(s => s.Row == row && s.Col == 5);
                if (rookFrom != null && rookTo != null)
                {
                    rookTo.PieceSymbol = rookFrom.PieceSymbol;
                    rookFrom.PieceSymbol = "";
                }
            }
            else if (toCol == 2)
            {
                // rook 从 a(0) -> d(3)
                var rookFrom = BoardSquares.FirstOrDefault(s => s.Row == row && s.Col == 0);
                var rookTo = BoardSquares.FirstOrDefault(s => s.Row == row && s.Col == 3);
                if (rookFrom != null && rookTo != null)
                {
                    rookTo.PieceSymbol = rookFrom.PieceSymbol;
                    rookFrom.PieceSymbol = "";
                }
            }
        }

        // 处理升变（promotion），把移动棋子设置为 promotion 类型（大小写根据颜色）
        if (promotion != '\0')
        {
            char promChar = movingIsWhite ? char.ToLower(promotion) : char.ToUpper(promotion);
            moving = promChar.ToString();
        }

        // 执行落子（覆盖目标格）
        toSquare.PieceSymbol = moving;
        fromSquare.PieceSymbol = "";

        // 把引擎的走法追加到 Moves 并让引擎同步最新局面
        if (string.IsNullOrEmpty(Moves)) Moves = uci;
        else Moves = Moves + " " + uci;
        Engine.SetPosition(Moves);

        // 清除高亮并切换回合
        ClearHighlights();
        isWhiteTurn = !isWhiteTurn;
        
        //记录到历史记录
        string[] movesArr = Moves.Split(' ');
        MoveRecords.Clear();
        for (int i = 0; i < movesArr.Length; i += 2)
        {
            MoveRecords.Add(new ChessMoveRecord()
            {
                Round = i / 2 + 1,
                WhiteMove = movesArr[i],
                BlackMove = (i + 1 < movesArr.Length) ? movesArr[i + 1] : ""
            });
        }
    }

    private bool TryParseSquare(string sq, out int row, out int col)
    {
        row = -1;
        col = -1;
        if (sq.Length != 2) return false;
        char file = sq[0];
        char rank = sq[1];
        if (file < 'a' || file > 'h') return false;
        if (rank < '1' || rank > '8') return false;
        col = file - 'a';
        // row 0 对应 rank 8，所以 row = 8 - rank
        row = 8 - (rank - '0');
        return true;
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
                var target = BoardSquares.FirstOrDefault(s =>
                    s.Row == nextRow && s.Col == col && string.IsNullOrEmpty(s.PieceSymbol));
                if (target != null)
                {
                    target.IsHighlighted = true;
                    highlightedSquares.Add(target);
                }

                // 首次可前进两格
                if ((piece == "p" && row == 6) || (piece == "P" && row == 1))
                {
                    int twoRow = row + dir * 2;
                    var target2 = BoardSquares.FirstOrDefault(s =>
                        s.Row == twoRow && s.Col == col && string.IsNullOrEmpty(s.PieceSymbol));
                    if (target2 != null &&
                        string.IsNullOrEmpty(BoardSquares.First(s => s.Row == nextRow && s.Col == col).PieceSymbol))
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
                    var target = BoardSquares.FirstOrDefault(s =>
                        s.Row == nextRow && s.Col == eatCol && !string.IsNullOrEmpty(s.PieceSymbol) &&
                        char.IsLower(s.PieceSymbol[0]) != isWhite);
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
                    var target = BoardSquares.FirstOrDefault(s =>
                        s.Row == nr && s.Col == nc && (string.IsNullOrEmpty(s.PieceSymbol) ||
                                                       char.IsLower(s.PieceSymbol[0]) != isWhite));
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
                    var target = BoardSquares.FirstOrDefault(s =>
                        s.Row == nr && s.Col == nc && (string.IsNullOrEmpty(s.PieceSymbol) ||
                                                       char.IsLower(s.PieceSymbol[0]) != isWhite));
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
            HighlightLineMoves(row, col, isWhite, [(0, 1), (0, -1), (1, 0), (-1, 0)]);
        }
        // 象
        else if (piece == "b" || piece == "B")
        {
            HighlightLineMoves(row, col, isWhite, [(1, 1), (1, -1), (-1, 1), (-1, -1)]);
        }
        // 后
        else if (piece == "q" || piece == "Q")
        {
            HighlightLineMoves(row, col, isWhite,
                [(0, 1), (0, -1), (1, 0), (-1, 0), (1, 1), (1, -1), (-1, 1), (-1, -1)]);
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

    private string ToUci(int fromRow, int fromCol, int toRow, int toCol)
    {
        string from = SquareName(fromRow, fromCol);
        string to = SquareName(toRow, toCol);
        return from + to;
    }

    private string SquareName(int row, int col)
    {
        // col 0->a ... 7->h
        char file = (char)('a' + col);
        // row 0 is rank 8, row 7 is rank 1
        int rank = 8 - row;
        return $"{file}{rank}";
    }

    [RelayCommand]
    private void GetBestMove()
    {
        Engine.SetPosition(Moves);
    }
}