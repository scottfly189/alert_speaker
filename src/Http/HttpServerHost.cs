using System.Net;
using System.Security.Cryptography;
using System.Text;
using AlertSpeaker.Config;
using AlertSpeaker.Tts;
using AlertSpeaker.Voices;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;

namespace AlertSpeaker.Http;

/// <summary>
/// 仅监听 127.0.0.1:18765 的本机 HTTP 服务。
/// </summary>
public sealed class HttpServerHost : IDisposable
{
    private readonly AppConfigStore _store;
    private readonly PlaybackQueue _queue;
    private WebApplication? _app;
    private CancellationTokenSource? _cts;

    public HttpServerHost(AppConfigStore store, PlaybackQueue queue)
    {
        _store = store;
        _queue = queue;
    }

    public async Task StartAsync()
    {
        _cts = new CancellationTokenSource();
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            Args = Array.Empty<string>(),
        });

        builder.Logging.ClearProviders();
        builder.WebHost.ConfigureKestrel(options =>
        {
            options.Listen(IPAddress.Loopback, ListenSettings.Port);
        });

        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "alert_speaker",
                Version = "v1",
                Description = "本机喇叭，一条 HTTP，电脑当刻说话。只守 127.0.0.1。AlertToken 为空时调用 /alert 无需带头；若已设置令牌，须带 X-Alert-Token。",
            });
            options.AddSecurityDefinition("AlertToken", new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.ApiKey,
                In = ParameterLocation.Header,
                Name = "X-Alert-Token",
                Description = "本机调用令牌，对应 App.json 的 AlertToken 字段。令牌为空时无需填写。",
            });
            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "AlertToken" },
                    },
                    Array.Empty<string>()
                },
            });
        });

        _app = builder.Build();
        _app.UseSwagger();
        _app.UseSwaggerUI(options =>
        {
            options.DocumentTitle = "alert_speaker";
        });

        _app.MapGet("/alert", (HttpContext context, string? text) => HandleAlert(context, text, null))
            .WithName("AlertGet")
            .WithTags("alert")
            .WithSummary("GET /alert?text=... 提交短文本播报");

        _app.MapPost("/alert", async (HttpContext context) =>
            {
                string? bodyText = null;
                if (context.Request.ContentLength != 0 && context.Request.HasJsonContentType())
                {
                    try
                    {
                        var parsed = await context.Request.ReadFromJsonAsync<AlertRequest>();
                        bodyText = parsed?.Text;
                    }
                    catch
                    {
                        // 没有合法 JSON 时仍可退回 query
                    }
                }

                var queryText = context.Request.Query["text"].ToString();
                return HandleAlert(context, queryText, bodyText);
            })
            .WithName("AlertPost")
            .WithTags("alert")
            .WithSummary("POST /alert，JSON body {text} 或 query text");

        _app.MapGet("/", () => Results.Redirect("/swagger"));

        await _app.StartAsync(_cts.Token);
        AppLog.Info($"HTTP 已启动：{ListenSettings.Origin} （Swagger：{ListenSettings.SwaggerUrl}）");
    }

    private IResult HandleAlert(HttpContext context, string? queryText, string? bodyText)
    {
        var config = _store.Load();
        if (config.HasAlertToken)
        {
            var provided = context.Request.Headers["X-Alert-Token"].ToString();
            if (!TokenEquals(provided, config.AlertToken))
            {
                AppLog.Warn("鉴权失败，已拒绝播报");
                return Results.Json(new { error = "unauthorized" }, statusCode: StatusCodes.Status401Unauthorized);
            }
        }

        var text = !string.IsNullOrWhiteSpace(bodyText) ? bodyText.Trim() : (queryText ?? "").Trim();
        if (string.IsNullOrWhiteSpace(text))
        {
            return Results.Json(new { error = "text is required" }, statusCode: StatusCodes.Status400BadRequest);
        }

        if (!config.HasQwenApiKey)
        {
            return Results.Json(
                new { error = "Qwen API Key is not configured. Set QwenApiKey in App.json." },
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        var voice = VoicePresetList.Contains(config.Voice) ? config.Voice : VoicePresetList.DefaultVoiceId;
        _queue.Enqueue(text, config.QwenApiKey, voice);
        AppLog.Info($"已接单（{context.Request.Method} /alert，字符数 {text.Length}，音色 {voice}）");
        return Results.Json(new { accepted = true, status = "queued" }, statusCode: StatusCodes.Status202Accepted);
    }

    private static bool TokenEquals(string provided, string expected)
    {
        var a = SHA256.HashData(Encoding.UTF8.GetBytes(provided ?? ""));
        var b = SHA256.HashData(Encoding.UTF8.GetBytes(expected ?? ""));
        return CryptographicOperations.FixedTimeEquals(a, b);
    }

    public async Task StopAsync()
    {
        try
        {
            if (_app != null)
                await _app.StopAsync();
        }
        catch (Exception ex)
        {
            AppLog.Error("停止 HTTP 服务时出错", ex);
        }

        _cts?.Cancel();
    }

    public void Dispose()
    {
        try { _cts?.Cancel(); } catch { /* ignore */ }
        try { _app?.DisposeAsync().AsTask().Wait(TimeSpan.FromSeconds(2)); } catch { /* ignore */ }
        _cts?.Dispose();
    }
}
