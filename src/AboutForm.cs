namespace AlertSpeaker;

public sealed class AboutForm : AntdUI.Window
{
    public AboutForm(string? alertToken)
    {
        Text = "关于 alert_speaker";
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        Resizable = false;
        ClientSize = new Size(540, 560);
        Font = new Font("Microsoft YaHei UI", 9F);
        Icon = AppAssets.LoadSpeakIcon();
        Mode = AntdUI.TAMode.Light;

        var header = new AntdUI.PageHeader
        {
            Dock = DockStyle.Top,
            Height = 44,
            Text = "关于 alert_speaker",
            ShowButton = true,
            ShowIcon = true,
            Icon = AppAssets.LoadSpeakImage(20),
            MaximizeBox = false,
            MinimizeBox = false,
            FullBox = false,
            EnableDoubleClickMaximize = false,
            CancelButton = true,
            DividerShow = true,
            DragMove = true,
        };

        var body = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
        };

        var promo =
            "本机喇叭，一条 HTTP，电脑当刻说话。" + Environment.NewLine + Environment.NewLine +
            "预警出声，不用盯着聊天窗口。" + Environment.NewLine +
            "只守 127.0.0.1，带鉴权，喇叭不出局域网。" + Environment.NewLine +
            "托盘常驻，音色可换，测试窗口可试听。" + Environment.NewLine + Environment.NewLine +
            "alert_speaker 是一个本机常驻的语音播报小服务。你的 Agent 或脚本向 http://127.0.0.1:18765/alert 发一句中文，电脑就会用通义千问 TTS 把警告念出来。它只监听本机、请求要带令牌，可以换音色、可以在测试窗口里先听一遍。" + Environment.NewLine + Environment.NewLine +
            "也可供 Agent / 本地自动化呼叫。" + Environment.NewLine + Environment.NewLine +
            "监听：" + ListenSettings.Origin + Environment.NewLine +
            "文档：" + ListenSettings.SwaggerUrl;

        var token = (alertToken ?? "").Trim();
        string authText;
        if (string.IsNullOrWhiteSpace(token))
        {
            authText =
                "当前鉴权：未设置令牌（AlertToken 为空）。" + Environment.NewLine +
                "测试时无需携带 X-Alert-Token，无头或空头也可调用 /alert。空 text 仍返回 400。" + Environment.NewLine +
                "可直接在 Swagger 中试接口，不必先填写未知令牌。";
        }
        else
        {
            authText =
                "当前鉴权：已设置令牌。" + Environment.NewLine +
                "测试时必须在请求头携带 X-Alert-Token，错或缺令牌返回 401 且不播报。" + Environment.NewLine +
                "可将下面的当前令牌粘贴到 Swagger 的 Authorize（X-Alert-Token）中：";
        }

        var box = new AntdUI.Input
        {
            Multiline = true,
            ReadOnly = true,
            AutoScroll = true,
            Location = new Point(20, 16),
            Size = new Size(500, 280),
            Text = promo,
        };
        var authBox = new AntdUI.Input
        {
            Multiline = true,
            ReadOnly = true,
            AutoScroll = true,
            Location = new Point(20, 304),
            Size = new Size(500, 90),
            Text = authText,
            AccessibleName = "鉴权说明",
        };
        body.Controls.Add(box);
        body.Controls.Add(authBox);

        var nextY = 402;
        if (!string.IsNullOrWhiteSpace(token))
        {
            var tokenBox = new AntdUI.Input
            {
                ReadOnly = true,
                Location = new Point(20, nextY),
                Size = new Size(500, 32),
                Text = token,
                UseSystemPasswordChar = false,
                AccessibleName = "当前鉴权令牌",
            };
            body.Controls.Add(tokenBox);
            nextY += 44;
        }

        var btn = new AntdUI.Button
        {
            Text = "确定",
            Type = AntdUI.TTypeMini.Primary,
            Location = new Point(420, 468),
            Size = new Size(100, 32),
            DialogResult = DialogResult.OK,
        };
        body.Controls.Add(btn);
        AcceptButton = btn;

        Controls.Add(body);
        Controls.Add(header);
    }
}
