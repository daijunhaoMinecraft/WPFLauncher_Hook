using System;
using System.IO;
using DotNetDetour;
using DotNetTranstor;

namespace ClassLibrary1.Fucker
{
	// Token: 0x02000012 RID: 18
	internal class Mods : IMethodHook
	{
		// Token: 0x06000041 RID: 65 RVA: 0x0000340C File Offset: 0x0000160C
		[HookMethod("System.IO.Directory", null, null)]
		public static string[] GetFiles(string path, string searchPattern, SearchOption searchOption)
		{
			Console.ForegroundColor = ConsoleColor.Cyan; // 设置输出颜色为青色
			Console.WriteLine($"[DEBUG] GetFiles 被调用，路径: {path}, 搜索模式: {searchPattern}, 搜索选项: {searchOption}"); // 输出调试信息

			string[] array = Mods.GetFiles_Original(path, searchPattern, searchOption);
			if (array == null)
			{
				Console.WriteLine("[DEBUG] 原始文件数组为空，返回空数组"); // 输出调试信息
				array = new string[0];
			}
			else
			{
				Console.WriteLine($"[DEBUG] 原始文件数组长度: {array.Length}"); // 输出调试信息
			}

			for (int i = 0; i < array.Length; i++)
			{
				if (!Mods.isGoodMod(array[i]))
				{
					Console.WriteLine($"[DEBUG] 文件 {array[i]} 不是有效的模组，设置为空字符串"); // 输出调试信息
					array[i] = "";
				}
			}
			return array;
		}

		// Token: 0x06000042 RID: 66 RVA: 0x00003474 File Offset: 0x00001674
		[OriginalMethod]
		public static string[] GetFiles_Original(string path, string searchPattern, SearchOption searchOption)
		{
			return null;
		}

		// Token: 0x06000043 RID: 67 RVA: 0x00003488 File Offset: 0x00001688
		[HookMethod("System.IO.Directory", null, null)]
		public static string[] GetFiles(string path)
		{
			Console.ForegroundColor = ConsoleColor.Cyan; // 设置输出颜色为青色
			Console.WriteLine($"[DEBUG] GetFiles 被调用，路径: {path}"); // 输出调试信息

			string[] array = Mods.GetFiles_Original(path);
			if (array == null)
			{
				Console.WriteLine("[DEBUG] 原始文件数组为空，返回空数组"); // 输出调试信息
				array = new string[0];
			}
			else
			{
				Console.WriteLine($"[DEBUG] 原始文件数组长度: {array.Length}"); // 输出调试信息
			}

			for (int i = 0; i < array.Length; i++)
			{
				if (!Mods.isGoodMod(array[i]))
				{
					Console.WriteLine($"[DEBUG] 文件 {array[i]} 不是有效的模组，设置为空字符串"); // 输出调试信息
					array[i] = "";
				}
			}
			return array;
		}

		// Token: 0x06000044 RID: 68 RVA: 0x000034EC File Offset: 0x000016EC
		[OriginalMethod]
		public static string[] GetFiles_Original(string path)
		{
			return null;
		}

		// Token: 0x06000045 RID: 69 RVA: 0x00003500 File Offset: 0x00001700
		[HookMethod("System.IO.Directory", null, null)]
		public static string[] GetFiles(string path, string searchPattern)
		{
			Console.ForegroundColor = ConsoleColor.Cyan; // 设置输出颜色为青色
			Console.WriteLine($"[DEBUG] GetFiles 被调用，路径: {path}, 搜索模式: {searchPattern}"); // 输出调试信息

			string[] array = Mods.GetFiles_Original(path, searchPattern);
			if (array == null)
			{
				Console.WriteLine("[DEBUG] 原始文件数组为空，返回空数组"); // 输出调试信息
				array = new string[0];
			}
			else
			{
				Console.WriteLine($"[DEBUG] 原始文件数组长度: {array.Length}"); // 输出调试信息
			}

			for (int i = 0; i < array.Length; i++)
			{
				if (!Mods.isGoodMod(array[i]))
				{
					Console.WriteLine($"[DEBUG] 文件 {array[i]} 不是有效的模组，设置为空字符串"); // 输出调试信息
					array[i] = "";
				}
			}
			return array;
		}

		// Token: 0x06000046 RID: 70 RVA: 0x00003568 File Offset: 0x00001768
		[OriginalMethod]
		public static string[] GetFiles_Original(string path, string searchPattern)
		{
			return null;
		}

		// Token: 0x06000047 RID: 71 RVA: 0x0000357C File Offset: 0x0000177C
		public static bool isGoodMod(string filePath)
		{
			try
			{
				bool isValid = filePath.Contains("\\Game\\.minecraft\\mods\\") && filePath.EndsWith(".jar") && !filePath.Contains("@");
				if (isValid)
				{
					Console.WriteLine($"[DEBUG] 文件 {filePath} 不是有效的模组"); // 输出调试信息
					return false;
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"[ERROR] 检查模组时发生异常: {ex.Message}"); // 输出错误信息
				return false;
			}
			return true;
		}

		// Token: 0x06000048 RID: 72 RVA: 0x000035E0 File Offset: 0x000017E0
		[HookMethod("WPFLauncher.Manager.Auth.atl", null, null)]
		public bool get_modsChanged()
		{
			Console.WriteLine("[DEBUG] get_modsChanged 被调用"); // 输出调试信息
			return false;
		}

		// Token: 0x06000049 RID: 73 RVA: 0x000024B9 File Offset: 0x000006B9
		[OriginalMethod]
		private static void get_modsChanged_Original()
		{
		}

		// Token: 0x0600004A RID: 74 RVA: 0x00002424 File Offset: 0x00000624
		public Mods()
		{
			Console.WriteLine("[DEBUG] Mods 构造函数被调用"); // 输出调试信息
		}
	}
}
