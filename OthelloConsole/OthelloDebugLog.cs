public static class OthelloDebugLog
{
    private static StreamWriter? sw;
    public static void StartLogging()
    {
        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string logFile = $"AI_log_{timestamp}.txt";
        sw = new StreamWriter(logFile, false); // 新規作成
        Console.WriteLine($"ログファイル生成先: {Path.GetFullPath(logFile)}");
    }

    public static void StopLogging()
    {
        sw?.Flush();
        sw?.Close();
    }

    public static void DebugLog(string message)
    {
        sw?.WriteLine(message);
    }
}