using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace AlertSpeaker.Tts;

/// <summary>
/// 通义千问 TTS：合成 wav 到本地临时文件。仅负责出文件，不走任何即时通讯发送通路。
/// </summary>
public sealed class QwenTtsClient
{
    private static readonly Uri SynthesizeUri = new("https://maas.qianwenaiapi.com/api/v1/services/audio/tts/SpeechSynthesizer");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly HttpClient _http;

    public QwenTtsClient(HttpClient http)
    {
        _http = http;
        _http.Timeout = TimeSpan.FromSeconds(180);
        if (!_http.DefaultRequestHeaders.UserAgent.Any())
            _http.DefaultRequestHeaders.UserAgent.ParseAdd("alert_speaker");
    }

    public async Task<string> SynthesizeToFileAsync(string apiKey, string message, VoiceOptions? options = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException("未配置通义千问 API Key。请在 App.json 填写 QwenApiKey，或在主窗口保存。");
        if (string.IsNullOrWhiteSpace(message))
            throw new ArgumentException("合成文本不能为空。", nameof(message));

        options ??= new VoiceOptions();
        if (string.IsNullOrWhiteSpace(options.Voice))
            throw new InvalidOperationException("音色(voice)不能为空。");

        var request = new HttpRequestMessage(HttpMethod.Post, SynthesizeUri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        request.Content = new StringContent(BuildInputJson(options, message), Encoding.UTF8, "application/json");

        using var response = await _http.SendAsync(request, cancellationToken).ConfigureAwait(false);
        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            var err = TryParse(body);
            throw new HttpRequestException($"语音合成失败，HTTP {(int)response.StatusCode}，code:{err?.Code}，message:{err?.Message}");
        }

        var result = TryParse(body) ?? throw new HttpRequestException("语音合成成功但响应无法解析。");
        var remoteUrl = result.Output?.Audio?.Url;
        if (string.IsNullOrWhiteSpace(remoteUrl))
            throw new HttpRequestException("语音合成成功但响应中没有音频地址。");

        var dir = Path.Combine(AppContext.BaseDirectory, "Temp");
        Directory.CreateDirectory(dir);
        var localFile = Path.Combine(dir, Guid.NewGuid().ToString("N") + ".wav");

        using var fileResponse = await _http.GetAsync(remoteUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
        fileResponse.EnsureSuccessStatusCode();
        await using (var httpStream = await fileResponse.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false))
        await using (var fileStream = File.Create(localFile))
        {
            await httpStream.CopyToAsync(fileStream, cancellationToken).ConfigureAwait(false);
        }
        return localFile;
    }

    private static string BuildInputJson(VoiceOptions options, string message)
    {
        var payload = new QwenRequestBody
        {
            Model = string.IsNullOrWhiteSpace(options.Model) ? VoiceOptions.DefaultModel : options.Model,
            Input = new QwenRequestInput
            {
                Text = message,
                Voice = options.Voice,
                Format = "wav",
                SampleRate = 24000,
                Instruction = string.IsNullOrWhiteSpace(options.Instructions) ? null : options.Instructions,
                Volume = options.Volume,
                Rate = options.Rate,
                Pitch = options.Pitch,
                Seed = options.Seed,
            },
        };
        return JsonSerializer.Serialize(payload, JsonOptions);
    }

    private static QwenResponseBody? TryParse(string body)
    {
        try
        {
            return JsonSerializer.Deserialize<QwenResponseBody>(body);
        }
        catch
        {
            return null;
        }
    }
}
