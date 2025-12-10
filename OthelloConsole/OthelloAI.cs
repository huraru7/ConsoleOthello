using System;
using static OthelloSystem;
using static EvaluationFunction;
using static OthelloDebugLog;

public class OthelloAI
{
    private int _depth = 0;
    public bool _isDebug = false; // デバッグログ出力フラグ
    private int _recursionsCount; //計算回数
    private const int BOARD_SIZE = 8; //定数
    private StreamWriter? sw;

    public (int x, int y) AI(List<(int x, int y)> _validMoves, MainGameData _gamedata)
    {
        (int, int) bestMove = (0, 0);//仮です
        StartLogging();

        switch (_gamedata._AIStrength)
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