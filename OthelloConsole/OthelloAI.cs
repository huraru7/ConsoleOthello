using System;
using System.Collections.Generic;

public class OthelloAI
{
    private int _depth = 0;
    public bool _isDebug = false; // デバッグログ出力フラグ
    private int _recursionsCount;//計算回数
    private const int BOARD_SIZE = 8;//定数
    public EvaluationFunction evalFunc = new EvaluationFunction();
    private StreamWriter? sw;

    public (int x, int y) AI(
        List<(int x, int y)> _validMoves,
        int[,] _board,
        int player,
        AIStrength _AIStrength)
    {
        (int, int) bestMove = (0, 0);//仮です
        StartLogging();

        switch (_AIStrength)
        {
            case AIStrength.nuub: _depth = 2; break;
            case AIStrength.normal: _depth = 4; break;
            case AIStrength.professional: _depth = 6; break;
        }

        DebugLog("=============Ai評価終了=============");
        DebugLog($"総計算手順回数：{_recursionsCount}");

        StopLogging();
        return bestMove;
    }

    private void Negamax(int[,] board, int depth, int player, int alpha, int beta, int level = 0)
    {

    }



    private List<(int x, int y)> GetValidMoves(int[,] board, int player)
    {
        List<(int x, int y)> moves = new();
        int opponent = (player == 1) ? 2 : 1;

        int[] dx = { -1, 1, 0, 0, -1, -1, 1, 1 };
        int[] dy = { 0, 0, -1, 1, -1, 1, -1, 1 };

        for (int y = 0; y < BOARD_SIZE; y++)
        {
            for (int x = 0; x < BOARD_SIZE; x++)
            {
                if (board[y, x] != 0) continue;

                for (int dir = 0; dir < 8; dir++)
                {
                    int nx = x + dx[dir];
                    int ny = y + dy[dir];
                    bool hasOpponent = false;

                    while (nx >= 0 && nx < BOARD_SIZE && ny >= 0 && ny < BOARD_SIZE)
                    {
                        if (board[ny, nx] == opponent)
                        {
                            hasOpponent = true;
                        }
                        else if (board[ny, nx] == player && hasOpponent)
                        {
                            moves.Add((x + 1, y + 1)); // 1-indexで返す
                            goto NextCell;
                        }
                        else break;

                        nx += dx[dir];
                        ny += dy[dir];
                    }
                }
            NextCell:;
            }
        }
        return moves;
    }

    private int[,] AIPlacePiece(int x, int y, int[,] board, int turn)
    {
        int[,] newBoard = new int[8, 8];
        Array.Copy(board, newBoard, board.Length);

        int current = turn;
        int opponent = (turn == 1) ? 2 : 1;

        x -= 1;
        y -= 1;

        newBoard[x, y] = current;

        int[] dx = { -1, 1, 0, 0, -1, -1, 1, 1 };
        int[] dy = { 0, 0, -1, 1, -1, 1, -1, 1 };

        for (int dir = 0; dir < 8; dir++)
        {
            List<(int x, int y)> candidates = new List<(int x, int y)>();
            int nx = x + dx[dir];
            int ny = y + dy[dir];

            while (nx >= 0 && nx < 8 && ny >= 0 && ny < 8)
            {
                if (newBoard[nx, ny] == opponent)
                {
                    candidates.Add((nx, ny));
                }
                else if (newBoard[nx, ny] == current)
                {
                    foreach (var c in candidates)
                        newBoard[c.x, c.y] = current;
                    break;
                }
                else break;

                nx += dx[dir];
                ny += dy[dir];
            }
        }

        return newBoard;
    }

    private int[,] CopyBoard(int[,] board)
    {
        int[,] copy = new int[BOARD_SIZE, BOARD_SIZE];
        Array.Copy(board, copy, board.Length);
        return copy;
    }
    public void StartLogging()
    {
        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string logFile = $"AI_log_{timestamp}.txt";
        sw = new StreamWriter(logFile, false); // 新規作成
        Console.WriteLine($"ログファイル生成先: {Path.GetFullPath(logFile)}");
    }
    public void StopLogging()
    {
        sw?.Flush();
        sw?.Close();
    }

    private void DebugLog(string message)
    {
        sw?.WriteLine(message);
    }
    private string BoardToString(int[,] board)
    {
        int size = board.GetLength(0);
        var sb = new System.Text.StringBuilder();
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                sb.Append(board[y, x] switch { 0 => "-", 1 => "●", 2 => "○", _ => "?" });
                sb.Append(" ");
            }
            sb.AppendLine();
        }
        return sb.ToString();
    }

}

public enum AIStrength
{
    nuub,
    normal,
    professional
}