using System.Text.Json;
using AlertSpeaker.Voices;

namespace AlertSpeaker.Config;

/// <summary>
/// 读写运行目录下的 App.json。首次缺失时生成空令牌和默认音色。
/// </summary>
public sealed class AppConfigStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
    };

    public string FilePath { get; } = Path.Combine(AppContext.BaseDirectory, "App.json");

    public AppConfig LoadOrCreate()
    {
        if (!File.Exists(FilePath))
        {
            var created = new AppConfig
            {
                QwenApiKey = (Environment.GetEnvironmentVariable("QWEN_API_KEY") ?? "").Trim(),
                AlertToken = "",
                Voice = VoicePresetList.DefaultVoiceId,
            };
            Save(created);
            if (created.HasQwenApiKey)
                AppLog.Info("未找到 App.json，已在运行目录生成新文件（AlertToken 默认为空；已从环境变量写入 API Key，日志不记录密钥）。");
            else
                AppLog.Info("未找到 App.json，已在运行目录生成新文件（AlertToken 默认为空，API Key 待填写）。");
            return created;
        }

        var json = File.ReadAllText(FilePath);
        var config = string.IsNullOrWhiteSpace(json)
            ? new AppConfig()
            : JsonSerializer.Deserialize<AppConfig>(json, JsonOptions) ?? new AppConfig();

        var changed = false;
        if (string.IsNullOrWhiteSpace(config.Voice) || !VoicePresetList.Contains(config.Voice))
        {
            config.Voice = VoicePresetList.DefaultVoiceId;
            changed = true;
        }
        if (changed)
            Save(config);
        return config;
    }

    public AppConfig Load()
    {
        if (!File.Exists(FilePath))
            return LoadOrCreate();
        var json = File.ReadAllText(FilePath);
        return string.IsNullOrWhiteSpace(json)
            ? new AppConfig()
            : JsonSerializer.Deserialize<AppConfig>(json, JsonOptions) ?? new AppConfig();
    }

    public void Save(AppConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);
        if (string.IsNullOrWhiteSpace(config.Voice) || !VoicePresetList.Contains(config.Voice))
            config.Voice = VoicePresetList.DefaultVoiceId;
        var json = JsonSerializer.Serialize(config, JsonOptions);
        File.WriteAllText(FilePath, json);
    }
}
