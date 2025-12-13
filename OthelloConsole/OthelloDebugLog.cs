using System.Diagnostics;
public static class OthelloDebugLog
{
    public static bool _isDebug = false; // デバッグログ出力フラグ
    private static StreamWriter? sw;
    private static StreamWriter? gameSw;
    private static Stopwatch thinkTimer = new Stopwatch();
    public static void StartAILog()
    {
        thinkTimer.Reset();
        thinkTimer.Start();
        if (!_isDebug) return;

        string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        string logFile = $"Log/AI/AIlog_{timestamp}.txt";
        sw = new StreamWriter(logFile, false); // 新規作成
        Console.WriteLine($"ログファイル生成先: {Path.GetFullPath(logFile)}");
    }

    public static void StartGameLog()
    {
        string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        string logFile = $"Log/Gamelog_{timestamp}.txt";
        gameSw = new StreamWriter(logFile, false); // 新規作成
        Console.WriteLine($"ログファイル生成先: {Path.GetFullPath(logFile)}");
    }

    public static void StopAILog()
    {
        thinkTimer.Stop();
        Console.WriteLine($"AI思考時間: {thinkTimer.ElapsedMilliseconds} ms");
        if (!_isDebug) return;

        sw?.Flush();
        sw?.Close();
    }

    public static void StopGameLog()
    {
        gameSw?.Flush();
        gameSw?.Close();
    }

    public static void DebugLog(string message)
    {
        if (!_isDebug) return;

        sw?.WriteLine(message);
    }

    public static void GameLog(string message)
    {
        gameSw?.WriteLine(message);
    }
}