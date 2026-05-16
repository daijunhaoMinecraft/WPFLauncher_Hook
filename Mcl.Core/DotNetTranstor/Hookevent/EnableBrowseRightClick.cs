using CefSharp;
using DotNetTranstor;

namespace Mcl.Core.DotNetTranstor.Hookevent;

public class EnableBrowseRightClick : IMethodHook
{
    [HookMethod("WPFLauncher.View.cx", "OnContextMenuCommand", null)]
    public bool OnContextMenuCommand(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IContextMenuParams parameters, CefMenuCommand commandId, CefEventFlags eventFlags)
    {
        return false;
    }
}