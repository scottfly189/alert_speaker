namespace AlertSpeaker.Tts;

public class VoiceOptions
{
    /// <summary>
    /// 默认语音合成模型。系统音色与该模型匹配，不要跨模型混用。
    /// </summary>
    public const string DefaultModel = "qwen-audio-3.1-tts-flash";

    /// <summary>音色。系统音色 Id，默认 longanhuan_v3.1。</summary>
    public string Voice { get; set; } = "longanhuan_v3.1";

    /// <summary>语音合成模型，默认 qwen-audio-3.1-tts-flash。</summary>
    public string Model { get; set; } = DefaultModel;

    /// <summary>指令控制文本。为空则不发送。</summary>
    public string Instructions { get; set; } = "";

    public int? Volume { get; set; }
    public float? Rate { get; set; }
    public float? Pitch { get; set; }
    public int? Seed { get; set; }
}
