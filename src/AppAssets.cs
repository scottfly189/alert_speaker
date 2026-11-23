using System.Drawing.Drawing2D;
using System.Reflection;

namespace AlertSpeaker;

/// <summary>
/// 从嵌入资源加载托盘图标和眼睛图标。
/// </summary>
internal static class AppAssets
{
    private const string SpeakIconName = "AlertSpeaker.Assets.speak.ico";
    private const string SpeakPngName = "AlertSpeaker.Assets.speak.png";
    private const string EyeOpenName = "AlertSpeaker.Assets.eye.png";
    private const string EyeCloseName = "AlertSpeaker.Assets.eye-close.png";

    public static Icon LoadSpeakIcon()
    {
        using var stream = Open(SpeakIconName);
        using var icon = new Icon(stream);
        return (Icon)icon.Clone();
    }

    public static Image LoadSpeakImage(int size) => LoadPng(SpeakPngName, size);

    public static Image LoadEyeOpen(int size) => LoadPng(EyeOpenName, size);

    public static Image LoadEyeClose(int size) => LoadPng(EyeCloseName, size);

    private static Image LoadPng(string name, int size)
    {
        using var stream = Open(name);
        using var src = new Bitmap(stream);
        var bmp = new Bitmap(size, size);
        using var g = Graphics.FromImage(bmp);
        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        g.SmoothingMode = SmoothingMode.HighQuality;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
        g.Clear(Color.Transparent);
        g.DrawImage(src, 0, 0, size, size);
        return bmp;
    }

    private static Stream Open(string name)
    {
        var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(name);
        if (stream == null)
            throw new InvalidOperationException("缺少嵌入资源：" + name);
        return stream;
    }
}
