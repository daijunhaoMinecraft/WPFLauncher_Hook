using System;
using System.Globalization;
using System.IO;
using System.Text;
using Mcl.Core.Dotnetdetour.Models.Config;

namespace Mcl.Core.Dotnetdetour.UI.WebAssets;

public static class HtmlResource
{
    private static readonly Lazy<string> RoomPage = new Lazy<string>(() => Read("RoomManage.html"));
    private static readonly Lazy<string> SettingsPage = new Lazy<string>(() => Read("Settings.html"));
    private static readonly Lazy<string> NetworkCapturePage = new Lazy<string>(() => Read("NetworkCapture.html"));
    private static readonly Lazy<string> Theme = new Lazy<string>(() => Read("Fluent.css"));

    public static string GetRoomManageHtml() => WithTheme(RoomPage.Value)
        .Replace("__MCL_HTTP_PORT__", WpfConfig.HttpPort.ToString(CultureInfo.InvariantCulture));

    public static string GetHotUpdateHtml() => WithTheme(SettingsPage.Value);

    public static string GetNetworkCaptureHtml() => WithTheme(NetworkCapturePage.Value);

    private static string WithTheme(string html) => html.Replace("/* MCL_FLUENT_THEME */", Theme.Value);

    private static string Read(string name)
    {
        using (var stream = typeof(HtmlResource).Assembly.GetManifestResourceStream("Mcl.Core.Web." + name))
        {
            if (stream == null) throw new InvalidOperationException("Missing embedded UI resource: " + name);
            using (var reader = new StreamReader(stream, Encoding.UTF8)) return reader.ReadToEnd();
        }
    }
}
