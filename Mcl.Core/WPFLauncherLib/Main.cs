using System;
using System.Diagnostics;
using DotNetDetour;
using DotNetTranstor;

namespace WPFLauncherLib
{
	// Token: 0x0200001B RID: 27
	public class Main
	{
		public static bool IsinitHook = false;
		// Token: 0x0600004D RID: 77 RVA: 0x0000392C File Offset: 0x00001B2C
		public static int start(string msg)
		{
			if (!IsinitHook)
			{
				MethodHook.Install(null);
				IsinitHook = true;
			}
			return 0;
		}

		// Token: 0x0600004E RID: 78 RVA: 0x00003948 File Offset: 0x00001B48
		public static void killMe(int pid = -1)
		{
			bool flag = pid <= 0;
			if (flag)
			{
				pid = Process.GetCurrentProcess().Id;
			}
			Process process = new Process();
			process.StartInfo.FileName = "cmd.exe";
			process.StartInfo.UseShellExecute = false;
			process.StartInfo.RedirectStandardInput = true;
			process.StartInfo.RedirectStandardOutput = true;
			process.StartInfo.RedirectStandardError = true;
			process.StartInfo.CreateNoWindow = true;
			process.Start();
			process.StandardInput.WriteLine("taskkill /PID " + pid.ToString() + "&exit");
			process.StandardInput.AutoFlush = true;
			process.StandardOutput.ReadToEnd();
			process.WaitForExit();
			process.Close();
			Process.GetCurrentProcess().Kill();
		}
	}
}
