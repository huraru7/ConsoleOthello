using System.Diagnostics;

public enum LogLevel
{
    Info,   // 常にコンソール表示（デバッグOFFでも）
    Debug,  // デバッグON時にコンソール+ファイル出力
    Detail, // デバッグON かつ Detail レベル時のみ（大量ログ向け）
}

public static class OthelloDebugLog
{
    public static bool _isDebug = false;
    public static LogLevel CurrentLevel = LogLevel.Debug;
    /// <summary>学習モード中はtrueにしてコンソール出力を全て抑制する</summary>
    public static bool SuppressOutput = false;

    private static StreamWriter? sw;
    private static StreamWriter? gameSw;
    private static Stopwatch thinkTimer = new Stopwatch();
    private static string _gameLogDir = "Log";

    public static void StartAILog(int turnCount)
    {
        thinkTimer.Reset();
        thinkTimer.Start();
        if (!_isDebug) return;

        string aiLogDir = Path.Combine(_gameLogDir, "AILog");
        Directory.CreateDirectory(aiLogDir);
        string logFile = Path.Combine(aiLogDir, $"AILog-{turnCount}.txt");
        sw = new StreamWriter(logFile, false);
        InfoLog($"AIログ出力先: {Path.GetFullPath(logFile)}");
    }

    public static void StartGameLog()
    {
        string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        _gameLogDir = Path.Combine("Log", timestamp);
        Directory.CreateDirectory(_gameLogDir);
        string logFile = Path.Combine(_gameLogDir, "Gamelog.txt");
        gameSw = new StreamWriter(logFile, false);
        Console.WriteLine($"ゲームログ出力先: {Path.GetFullPath(logFile)}");
    }

    public static void StopAILog()
    {
        thinkTimer.Stop();
        if (!SuppressOutput)
            Console.WriteLine($"AI思考時間: {thinkTimer.ElapsedMilliseconds} ms");
        sw?.WriteLine($"AI思考時間: {thinkTimer.ElapsedMilliseconds} ms");
        if (!_isDebug) return;

        sw?.Flush();
        sw?.Close();
        sw = null;
    }

    public static void StopGameLog()
    {
        gameSw?.Flush();
        gameSw?.Close();
        gameSw = null;
    }

    /// <summary>
    /// Info: デバッグOFFでもコンソールに表示される重要情報
    /// </summary>
    public static void InfoLog(string message)
    {
        if (SuppressOutput) return;
        Console.WriteLine($"[INFO] {message}");
        sw?.WriteLine($"[INFO] {message}");
    }

    /// <summary>
    /// Debug: デバッグON時にコンソール+ファイルへ出力
    /// </summary>
    public static void DebugLog(string message)
    {
        if (!_isDebug) return;
        if (CurrentLevel > LogLevel.Debug) return;

        Console.WriteLine($"[DEBUG] {message}");
        sw?.WriteLine($"[DEBUG] {message}");
    }

    /// <summary>
    /// Detail: デバッグON かつ Detail レベル時のみ出力（大量ログ向け）
    /// </summary>
    public static void DetailLog(string message)
    {
        if (!_isDebug) return;
        if (CurrentLevel != LogLevel.Detail) return;

        Console.WriteLine($"[DETAIL] {message}");
        sw?.WriteLine($"[DETAIL] {message}");
    }

    /// <summary>
    /// ゲームログファイルに書き込む（常に記録）
    /// </summary>
    public static void GameLog(string message)
    {
        gameSw?.WriteLine(message);
    }
}
