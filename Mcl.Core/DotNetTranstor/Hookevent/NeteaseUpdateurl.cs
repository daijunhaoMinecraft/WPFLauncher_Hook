using DotNetTranstor;
using DotNetTranstor.Hookevent;
using WPFLauncher.Manager.Login;

namespace MicrosoftTranslator.DotNetTranstor.Hookevent
{
    public class NeteaseUpdateUrl : IMethodHook
    {
        [HookMethod("WPFLauncher.Manager.Configuration.axf", "c", null)]
        public string UpdateUrl()
        {
            return Path_Bool.NeteaseUpdateDomainhttp;
        }
    }
}