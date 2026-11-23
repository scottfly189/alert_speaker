namespace AlertSpeaker.Config;

/// <summary>
/// 运行目录 App.json 的结构。
/// </summary>
public sealed class AppConfig
{
    /// <summary>通义千问 API Key。</summary>
    public string QwenApiKey { get; set; } = "";

    /// <summary>调用 /alert 时通过请求头 X-Alert-Token 携带的令牌。为空或全空白时不要求鉴权。</summary>
    public string AlertToken { get; set; } = "";

    /// <summary>当前音色 Id。默认 longanhuan_v3.1。</summary>
    public string Voice { get; set; } = "longanhuan_v3.1";

    /// <summary>是否在当前用户登录 Windows 时自动启动。默认 false。缺失按 false。</summary>
    public bool StartWithWindows { get; set; }

    public bool HasQwenApiKey => !string.IsNullOrWhiteSpace(QwenApiKey);

    public bool HasAlertToken => !string.IsNullOrWhiteSpace(AlertToken);
}
