using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WPFLauncher.Manager.Configuration.CppConfigure;
using WPFLauncher.Manager.LanGame;
using WPFLauncher.Network.Protocol.LobbyGame;
using WPFLauncher.Util;
using MicrosoftTranslator.DotNetTranstor.Tools;
using NLog.Targets;

namespace Login.NetEase
{
	// Token: 0x02000036 RID: 54
	internal class Function
	{

		// Token: 0x06000271 RID: 625 RVA: 0x00009DC0 File Offset: 0x00007FC0
		public static string GetName(string values)
		{
			string text;
			try
			{
				text = JsonConvert.DeserializeObject<JObject>(JsonConvert.DeserializeObject<JObject>(values)["entity"].ToString())["name"].ToString();
			}
			catch
			{
				text = null;
			}
			return text;
		}
		
		// Token: 0x06000831 RID: 2097 RVA: 0x000220B8 File Offset: 0x000202B8
		public static void ClientLog(string log, ConsoleColor color = ConsoleColor.Gray)
		{
			Console.ForegroundColor = color;
			string text = string.Format("[INFO][{0}]{1}", DateTime.Now, log);
			Console.ForegroundColor = ConsoleColor.Gray;
			DebugPrint.LogDebug_NoColorSelect(text);
		}

		// Token: 0x06000832 RID: 2098 RVA: 0x00022218 File Offset: 0x00020418
		public static void ClientError(string log, ConsoleColor color = ConsoleColor.Red)
		{
			Console.ForegroundColor = color;
			string text = "[ERROR][" + DateTime.Now.ToString() + "]" + log;
			Console.ForegroundColor = ConsoleColor.Gray;
			DebugPrint.LogDebug_NoColorSelect(text);
		}

		// Token: 0x06000286 RID: 646 RVA: 0x0000A746 File Offset: 0x00008946
		public static string[] GetAllpng(string path)
		{
			return Directory.GetFiles(path, "*.png", SearchOption.AllDirectories);
		}

		// Token: 0x06000287 RID: 647 RVA: 0x0000A754 File Offset: 0x00008954
		public static string[] GetAlljpeg(string path)
		{
			return Directory.GetFiles(path, "*.jpeg", SearchOption.AllDirectories);
		}

		// Token: 0x06000288 RID: 648 RVA: 0x0000A762 File Offset: 0x00008962
		public static string[] GetAlljpg(string path)
		{
			return Directory.GetFiles(path, "*.jpg", SearchOption.AllDirectories);
		}

		// Token: 0x06000289 RID: 649 RVA: 0x0000A770 File Offset: 0x00008970
		public static string[] GetAlljson(string path)
		{
			return Directory.GetFiles(path, "*.json", SearchOption.AllDirectories);
		}

		// Token: 0x0600028A RID: 650 RVA: 0x0000A77E File Offset: 0x0000897E
		public static string[] GetAlllang(string path)
		{
			return Directory.GetFiles(path, "*.lang", SearchOption.AllDirectories);
		}

		// Token: 0x0600028B RID: 651 RVA: 0x0000A78C File Offset: 0x0000898C
		public static string[] GetAllmaterial(string path)
		{
			return Directory.GetFiles(path, "*.material", SearchOption.AllDirectories);
		}

		// Token: 0x0600028C RID: 652 RVA: 0x0000A79A File Offset: 0x0000899A
		public static string[] GetAlltga(string path)
		{
			return Directory.GetFiles(path, "*.tga", SearchOption.AllDirectories);
		}

		// Token: 0x0600028D RID: 653 RVA: 0x0000A7A8 File Offset: 0x000089A8
		public static string[] GetAllFiles(string path, string name)
		{
			return Directory.GetFiles(path, "*" + name, SearchOption.AllDirectories);
		}

		// Token: 0x0600028E RID: 654 RVA: 0x0000A7BC File Offset: 0x000089BC
		public static string[] GetAllFiless(string path)
		{
			List<string> strArray = new List<string>();
			List<string> strings = Directory.GetFiles(path, "*", SearchOption.AllDirectories).ToList<string>();
			for (int i = 0; i < strings.Count; i++)
			{
				if (Directory.Exists(strings[i]))
				{
					strArray.Concat(Function.GetAllFiless(strings[i]));
				}
				else
				{
					strArray.Add(strings[i]);
				}
			}
			return strArray.ToArray();
		}

		// Token: 0x0600028F RID: 655 RVA: 0x0000A828 File Offset: 0x00008A28
		public static bool DeleteFile(string fileFullPath)
		{
			if (!File.Exists(fileFullPath))
			{
				return false;
			}
			if (File.GetAttributes(fileFullPath) == FileAttributes.Directory)
			{
				Directory.Delete(fileFullPath, true);
				return true;
			}
			File.Delete(fileFullPath);
			return true;
		}
	}	
}
