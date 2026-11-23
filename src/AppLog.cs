namespace AlertSpeaker;

/// <summary>
/// 简单日志。不会写入 API Key 或鉴权令牌。
/// </summary>
public static class AppLog
{
    private static readonly object Gate = new();
    private static Action<string>? _sink;

    public static void SetSink(Action<string>? sink) => _sink = sink;

    public static void Info(string message) => Write("INFO", message, null);

    public static void Warn(string message) => Write("WARN", message, null);

    public static void Error(string message, Exception? ex = null) => Write("ERROR", message, ex);

    private static void Write(string level, string message, Exception? ex)
    {
        var line = $"{DateTime.Now:HH:mm:ss} [{level}] {Redact(message)}";
        if (ex != null)
            line += " " + Redact(ex.Message);

        lock (Gate)
        {
            try
            {
                var dir = Path.Combine(AppContext.BaseDirectory, "logs");
                Directory.CreateDirectory(dir);
                File.AppendAllText(Path.Combine(dir, "alert_speaker.log"), line + Environment.NewLine);
            }
            catch
            {
                // 写文件失败不影响播报
            }
        }

        try { _sink?.Invoke(line); } catch { /* 界面已关闭时忽略 */ }
    }

    internal static string Redact(string? text)
    {
        if (string.IsNullOrEmpty(text))
            return "";
        // 避免把密钥或令牌打进日志：去掉常见查询/头部形态
        var t = text;
        t = System.Text.RegularExpressions.Regex.Replace(t, @"(?i)(api[_-]?key|alert[_-]?token|authorization|bearer|x-alert-token)\s*[=:]\s*[^,\s;]+", "$1=***");
        return t;
    }
}
