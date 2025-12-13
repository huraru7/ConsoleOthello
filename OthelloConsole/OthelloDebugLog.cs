using System.Diagnostics;
public static class OthelloDebugLog
{
    public static bool _isDebug = false; // デバッグログ出力フラグ
    private static StreamWriter? sw;
    private static Stopwatch thinkTimer = new Stopwatch();
    public static void StartLogging()
    {
        thinkTimer.Reset();
        thinkTimer.Start();
        if (!_isDebug) return;

        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string logFile = $"Log/AIlog_{timestamp}.txt";
        sw = new StreamWriter(logFile, false); // 新規作成
        Console.WriteLine($"ログファイル生成先: {Path.GetFullPath(logFile)}");
    }

    public static void StopLogging()
    {
        thinkTimer.Stop();
        Console.WriteLine($"AI思考時間: {thinkTimer.ElapsedMilliseconds} ms");
        if (!_isDebug) return;

        sw?.Flush();
        sw?.Close();
    }

    public static void DebugLog(string message)
    {
        if (!_isDebug) return;

        sw?.WriteLine(message);
    }
}