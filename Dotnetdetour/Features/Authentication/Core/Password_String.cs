using System.Runtime.CompilerServices;

namespace Mcl.Core.Dotnetdetour.HookList
{
	//解决密码只能是纯数字问题(特别是联机大厅密码设置)
	internal class Password_String : IMethodHook
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
