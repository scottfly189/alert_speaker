namespace AlertSpeaker;

/// <summary>
/// 固定监听地址。不要改端口或改成非回环地址。
/// </summary>
public static class ListenSettings
{
    public const string Address = "127.0.0.1";
    public const int Port = 18765;

    public static string Origin => $"http://{Address}:{Port}";
    public static string SwaggerUrl => $"{Origin}/swagger";
    public static string AlertUrl => $"{Origin}/alert";
}
