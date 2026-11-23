using System.Text.Json.Serialization;

namespace AlertSpeaker.Tts;

internal sealed class QwenRequestBody
{
    [JsonPropertyName("model")]
    public string Model { get; set; } = VoiceOptions.DefaultModel;

    [JsonPropertyName("input")]
    public QwenRequestInput Input { get; set; } = new();
}

internal sealed class QwenRequestInput
{
    [JsonPropertyName("text")]
    public string Text { get; set; } = "";

    [JsonPropertyName("voice")]
    public string Voice { get; set; } = "";

    [JsonPropertyName("format")]
    public string Format { get; set; } = "wav";

    [JsonPropertyName("sample_rate")]
    public int SampleRate { get; set; } = 24000;

    [JsonPropertyName("instruction")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Instruction { get; set; }

    [JsonPropertyName("volume")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Volume { get; set; }

    [JsonPropertyName("rate")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public float? Rate { get; set; }

    [JsonPropertyName("pitch")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public float? Pitch { get; set; }

    [JsonPropertyName("seed")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Seed { get; set; }
}

internal sealed class QwenResponseBody
{
    [JsonPropertyName("status_code")]
    public int StatusCode { get; set; }

    [JsonPropertyName("code")]
    public string? Code { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("output")]
    public QwenOutputInfo? Output { get; set; }
}

internal sealed class QwenOutputInfo
{
    [JsonPropertyName("audio")]
    public QwenAudioInfo? Audio { get; set; }
}

internal sealed class QwenAudioInfo
{
    [JsonPropertyName("url")]
    public string? Url { get; set; }
}
