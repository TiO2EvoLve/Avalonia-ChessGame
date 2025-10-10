using System;
using System.Diagnostics;
using System.Text;

namespace Chess.Manage;

public class StockfishService : IDisposable
{
    private Process _proc;

    public StockfishService(string path = "stockfish.exe")
    {
        _proc = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = path,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8
            }
        };
        _proc.Start();
        _proc.StandardInput.WriteLine("uci");
        _proc.StandardInput.WriteLine("isready");
    }

    public void SetPosition(string moves)
    {
        _proc.StandardInput.WriteLine($"position startpos moves {moves}");
    }

    public string GetBestMove(int depth = 1)
    {
        _proc.StandardInput.WriteLine($"go depth {depth}");
        while (true)
        {
            var line = _proc.StandardOutput.ReadLine();
            if (line == null) break;
            if (line.StartsWith("bestmove"))
            {
                return line.Split(' ')[1];
            }
        }
        return "";
    }

    public void Dispose()
    {
        if (!_proc.HasExited)
        {
            _proc.StandardInput.WriteLine("quit");
            _proc.Kill();
        }
        _proc.Dispose();
    }
}