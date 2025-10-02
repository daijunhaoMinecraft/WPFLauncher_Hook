using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using DotNetDetour;
using Mcl.Core.Network;

namespace Mcl.Core.Utils
{
	// Token: 0x02000002 RID: 2
	public class AppMutexHelper
	{
		// 导入 AllocConsole 函数
		[DllImport("kernel32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool AllocConsole();
		
		// Token: 0x06000001 RID: 1 RVA: 0x00002050 File Offset: 0x00000250
		public static bool CheckAppMutex()
		{
			string name = Assembly.GetEntryAssembly().GetName().Name;
			bool flag;
			AppMutexHelper.AppMutex = new Mutex(true, name, out flag);
			return flag;
		}

		// Token: 0x06000002 RID: 2 RVA: 0x00002084 File Offset: 0x00000284
		public static bool CheckAppMutex(string appId)
		{
			if (!File.Exists("DisableConsole"))
			{
				// 分配一个新的控制台
				AllocConsole();
				// 重定向输出流到控制台
				var writer = new StreamWriter(Console.OpenStandardOutput());
				writer.AutoFlush = true;
				Console.SetOut(writer);
				Console.OutputEncoding = Encoding.UTF8;
				Console.CursorVisible = false;
			}
			try
			{
				MethodHook.Install(null);
			}
			catch (ReflectionTypeLoadException ex)
			{
				Console.WriteLine("=== ReflectionTypeLoadException: 部分类型加载失败 ===");

				// 输出成功加载的类型（可选）
				if (ex.Types != null)
				{
					var loadedTypes = ex.Types.Where(t => t != null).ToArray();
					Console.WriteLine($"成功加载 {loadedTypes.Length} 个类型:");
					foreach (var type in loadedTypes)
					{
						Console.WriteLine($"  ✔ {type?.FullName}");
					}
				}

				// 输出加载失败的异常信息
				Console.WriteLine($"\n失败的加载异常 ({ex.LoaderExceptions.Length} 个):");
				for (int i = 0; i < ex.LoaderExceptions.Length; i++)
				{
					var loaderEx = ex.LoaderExceptions[i];
					Console.WriteLine($"--- 加载异常 #{i + 1} ---");
					Console.WriteLine(loaderEx.Message);

					// 如果是文件找不到，输出更详细信息
					if (loaderEx is FileNotFoundException fileEx && !string.IsNullOrEmpty(fileEx.FileName))
					{
						Console.WriteLine($"缺少程序集: {fileEx.FileName}");
					}

					// 输出完整堆栈（可选）
					Console.WriteLine(loaderEx.StackTrace);
				}

				// 仍然抛出或记录完整异常（可选）
				// throw; // 如果需要继续传播
			}
			catch (Exception ex)
			{
				// 其他非 ReflectionTypeLoadException 的异常
				Console.WriteLine("=== 未处理异常 ===");
				Console.WriteLine(ex);
			}
			//MethodHook.Install(null);
			bool flag;
			AppMutexHelper.AppMutex = new Mutex(true, appId, out flag);
			return true;
		}

		// Token: 0x04000001 RID: 1
		public static Mutex AppMutex;
	}
}
