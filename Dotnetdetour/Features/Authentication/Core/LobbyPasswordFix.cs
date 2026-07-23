using System.Runtime.CompilerServices;

namespace Mcl.Core.Dotnetdetour.HookList
{
    internal class LobbyPasswordFix : IMethodHook
    {
        [OriginalMethod]
        public static bool No_Password_Number(string dpu)
        {
            return true;
        }

        [CompilerGenerated]
        [HookMethod("WPFLauncher.ViewModel.LobbyGame.jo", "c", "No_Password_Number")]
        public static bool c(string dpu)
        {
            return true;
        }
    }
}
