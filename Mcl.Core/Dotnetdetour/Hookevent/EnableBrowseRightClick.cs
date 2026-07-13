using CefSharp;

namespace Mcl.Core.Dotnetdetour.Hookevent;

public class EnableBrowseRightClick : IMethodHook
{
    [HookMethod("WPFLauncher.View.cx", "OnContextMenuCommand", null)]
    public bool OnContextMenuCommand(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IContextMenuParams parameters, CefMenuCommand commandId, CefEventFlags eventFlags)
    {
        return false;
    }
}