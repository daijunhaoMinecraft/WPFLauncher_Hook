using System.Runtime.CompilerServices;

namespace Mcl.Core.Dotnetdetour.HookList
{
    internal class AntiIndulgencePasswordFix : IMethodHook
    {
        [OriginalMethod]
        public static bool No_Password_Number(string dpu)
        {
            return true;
        }

        [CompilerGenerated]
        [HookMethod("WPFLauncher.Manager.AntiIndulgence.aro", "h", "No_Password_Number")]
        public static string h(string nfu, string nfv = "密码")
        {
            return "";
        }
    }
}
