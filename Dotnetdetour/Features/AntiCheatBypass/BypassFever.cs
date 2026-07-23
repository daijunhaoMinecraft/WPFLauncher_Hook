using System.Runtime.InteropServices;

namespace Mcl.Core.Dotnetdetour.HookList
{
    internal class BypassFever : IMethodHook
    {
        [DllImport("kernel32.dll")]
        private static extern bool AllocConsole();

        [OriginalMethod]
        public static void X19_Fever_bypass() { }

        [HookMethod("WPFLauncher.Manager.apm", "aj", "X19_Fever_bypass")]
        public bool Fever_False()
        {
            return false;
        }
    }
}