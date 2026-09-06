using Mcl.Core.Dotnetdetour.CoreEngine.Attributes;
using Mcl.Core.Dotnetdetour.CoreEngine.Interfaces;
using Mcl.Core.Dotnetdetour.Models.Config;
using Mcl.Core.Dotnetdetour.Utilities.Diagnostics;

namespace Mcl.Core.Dotnetdetour.Features.GeneralHooks;

public class LoggerHook : IMethodHook
{
    [HookMethod("WPFLauncher.Util.te", "a")]
    public void Hook_a(object instance)
    {
        // The host still owns its old-log cleanup; apply our policy afterwards.
        Original_a(instance);
        LauncherLogging.Configure(WpfConfig.EnableVerboseLogging,
            WpfConfig.WriteLauncherLogsToFile, WpfConfig.LauncherRootDirectory);
    }

    [OriginalMethod]
    public void Original_a(object instance) { }
}
