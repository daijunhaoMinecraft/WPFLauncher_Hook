using System.Runtime.CompilerServices;
using Mcl.Core.Dotnetdetour.CoreEngine.Attributes;
using Mcl.Core.Dotnetdetour.CoreEngine.Interfaces;

namespace Mcl.Core.Dotnetdetour.Features.Authentication.Core;

//解决密码只能是纯数字问题(特别是联机大厅密码设置)
internal class PasswordFix : IMethodHook
{
    [HookMethod("WPFLauncher.ViewModel.LobbyGame.jo", "c", null)]
    public static bool CanUseThisPasswordHook(string password)
    {
        return true;
    }
    [HookMethod("WPFLauncher.Manager.AntiIndulgence.aro", "h", "No_Password_Number")]
    public static string CanUseThisPasswordHook(string password, string moduleName = "密码")
    {
        return password;
    }
}