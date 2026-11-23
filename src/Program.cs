namespace AlertSpeaker;

static class Program
{
    private static Mutex? _mutex;

    [STAThread]
    static void Main()
    {
        bool createdNew;
        _mutex = new Mutex(true, "alert_speaker_Single_Instance", out createdNew);
        if (!createdNew)
        {
            MessageBox.Show(
                "alert_speaker 已在运行。请从托盘打开主窗口。",
                "alert_speaker",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        ApplicationConfiguration.Initialize();
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
        Application.ThreadException += (_, e) =>
        {
            AppLog.Error("未处理的界面异常", e.Exception);
        };
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            if (e.ExceptionObject is Exception ex)
                AppLog.Error("未处理的后台异常", ex);
        };

        Application.Run(new MainForm());
    }
}
