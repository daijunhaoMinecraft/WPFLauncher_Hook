using System.Runtime.CompilerServices;

namespace Mcl.Core.Dotnetdetour.Hookevent
{
	//解决密码只能是纯数字问题(特别是联机大厅密码设置)
	internal class Password_String_1 : IMethodHook
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
