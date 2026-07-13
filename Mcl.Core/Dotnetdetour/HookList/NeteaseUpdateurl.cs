namespace Mcl.Core.Dotnetdetour.HookList
{
    public class NeteaseUpdateUrl : IMethodHook
    {
        [HookMethod("WPFLauncher.Manager.Configuration.axg", "c", null)]
        public string UpdateUrl()
        {
            return WpfConfig.NeteaseUpdateDomainhttp;
        }
    }
}