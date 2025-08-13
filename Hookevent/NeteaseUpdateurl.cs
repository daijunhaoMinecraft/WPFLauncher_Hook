using DotNetTranstor;
using DotNetTranstor.Hookevent;
using WPFLauncher.Manager.Login;

namespace MicrosoftTranslator.DotNetTranstor.Hookevent
{
    public class NeteaseUpdateUrl : IMethodHook
    {
        [HookMethod("WPFLauncher.Manager.Configuration.axe", "c", null)]
        public string UpdateUrl()
        {
            if (Path_Bool.NeteaseUpdateDomainhttp.EndsWith("/"))
            {
                return Path_Bool.NeteaseUpdateDomainhttp + "serverlist/release.json";
            }
            return Path_Bool.NeteaseUpdateDomainhttp + "/serverlist/release.json";
        }
    }
}