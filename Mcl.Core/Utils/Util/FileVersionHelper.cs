using System;
using System.Diagnostics;
using System.Reflection;

namespace Mcl.Core.Utils.Util
{
	// Token: 0x02000005 RID: 5
	public class FileVersionHelper
	{
		// Token: 0x06000009 RID: 9 RVA: 0x00002174 File Offset: 0x00000374
		public static string GetVersion(string executablePath = null)
		{
			string text2;
			try
			{
				bool flag = string.IsNullOrEmpty(executablePath);
				string text;
				if (flag)
				{
					text = Assembly.GetExecutingAssembly().GetName().Version.ToString();
				}
				else
				{
					FileVersionInfo versionInfo = FileVersionInfo.GetVersionInfo(executablePath);
					text = versionInfo.FileVersion;
				}
				text2 = text;
			}
			catch (Exception)
			{
				text2 = "0.0.0.0";
			}
			return text2;
		}
	}
}
