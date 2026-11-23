using System.Diagnostics;
using AlertSpeaker.Config;
using AlertSpeaker.Http;
using AlertSpeaker.Tts;
using AlertSpeaker.Voices;

namespace AlertSpeaker;

public sealed class MainForm : AntdUI.Window
{
    private readonly AppConfigStore _store = new();
    private readonly QwenTtsClient _tts;
    private readonly PlaybackQueue _queue;
    private readonly HttpServerHost _http;
    private readonly HttpClient _httpClient = new();

    private readonly NotifyIcon _tray = new();
    private readonly ContextMenuStrip _trayMenu = new();
    private readonly Icon _appIcon;
    private readonly Image _headerIcon;
    private readonly Image _eyeOpen;
    private readonly Image _eyeClose;
    private readonly AntdUI.Input _txtApiKey = new();
    private readonly AntdUI.Input _txtToken = new();
    private readonly AntdUI.Select _cmbVoice = new();
    private readonly AntdUI.Input _txtTest = new();
    private readonly AntdUI.Input _txtLog = new();
    private readonly AntdUI.Button _lnkSwagger = new();
    private readonly AntdUI.Button _btnPlay = new();
    private readonly AntdUI.Checkbox _chkStartWithWindows = new();
    private bool _exiting;
    private bool _loading;

    public MainForm()
    {
        _httpClient.Timeout = TimeSpan.FromSeconds(180);
        _tts = new QwenTtsClient(_httpClient);
        _queue = new PlaybackQueue(_tts);
        _http = new HttpServerHost(_store, _queue);

        _appIcon = AppAssets.LoadSpeakIcon();
        _headerIcon = AppAssets.LoadSpeakImage(20);
        _eyeOpen = AppAssets.LoadEyeOpen(18);
        _eyeClose = AppAssets.LoadEyeClose(18);

        Text = "alert_speaker";
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Microsoft YaHei UI", 9F);
        ClientSize = new Size(660, 680);
        MinimumSize = new Size(660, 680);
        Resizable = false;
        MaximizeBox = false;
        Icon = _appIcon;
        Mode = AntdUI.TAMode.Light;

        BuildUi();
        BuildTray();

        Load += MainForm_Load;
        FormClosing += MainForm_FormClosing;
        FormClosed += MainForm_FormClosed;
    }

    private void BuildUi()
    {
        var header = new AntdUI.PageHeader
        {
            Dock = DockStyle.Top,
            Height = 44,
            Text = "alert_speaker",
            ShowButton = true,
            ShowIcon = true,
            Icon = _headerIcon,
            MaximizeBox = false,
            MinimizeBox = true,
            FullBox = false,
            EnableDoubleClickMaximize = false,
            DividerShow = true,
            DragMove = true,
        };

        var body = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
        };

        var y = 16;
        var headline = new AntdUI.Label
        {
            Location = new Point(16, y),
            Size = new Size(628, 28),
            Font = new Font("Microsoft YaHei UI", 11F, FontStyle.Bold),
            Text = "本机喇叭，一条 HTTP，电脑当刻说话。",
        };
        y += 32;
        var sub = new AntdUI.Label
        {
            Location = new Point(16, y),
            Size = new Size(628, 36),
            Text = "预警出声，不用盯着聊天窗口。只守 127.0.0.1，带鉴权，喇叭不出局域网。",
        };
        y += 40;
        var listen = new AntdUI.Label
        {
            Location = new Point(16, y),
            Size = new Size(200, 22),
            Text = "监听：" + ListenSettings.Origin,
        };
        _lnkSwagger.Location = new Point(230, y - 4);
        _lnkSwagger.Size = new Size(400, 28);
        _lnkSwagger.Type = AntdUI.TTypeMini.Default;
        _lnkSwagger.Ghost = true;
        _lnkSwagger.Text = "Swagger：" + ListenSettings.SwaggerUrl;
        _lnkSwagger.Click += (_, _) => OpenUrl(ListenSettings.SwaggerUrl);
        y += 36;

        body.Controls.Add(MakeLabel("通义千问 API Key", 16, y));
        SetupSecretInput(_txtApiKey, 150, y - 2, "通义千问 API Key");
        y += 40;

        body.Controls.Add(MakeLabel("鉴权令牌", 16, y));
        SetupSecretInput(_txtToken, 150, y - 2, "鉴权令牌");
        y += 40;

        body.Controls.Add(MakeLabel("音色", 16, y));
        _cmbVoice.Location = new Point(150, y - 2);
        _cmbVoice.Size = new Size(494, 32);
        _cmbVoice.ListAutoWidth = true;
        _cmbVoice.MaxCount = 12;
        _cmbVoice.WheelModifyEnabled = false;
        _cmbVoice.AccessibleName = "音色";
        _cmbVoice.SelectedIndexChanged += CmbVoice_SelectedIndexChanged;
        y += 40;

        body.Controls.Add(MakeLabel("试听文本", 16, y));
        y += 22;
        _txtTest.Location = new Point(16, y);
        _txtTest.Size = new Size(628, 70);
        _txtTest.Multiline = true;
        _txtTest.AutoScroll = true;
        _txtTest.Text = "这是一条本机预警，请立刻查看。";
        _txtTest.AccessibleName = "试听文本";
        y += 80;

        var btnSave = new AntdUI.Button
        {
            Text = "保存配置",
            Type = AntdUI.TTypeMini.Primary,
            Location = new Point(16, y),
            Size = new Size(110, 32),
        };
        btnSave.Click += (_, _) => SaveFromForm(announce: true);
        _btnPlay.Text = "试听";
        _btnPlay.Type = AntdUI.TTypeMini.Default;
        _btnPlay.Location = new Point(136, y);
        _btnPlay.Size = new Size(110, 32);
        _btnPlay.AccessibleName = "试听";
        _btnPlay.Click += BtnPlay_Click;
        var btnAbout = new AntdUI.Button
        {
            Text = "关于",
            Type = AntdUI.TTypeMini.Default,
            Location = new Point(256, y),
            Size = new Size(90, 32),
        };
        btnAbout.Click += (_, _) => ShowAbout();
        _chkStartWithWindows.Text = "开机时自动启动";
        _chkStartWithWindows.Location = new Point(360, y);
        _chkStartWithWindows.Size = new Size(280, 32);
        _chkStartWithWindows.Checked = false;
        _chkStartWithWindows.AccessibleName = "开机时自动启动";
        _chkStartWithWindows.CheckedChanged += ChkStartWithWindows_CheckedChanged;
        y += 46;

        var logLabel = new AntdUI.Label { Location = new Point(16, y), Size = new Size(400, 22), Text = "运行日志（不会记录密钥或令牌）" };
        y += 22;
        _txtLog.Location = new Point(16, y);
        _txtLog.Size = new Size(628, 200);
        _txtLog.Multiline = true;
        _txtLog.ReadOnly = true;
        _txtLog.AutoScroll = true;
        _txtLog.AccessibleName = "运行日志";

        body.Controls.AddRange(new Control[]
        {
            headline, sub, listen, _lnkSwagger,
            _txtApiKey, _txtToken, _cmbVoice, _txtTest,
            btnSave, _btnPlay, btnAbout, _chkStartWithWindows, logLabel, _txtLog,
        });

        Controls.Add(body);
        Controls.Add(header);
    }

    private void SetupSecretInput(AntdUI.Input input, int x, int y, string accessibleName)
    {
        input.Location = new Point(x, y);
        input.Size = new Size(494, 32);
        input.UseSystemPasswordChar = true;
        input.PasswordCopy = false;
        input.Suffix = _eyeClose;
        input.AccessibleName = accessibleName;
        input.AccessibleDescription = "当前为密文，点击右侧眼睛显示明文";
        input.SuffixClick += (_, _) => ToggleSecret(input);
    }

    private void ToggleSecret(AntdUI.Input input)
    {
        var hide = !input.UseSystemPasswordChar;
        input.UseSystemPasswordChar = hide;
        input.Suffix = hide ? _eyeClose : _eyeOpen;
        input.AccessibleDescription = hide
            ? "当前为密文，点击右侧眼睛显示明文"
            : "当前为明文，点击右侧眼睛恢复密文";
    }

    private static AntdUI.Label MakeLabel(string text, int x, int y) => new()
    {
        Location = new Point(x, y),
        Size = new Size(130, 22),
        Text = text,
    };

    private void BuildTray()
    {
        _trayMenu.Items.Add("打开主窗口", null, (_, _) => ShowMain());
        _trayMenu.Items.Add("关于", null, (_, _) => ShowAbout());
        _trayMenu.Items.Add(new ToolStripSeparator());
        _trayMenu.Items.Add("退出", null, (_, _) => ExitApplication());

        _tray.Icon = _appIcon;
        _tray.Text = "本机喇叭，一条 HTTP，电脑当刻说话。";
        _tray.Visible = true;
        _tray.ContextMenuStrip = _trayMenu;
        _tray.DoubleClick += (_, _) => ShowMain();
    }

    private async void MainForm_Load(object? sender, EventArgs e)
    {
        AppLog.SetSink(line =>
        {
            if (IsDisposed)
                return;
            BeginInvoke(() =>
            {
                if (_txtLog.IsDisposed)
                    return;
                var current = _txtLog.Text ?? "";
                if (current.Length > 0 && !current.EndsWith(Environment.NewLine, StringComparison.Ordinal))
                    current += Environment.NewLine;
                _txtLog.Text = current + line + Environment.NewLine;
            });
        });

        _loading = true;
        LoadConfigToForm();
        FillVoices();
        SyncStartupRegistration();
        _loading = false;

        try
        {
            await _http.StartAsync();
        }
        catch (Exception ex)
        {
            AppLog.Error("HTTP 服务启动失败", ex);
            AntdUI.Modal.open(this, "alert_speaker", "HTTP 服务启动失败：" + ex.Message + Environment.NewLine + "监听地址仍应为 " + ListenSettings.Origin);
        }
    }

    private void LoadConfigToForm()
    {
        var config = _store.LoadOrCreate();
        _txtApiKey.Text = config.QwenApiKey ?? "";
        _txtToken.Text = config.AlertToken ?? "";
        _chkStartWithWindows.Checked = config.StartWithWindows;
    }

    private void FillVoices()
    {
        _cmbVoice.Items.Clear();
        var config = _store.Load();
        var current = VoicePresetList.Contains(config.Voice) ? config.Voice : VoicePresetList.DefaultVoiceId;
        var selected = 0;
        var i = 0;
        foreach (var preset in VoicePresetList.AllVoicePresets)
        {
            _cmbVoice.Items.Add(new AntdUI.SelectItem($"{preset.Id}（{preset.Description}）", preset.Id));
            if (string.Equals(preset.Id, current, StringComparison.Ordinal))
                selected = i;
            i++;
        }
        if (_cmbVoice.Items.Count > 0)
            _cmbVoice.SelectedIndex = selected;
    }

    private void CmbVoice_SelectedIndexChanged(object sender, AntdUI.IntEventArgs e)
    {
        if (_loading)
            return;
        SaveFromForm(announce: false);
        var id = CurrentVoiceId();
        if (!string.IsNullOrWhiteSpace(id))
            AppLog.Info("已切换音色：" + id);
    }

    private string? CurrentVoiceId()
    {
        if (_cmbVoice.SelectedValue is string id && VoicePresetList.Contains(id))
            return id;
        return null;
    }

    private void SaveFromForm(bool announce)
    {
        var config = _store.LoadOrCreate();
        config.QwenApiKey = _txtApiKey.Text?.Trim() ?? "";
        config.AlertToken = _txtToken.Text?.Trim() ?? "";
        var voiceId = CurrentVoiceId();
        if (!string.IsNullOrWhiteSpace(voiceId))
            config.Voice = voiceId;
        config.StartWithWindows = _chkStartWithWindows.Checked;
        _store.Save(config);
        if (announce)
            AppLog.Info("配置已保存");
    }

    private void ChkStartWithWindows_CheckedChanged(object sender, AntdUI.BoolEventArgs e)
    {
        if (_loading)
            return;
        ApplyStartWithWindows(e.Value);
    }

    private void ApplyStartWithWindows(bool enabled)
    {
        var config = _store.LoadOrCreate();
        config.StartWithWindows = enabled;
        _store.Save(config);
        try
        {
            WindowsStartup.Apply(enabled);
            AppLog.Info(enabled ? "已开启开机时自动启动" : "已关闭开机时自动启动");
        }
        catch (Exception ex)
        {
            AppLog.Error("更新开机启动登记失败", ex);
            AntdUI.Modal.open(this, "alert_speaker", "更新开机启动登记失败：" + ex.Message);
            _loading = true;
            _chkStartWithWindows.Checked = !enabled;
            config.StartWithWindows = !enabled;
            _store.Save(config);
            _loading = false;
        }
    }

    private void SyncStartupRegistration()
    {
        try
        {
            WindowsStartup.Apply(_chkStartWithWindows.Checked);
        }
        catch (Exception ex)
        {
            AppLog.Error("同步开机启动登记失败", ex);
        }
    }

    private void BtnPlay_Click(object? sender, EventArgs e)
    {
        SaveFromForm(announce: false);
        var config = _store.Load();
        var text = _txtTest.Text?.Trim() ?? "";
        if (string.IsNullOrWhiteSpace(text))
        {
            AntdUI.Modal.open(this, "alert_speaker", "请输入要试听的文本。");
            return;
        }
        if (!config.HasQwenApiKey)
        {
            AntdUI.Modal.open(this, "alert_speaker", "未配置通义千问 API Key。请在主窗口填写 QwenApiKey 并保存，或写入运行目录的 App.json。");
            return;
        }

        var voice = VoicePresetList.Contains(config.Voice) ? config.Voice : VoicePresetList.DefaultVoiceId;
        _queue.Enqueue(text, config.QwenApiKey, voice);
        AppLog.Info($"测试窗口已交播（字符数 {text.Length}，音色 {voice}）");
    }

    private void ShowAbout()
    {
        var config = _store.Load();
        using var form = new AboutForm(config.AlertToken);
        form.ShowDialog(this.Visible ? this : null);
    }

    private void ShowMain()
    {
        Show();
        WindowState = FormWindowState.Normal;
        Activate();
    }

    private void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
    {
        if (_exiting || e.CloseReason != CloseReason.UserClosing)
            return;
        e.Cancel = true;
        Hide();
        _tray.ShowBalloonTip(2000, "alert_speaker", "已在托盘常驻。预警出声，不用盯着聊天窗口。", ToolTipIcon.Info);
    }

    private async void MainForm_FormClosed(object? sender, FormClosedEventArgs e)
    {
        AppLog.SetSink(null);
        try { await _http.StopAsync(); } catch { /* 退出时忽略 */ }
        _http.Dispose();
        _queue.Dispose();
        _httpClient.Dispose();
        _tray.Visible = false;
        _tray.Dispose();
        _trayMenu.Dispose();
        _appIcon.Dispose();
        _headerIcon.Dispose();
        _eyeOpen.Dispose();
        _eyeClose.Dispose();
    }

    private void ExitApplication()
    {
        _exiting = true;
        Close();
        Application.Exit();
    }

    private static void OpenUrl(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
        }
        catch (Exception ex)
        {
            AppLog.Error("打开链接失败", ex);
        }
    }
}
