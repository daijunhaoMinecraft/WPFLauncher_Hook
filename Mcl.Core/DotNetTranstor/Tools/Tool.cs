using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.Win32;
using MicrosoftTranslator.DotNetTranstor.Tools;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WPFLauncher.Manager.LanGame;

namespace Azure
{
	// Token: 0x0200006A RID: 106
	public class Tool
	{
		// Token: 0x060003B8 RID: 952 RVA: 0x0000D39C File Offset: 0x0000B59C
		public static string Between(string str, string leftstr, string rightstr)
		{
			int num = str.IndexOf(leftstr) + leftstr.Length;
			return str.Substring(num, str.IndexOf(rightstr, num) - num);
		}

		// Token: 0x060003B9 RID: 953 RVA: 0x0000D3CC File Offset: 0x0000B5CC
		public static string randomStr(int len, string[] arr = null)
		{
			if (arr == null || arr.Length <= 1)
			{
				arr = new string[]
				{
					"a", "b", "c", "d", "e", "f", "0", "1", "2", "3",
					"4", "5", "6", "7", "8", "9"
				};
			}
			string text = "";
			for (int i = 0; i < len; i++)
			{
				text += arr[new Random(new Random(Guid.NewGuid().GetHashCode()).Next(0, 0x64)).Next(arr.Length - 1)];
			}
			return text;
		}

		// Token: 0x060003BA RID: 954 RVA: 0x0000D4D0 File Offset: 0x0000B6D0
		public static Process RunCommand(string command)
		{
			ProcessStartInfo processStartInfo = new ProcessStartInfo();
			processStartInfo.FileName = "cmd.exe";
			processStartInfo.Arguments = "/c " + command;
			processStartInfo.RedirectStandardOutput = true;
			processStartInfo.UseShellExecute = false;
			Process process = new Process();
			process.StartInfo = processStartInfo;
			process.EnableRaisingEvents = true;
			process.Start();
			return process;
		}

		// Token: 0x060003BB RID: 955 RVA: 0x0000D52C File Offset: 0x0000B72C
		public static int GetProcessIdByPort(int port)
		{
			ProcessStartInfo processStartInfo = new ProcessStartInfo();
			processStartInfo.FileName = "netstat";
			processStartInfo.Arguments = "-ano -p TCP";
			processStartInfo.RedirectStandardOutput = true;
			processStartInfo.UseShellExecute = false;
			Process process = new Process();
			process.StartInfo = processStartInfo;
			process.Start();
			string text = process.StandardOutput.ReadToEnd();
			process.WaitForExit();
			string[] array = text.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
			foreach (string text2 in array)
			{
				string[] array3 = text2.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
				if (array3.Length >= 4)
				{
					string[] array4 = array3[1].Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
					string text3 = array3[array3.Length - 1];
					int num;
					int num2;
					if (array4.Length == 2 && int.TryParse(array4[1], out num) && num == port && int.TryParse(text3, out num2))
					{
						return num2;
					}
				}
			}
			return -1;
		}

		// Token: 0x060003BC RID: 956 RVA: 0x0000D624 File Offset: 0x0000B824
		public static int GetProcessId(string processName)
		{
			Process[] processesByName = Process.GetProcessesByName(processName);
			if (processesByName.Length != 0)
			{
				return processesByName[0].Id;
			}
			return -1;
		}

		// Token: 0x060003BD RID: 957 RVA: 0x0000D648 File Offset: 0x0000B848
		public static void KillProcessById(int processId)
		{
			try
			{
				Process processById = Process.GetProcessById(processId);
				processById.Kill();
			}
			catch (ArgumentException)
			{
			}
		}

		// Token: 0x060003BE RID: 958 RVA: 0x00003E04 File Offset: 0x00002004
		public static void PrintBlue(string message)
		{
			Console.ForegroundColor = ConsoleColor.Blue;
			DebugPrint.LogDebug_NoColorSelect(message);
			Console.ResetColor();
		}

		// Token: 0x060003BF RID: 959 RVA: 0x00003E18 File Offset: 0x00002018
		public static void PrintYellow(string message)
		{
			Console.ForegroundColor = ConsoleColor.Yellow;
			DebugPrint.LogDebug_NoColorSelect(message);
			Console.ResetColor();
		}

		// Token: 0x060003C0 RID: 960 RVA: 0x00003E2C File Offset: 0x0000202C
		public static void PrintGreen(string message)
		{
			Console.ForegroundColor = ConsoleColor.Green;
			DebugPrint.LogDebug_NoColorSelect(message);
			Console.ResetColor();
		}

		// Token: 0x060003C1 RID: 961 RVA: 0x00003E40 File Offset: 0x00002040
		public static void PrintRed(string message)
		{
			Console.ForegroundColor = ConsoleColor.Red;
			DebugPrint.LogDebug_NoColorSelect(message);
			Console.ResetColor();
		}

		// Token: 0x060003C2 RID: 962 RVA: 0x00003E54 File Offset: 0x00002054
		public static void PrintBlack(string message)
		{
			Console.ForegroundColor = ConsoleColor.Black;
			DebugPrint.LogDebug_NoColorSelect(message);
			Console.ResetColor();
		}

		// Token: 0x060003C3 RID: 963 RVA: 0x0000D678 File Offset: 0x0000B878
		public static string HttpGet(string url)
		{
			string text = "";
			try
			{
				WebClient webClient = new WebClient();
				text = webClient.DownloadString(url);
			}
			catch (WebException)
			{
				text = "error";
			}
			return text;
		}

		// Token: 0x060003C4 RID: 964 RVA: 0x0000D6B8 File Offset: 0x0000B8B8
		public static DateTime GetDateTimeSeconds(string timestamp)
		{
			long num = long.Parse(timestamp);
			long num2 = num * 0x989680L;
			DateTime dateTime = new DateTime(0x7B2, 1, 1, 0, 0, 0, DateTimeKind.Utc);
			long ticks = dateTime.Ticks;
			long num3 = ticks + num2;
			DateTime dateTime2 = new DateTime(num3, DateTimeKind.Utc);
			return dateTime2;
		}

		// Token: 0x060003C5 RID: 965 RVA: 0x0000D704 File Offset: 0x0000B904
		public static string GetCurrentTimeStamp()
		{
			string text = Tool.HttpGet("https://x19mclobt.nie.netease.com/server-time");
			JObject jobject = JObject.Parse(text);
			return (string)jobject["entity"]["current"];
		}

		// Token: 0x060003C6 RID: 966 RVA: 0x0000D744 File Offset: 0x0000B944
		public static string CalculateMD5Hash(string text)
		{
			string text2;
			using (MD5 md = MD5.Create())
			{
				byte[] bytes = Encoding.UTF8.GetBytes(text);
				byte[] array = md.ComputeHash(bytes);
				StringBuilder stringBuilder = new StringBuilder();
				for (int i = 0; i < array.Length; i++)
				{
					stringBuilder.Append(array[i].ToString("x2"));
				}
				text2 = stringBuilder.ToString();
			}
			return text2;
		}

		// Token: 0x060003C7 RID: 967 RVA: 0x0000D7C4 File Offset: 0x0000B9C4
		public static string randomStr_Unicode(int len)
		{
			string[] array = new string[]
			{
				"\ud83d\ude04", "\ud83c\udd92", "\ud83d\ude2d", "\ud83d\udc37", "\ud83c\udf5f", "\ud83d\udc4d", "\ud83d\udeb2", "❌", "\ud83e\uddd4", "\ud83e\udd11",
				"\ud83d\ude1c", "\ud83d\ude0b", "\ud83d\ude21", "\ud83d\ude00", "\ud83d\ude18", "\ud83d\udc4c", "\ud83d\ude04", "\ud83d\ude24", "\ud83d\udc32", "\ud83d\udc7b",
				"\ud83d\udc41", "\ud83d\udd2e", "⚔", "⚽", "\ud83d\udea6", "\ud83c\udf69", "\ud83c\udf59", "\ud83e\udd53", "\ud83c\udf56", "\ud83d\udc71\u200d",
				"\ud83c\udfa0", "₯", "\ud835\udd6c", "\ud835\udd9c", "\ud835\udd8e", "\ud835\udd98", "\ud835\udd8d", "\ud835\udd8b", "\ud835\udd94", "\ud835\udd92",
				"\ud835\udd9e", "\ud835\udd95", "\ud835\udd8e", "\ud835\udd93", "\ud835\udd88", "\ud835\udd8a", "\ud835\udd98", "\ud835\udd6c", "\ud835\udd6d", "\ud835\udd6e",
				"\ud835\udd6f", "\ud835\udd70", "\ud835\udd71", "\ud835\udd72", "\ud835\udd73", "\ud835\udd74", "\ud835\udd75", "\ud835\udd76", "\ud835\udd77", "\ud835\udd78",
				"\ud835\udd79", "\ud835\udd7a", "\ud835\udd7b", "\ud835\udd7c", "\ud835\udd7d", "\ud835\udd7e", "\ud835\udd7f", "\ud835\udd80", "\ud835\udd81", "\ud835\udd82",
				"\ud835\udd83", "\ud835\udd84", "\ud835\udd85"
			};
			return Tool.randomStr(len, array);
		}

		// Token: 0x060003C8 RID: 968 RVA: 0x0000DAB4 File Offset: 0x0000BCB4
		public static string randomMac(string source = null)
		{
			string text = "";
			int num = 0xC;
			if (source != null)
			{
				num = source.Length;
			}
			for (int i = 1; i <= num; i++)
			{
				string text2;
				if (i % 2 != 0)
				{
					text2 = text + Tool.randomStr(1, null);
				}
				else
				{
					string text3;
					if (i != 2)
					{
						if (i != 0xC)
						{
							text3 = text + Tool.randomStr(1, null);
						}
						else
						{
							text3 = text + Tool.randomStr(1, new string[]
							{
								"0", "1", "2", "3", "4", "5", "6", "7", "8", "9",
								"A", "B", "C", "D", "E"
							});
						}
					}
					else
					{
						text3 = text + Tool.randomStr(1, new string[] { "0", "2", "4", "6", "8", "A", "C", "E" });
					}
					text2 = text3;
				}
				text = text2;
			}
			return text.ToUpper();
		}

		// Token: 0x060003C9 RID: 969 RVA: 0x0000DC2C File Offset: 0x0000BE2C
		public static string GetHardwareID()
		{
			string text = string.Empty;
			try
			{
				ManagementObjectSearcher managementObjectSearcher = new ManagementObjectSearcher("SELECT ProcessorId FROM Win32_Processor");
				ManagementObjectCollection managementObjectCollection = managementObjectSearcher.Get();
				using (ManagementObjectCollection.ManagementObjectEnumerator enumerator = managementObjectCollection.GetEnumerator())
				{
					if (enumerator.MoveNext())
					{
						ManagementObject managementObject = (ManagementObject)enumerator.Current;
						text = managementObject["ProcessorId"].ToString();
					}
				}
			}
			catch (Exception ex)
			{
				DebugPrint.LogDebug_NoColorSelect("获取硬件ID出错：" + ex.Message);
			}
			return text;
		}

		// Token: 0x060003CA RID: 970 RVA: 0x00003E67 File Offset: 0x00002067
		public static byte[] Base64Encrypt(byte[] bytes)
		{
			bytes = Encoding.GetEncoding("gbk").GetBytes(Convert.ToBase64String(bytes));
			bytes = bytes.Reverse<byte>().ToArray<byte>();
			return bytes;
		}

		// Token: 0x060003CB RID: 971 RVA: 0x00003E8F File Offset: 0x0000208F
		public static string Base64Decrypt(string str)
		{
			return Encoding.GetEncoding("gbk").GetString(Convert.FromBase64String(str));
		}

		// Token: 0x060003CC RID: 972 RVA: 0x0000DCC8 File Offset: 0x0000BEC8
		public static bool isGoodTitle(string title)
		{
			if (title != null && title.Length != 0)
			{
				string[] array = new string[]
				{
					"ggl", "交流", "client", "360", "mysql", "google", "集", "visual", "idea", "liquid",
					"script", "java", "c#", "sharp", "Azure", "sense", "aquavit", "bounce", "挂", "Wser",
					"Gro", "mix", "zero", "dnspy", "破解", "crack", "sigma", "jello", "flux", "remix",
					"lunar", "hax", "hacker", "minecraft", "godie", "管理员", "x64dbg", "x32dbg", "语言", "命令",
					"system32", "程序", "桌面", "图片", "弊", "端", "bili", "运行", "power", "远程",
					"连接", "server", "jbyte", "gui", "ida", "typora", "studio", "windows", "linux", "admin",
					"直播", "推流", "录制", "live", "收藏", "everything", "installer", "记事本", "XruiDD", "demarcia"
				};
				title = title.ToLower();
				string[] array2 = array;
				foreach (string text in array2)
				{
					if (title.Contains(text.ToLower()))
					{
						return false;
					}
				}
				return true;
			}
			return true;
		}

		// Token: 0x060003CD RID: 973 RVA: 0x0000DFD4 File Offset: 0x0000C1D4
		public static bool isGoodName(string name)
		{
			if (name == null || name.Length == 0)
			{
				return false;
			}
			name = name.ToLower();
			if (Tool.isSystemProcess(name))
			{
				return true;
			}
			if (name.Equals("java") || name.Equals("e") || name.Equals("qq") || name.Equals("tim") || name.StartsWith("lsp"))
			{
				return false;
			}
			if (name.StartsWith("obs") || name.StartsWith("huya"))
			{
				return false;
			}
			if (!name.EndsWith(".dat") && !name.EndsWith(".tmp") && !name.EndsWith(".safe"))
			{
				string[] array = new string[]
				{
					"cheat", "调试", "inject", "hacker", "edit", "dnspy", "xmind", "wechat", "chrome", "installer",
					"SecureFolders", "Locker", "Hide", "sstap", "Lite", "feiq", "v2ray", "studio", "presentermodulemonitor", "presentermodule",
					"ServiceHub", "PerfWatson2", "HipsTray", "DeskTopShare", "wallpaper", "mysql", "usysdiag", "sqlwriter", "jusched", "HipsDaemon",
					"PresentationFontCache", "破解", "盒子", "鼠标", "连点", "工具"
				};
				foreach (string text in array)
				{
					if (name.Contains(text.ToLower()))
					{
						return false;
					}
				}
				return true;
			}
			return false;
		}

		// Token: 0x060003CE RID: 974 RVA: 0x0000E244 File Offset: 0x0000C444
		public static bool isSystemProcess(string name)
		{
			string[] array = new string[]
			{
				"dllhost", "IAStorDataMgrSvc", "fontdrvhost", "WmiPrvSE", "svchost", "crss", "SecurityHealthService", "spoolsv", "dwm", "ctfmon",
				"conhost"
			};
			foreach (string text in array)
			{
				if (name.Equals(text))
				{
					return true;
				}
			}
			return false;
		}

		// Token: 0x060003CF RID: 975 RVA: 0x00003EA7 File Offset: 0x000020A7
		public static string getMainPath()
		{
			if (Tool.mainPath != null && File.Exists(Tool.mainPath))
			{
				return Tool.mainPath;
			}
			return Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
		}

		// Token: 0x060003D0 RID: 976 RVA: 0x0000E2E0 File Offset: 0x0000C4E0
		public static string getMac(string source)
		{
			string text = Tool.randomMac(source).ToUpper();
			if (text.Length > 0xC)
			{
				text = text.Substring(0, 0xC);
			}
			return text;
		}

		// Token: 0x060003D1 RID: 977 RVA: 0x0000E310 File Offset: 0x0000C510
		public static string transform(string encryptedText)
		{
			byte[] array = Convert.FromBase64String(encryptedText);
			string text;
			using (Aes aes = Aes.Create())
			{
				aes.Key = Tool.Key;
				aes.IV = Tool.IV;
				ICryptoTransform cryptoTransform = aes.CreateDecryptor(aes.Key, aes.IV);
				using (MemoryStream memoryStream = new MemoryStream(array))
				{
					using (CryptoStream cryptoStream = new CryptoStream(memoryStream, cryptoTransform, CryptoStreamMode.Read))
					{
						using (StreamReader streamReader = new StreamReader(cryptoStream))
						{
							text = streamReader.ReadToEnd();
						}
					}
				}
			}
			return text;
		}

		// Token: 0x060003D2 RID: 978 RVA: 0x0000E3DC File Offset: 0x0000C5DC
		public static string getLocalIP(string source)
		{
			string text = Tool.randomIP(source).ToUpper();
			if (text.Length > 0xF)
			{
				text = text.Substring(0, 0xF);
			}
			return text;
		}

		// Token: 0x060003D3 RID: 979 RVA: 0x0000E40C File Offset: 0x0000C60C
		public static string randomIP(string source)
		{
			Random random = new Random();
			string text = "";
			for (int i = 0; i <= 3; i++)
			{
				string text2 = random.Next(0, 0xFF).ToString();
				text = ((i >= 3) ? (text + text2.ToString()) : (text + (text2 + ".").ToString()));
			}
			if (Regex.IsMatch(text, "^((25[0-5]|2[0-4]\\d|((1\\d{2})|([1-9]?\\d)))\\.){3}(25[0-5]|2[0-4]\\d|((1\\d{2})|([1-9]?\\d)))$"))
			{
				return text;
			}
			return "";
		}

		// Token: 0x060003D4 RID: 980 RVA: 0x0000E488 File Offset: 0x0000C688
		public static string getDiskCode()
		{
			return Tool.randomStr(8, null).ToUpper();
		}

		// Token: 0x060003D5 RID: 981 RVA: 0x0000E4A8 File Offset: 0x0000C6A8
		public static string getCPUID()
		{
			return Tool.randomStr(0x10, null).ToUpper();
		}

		// Token: 0x060003D6 RID: 982 RVA: 0x0000E4C8 File Offset: 0x0000C6C8
		public static string getGamePath()
		{
			string text;
			try
			{
				if (File.Exists(Tool.getMainPath() + "//PC4399SDK.dll"))
				{
					text = (string)Registry.CurrentUser.OpenSubKey(Tool.HKEY_BASE1).GetValue("DownloadPath");
				}
				else
				{
					text = (string)Registry.CurrentUser.OpenSubKey(Tool.HKEY_BASE).GetValue("DownloadPath");
				}
			}
			catch (Exception)
			{
				text = "C:\\MCLDownload";
			}
			return text;
		}

		// Token: 0x060003D8 RID: 984 RVA: 0x0000E5E4 File Offset: 0x0000C7E4
		public static bool deleteMK(string dest)
		{
			Tool.runCMD("rmdir \"" + dest + "\"", true);
			try
			{
				if (Directory.Exists(dest))
				{
					if (Directory.Exists(dest))
					{
						Directory.Delete(dest);
					}
					return !Directory.Exists(dest);
				}
				if (File.Exists(dest))
				{
					if (File.Exists(dest))
					{
						File.Delete(dest);
					}
					return !File.Exists(dest);
				}
			}
			catch
			{
			}
			return true;
		}

		// Token: 0x060003D9 RID: 985 RVA: 0x0000E668 File Offset: 0x0000C868
		public static string runCMD(string cmdline, bool waiteForExit = true)
		{
			string text = "";
			Process process = new Process();
			process.StartInfo = new ProcessStartInfo
			{
				FileName = "cmd.exe",
				Arguments = "/c " + cmdline,
				UseShellExecute = false,
				RedirectStandardInput = false,
				RedirectStandardOutput = true,
				CreateNoWindow = true
			};
			try
			{
				process.Start();
				if (waiteForExit)
				{
					process.WaitForExit();
				}
				text = process.StandardOutput.ReadToEnd();
			}
			catch
			{
			}
			finally
			{
				process.Kill();
				process.Close();
			}
			return text;
		}

		// Token: 0x060003DA RID: 986 RVA: 0x00003AE0 File Offset: 0x00001CE0
		public static string toString()
		{
			return "";
		}
		
		// Token: 0x060003DD RID: 989 RVA: 0x0000E76C File Offset: 0x0000C96C
		public static bool copyFile(string source, string target)
		{
			if (!File.Exists(source))
			{
				return false;
			}
			try
			{
				if (Tool.deleteFile(target))
				{
					File.Copy(source, target);
				}
			}
			catch
			{
			}
			return false;
		}

		// Token: 0x060003DE RID: 990 RVA: 0x0000E7AC File Offset: 0x0000C9AC
		public static bool deleteFile(string source)
		{
			if (!File.Exists(source))
			{
				return true;
			}
			try
			{
				File.Delete(source);
			}
			catch
			{
			}
			return File.Exists(source);
		}
		
		// Token: 0x060003E1 RID: 993 RVA: 0x0000E938 File Offset: 0x0000CB38
		public static byte[] objectsToBytes(params object[] ktb)
		{
			if (ktb == null)
			{
				return null;
			}
			byte[] array = new byte[0];
			foreach (object obj in ktb)
			{
				byte[] array2 = new byte[0];
				Type type = obj.GetType();
				switch (Type.GetTypeCode(type))
				{
				case TypeCode.Object:
					if (type == typeof(byte[]))
					{
						array2 = (byte[])obj;
					}
					else if (type == typeof(List<uint>))
					{
						List<byte> list = new List<byte>();
						byte[] bytes = BitConverter.GetBytes((ushort)(((List<uint>)obj).Count * 4));
						list.AddRange(array2);
						list.AddRange(bytes);
						foreach (uint num in (obj as List<uint>))
						{
							list.AddRange(BitConverter.GetBytes(num));
						}
						array2 = list.ToArray();
					}
					else if (type == typeof(List<ulong>))
					{
						List<byte> list2 = new List<byte>();
						byte[] bytes2 = BitConverter.GetBytes((ushort)(((List<ulong>)obj).Count * 8));
						list2.AddRange(array2);
						list2.AddRange(bytes2);
						foreach (ulong num2 in (obj as List<ulong>))
						{
							list2.AddRange(BitConverter.GetBytes(num2));
						}
						array2 = list2.ToArray();
					}
					else if (type == typeof(List<long>))
					{
						List<byte> list3 = new List<byte>();
						byte[] bytes3 = BitConverter.GetBytes((ushort)(((List<long>)obj).Count * 8));
						list3.AddRange(array2);
						list3.AddRange(bytes3);
						foreach (long num3 in (obj as List<long>))
						{
							list3.AddRange(BitConverter.GetBytes(num3));
						}
						array2 = list3.ToArray();
					}
					else if (type == typeof(GameDescription))
					{
						string text = JsonConvert.SerializeObject(obj);
						array2 = Tool.objectsToBytes(new object[] { text });
					}
					break;
				case TypeCode.Boolean:
					array2 = BitConverter.GetBytes((bool)obj);
					break;
				case TypeCode.Byte:
					array2 = new byte[] { (byte)obj };
					break;
				case TypeCode.Int16:
					array2 = BitConverter.GetBytes((short)obj);
					break;
				case TypeCode.UInt16:
					array2 = BitConverter.GetBytes((ushort)obj);
					break;
				case TypeCode.Int32:
					array2 = BitConverter.GetBytes((int)obj);
					break;
				case TypeCode.UInt32:
					array2 = BitConverter.GetBytes((uint)obj);
					break;
				case TypeCode.Int64:
					array2 = BitConverter.GetBytes((long)obj);
					break;
				case TypeCode.Double:
					array2 = BitConverter.GetBytes((double)obj);
					break;
				case TypeCode.String:
					array2 = Encoding.UTF8.GetBytes((string)obj);
					array2 = Tool.objectsToBytes(new object[]
					{
						(ushort)array2.Length,
						array2
					});
					break;
				}
				array = array.Concat(array2).ToArray<byte>();
			}
			return array;
		}

		// Token: 0x060003E2 RID: 994 RVA: 0x0000ECC8 File Offset: 0x0000CEC8
		public static string dataEncrypt(string input, string key)
		{
			string text = Tool.ReverseString(input);
			byte[] array;
			using (AesManaged aesManaged = new AesManaged())
			{
				aesManaged.Key = Encoding.UTF8.GetBytes(key);
				aesManaged.Mode = CipherMode.ECB;
				aesManaged.Padding = PaddingMode.PKCS7;
				ICryptoTransform cryptoTransform = aesManaged.CreateEncryptor(aesManaged.Key, aesManaged.IV);
				byte[] bytes = Encoding.UTF8.GetBytes(text);
				array = cryptoTransform.TransformFinalBlock(bytes, 0, bytes.Length);
			}
			return Convert.ToBase64String(array);
		}

		// Token: 0x060003E3 RID: 995 RVA: 0x0000ED54 File Offset: 0x0000CF54
		public static string dataDecrypt(string input, string key)
		{
			byte[] array2;
			using (AesManaged aesManaged = new AesManaged())
			{
				aesManaged.Key = Encoding.UTF8.GetBytes(key);
				aesManaged.Mode = CipherMode.ECB;
				aesManaged.Padding = PaddingMode.PKCS7;
				ICryptoTransform cryptoTransform = aesManaged.CreateDecryptor(aesManaged.Key, aesManaged.IV);
				byte[] array = Convert.FromBase64String(input);
				array2 = cryptoTransform.TransformFinalBlock(array, 0, array.Length);
			}
			return Tool.ReverseString(Encoding.UTF8.GetString(array2));
		}

		// Token: 0x060003E4 RID: 996 RVA: 0x0000EDE0 File Offset: 0x0000CFE0
		private static string ReverseString(string input)
		{
			char[] array = input.ToCharArray();
			Array.Reverse(array);
			return new string(array);
		}

		// Token: 0x0400015B RID: 347
		private static string HKEY_BASE = "SOFTWARE\\Netease\\MCLauncher";

		// Token: 0x0400015C RID: 348
		private static string HKEY_BASE1 = "SOFTWARE\\Netease\\PC4399_MCLauncher";

		// Token: 0x0400015D RID: 349
		private static string mainPath = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);

		// Token: 0x0400015E RID: 350
		private static readonly byte[] Key = new byte[]
		{
			1, 0x23, 0x45, 0x67, 0x89, 0xAB, 0xCD, 0xEF, 1, 0x23,
			0x45, 0x67, 0x89, 0xAB, 0xCD, 0xEF, 1, 0x23, 0x45, 0x67,
			0x89, 0xAB, 0xCD, 0xEF, 1, 0x23, 0x45, 0x67, 0x89, 0xAB,
			0xCD, 0xEF
		};

		// Token: 0x0400015F RID: 351
		private static readonly byte[] IV = new byte[]
		{
			0xFE, 0xDC, 0xBA, 0x98, 0x76, 0x54, 0x32, 0x10, 0xFE, 0xDC,
			0xBA, 0x98, 0x76, 0x54, 0x32, 0x10
		};

		// Token: 0x0200006B RID: 107
		internal class SocketController
		{
			// Token: 0x060003E7 RID: 999 RVA: 0x0000EE6C File Offset: 0x0000D06C
			public bool StartRPC(int port)
			{
				if (this.hookSocketServer == null)
				{
					bool flag;
					try
					{
						this.hookSocketServer = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
						this.hookSocketServer.Bind(new IPEndPoint(IPAddress.Parse("127.0.0.1"), port));
						this.hookSocketServer.Listen(0x64);
						Thread thread = new Thread(new ThreadStart(this.ReceiveHookTCP));
						thread.Start();
						return true;
					}
					catch (Exception)
					{
						flag = false;
					}
					return flag;
				}
				return true;
			}

			// Token: 0x060003E8 RID: 1000 RVA: 0x0000EEEC File Offset: 0x0000D0EC
			public void ReceiveHookTCP()
			{
				for (;;)
				{
					Thread.Sleep(1);
					try
					{
						this.hookSocketClient = this.hookSocketServer.Accept();
						Thread thread = new Thread(new ThreadStart(this.method_0));
						thread.Start();
					}
					catch (Exception)
					{
					}
				}
			}

			// Token: 0x060003E9 RID: 1001 RVA: 0x0000EF3C File Offset: 0x0000D13C
			private void method_0()
			{
				for (;;)
				{
					Thread.Sleep(1);
					try
					{
						if (this.hookSocketServer != null && this.hookSocketClient != null)
						{
							byte[] array = new byte[this.hookSocketServer.ReceiveBufferSize];
							this.hookSocketClient.Receive(array);
							continue;
						}
					}
					catch (Exception)
					{
						continue;
					}
					break;
				}
			}

			// Token: 0x04000160 RID: 352
			private Socket hookSocketServer;

			// Token: 0x04000161 RID: 353
			private Socket hookSocketClient;
		}
	}
}
