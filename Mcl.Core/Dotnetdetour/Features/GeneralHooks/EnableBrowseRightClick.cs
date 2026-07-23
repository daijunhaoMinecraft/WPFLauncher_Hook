using CefSharp;
using Mcl.Core.Dotnetdetour.CoreEngine.Attributes;
using Mcl.Core.Dotnetdetour.CoreEngine.Interfaces;

namespace Mcl.Core.Dotnetdetour.Features.GeneralHooks;

public class EnableBrowseRightClick : IMethodHook
{
    [HookMethod("WPFLauncher.View.cx", "OnContextMenuCommand")]
    public bool OnContextMenuCommand(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame,
        IContextMenuParams parameters, CefMenuCommand commandId, CefEventFlags eventFlags)
    {
        return false;
    }
}