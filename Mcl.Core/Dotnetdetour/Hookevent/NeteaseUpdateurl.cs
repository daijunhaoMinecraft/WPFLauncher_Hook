namespace Mcl.Core.Dotnetdetour.Hookevent
{
    public class NeteaseUpdateUrl : IMethodHook
    {
        [HookMethod("WPFLauncher.Manager.Configuration.axg", "c", null)]
        public string UpdateUrl()
        {
            return Path_Bool.NeteaseUpdateDomainhttp;
        }
    }
}