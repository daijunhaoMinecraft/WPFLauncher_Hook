using System;
using System.Runtime.InteropServices;

namespace Mcl.Core.Dotnetdetour
{
	public class NativeAPI
	{
		[DllImport("kernel32")]
		public static extern bool VirtualProtect(IntPtr lpAddress, uint dwSize, Protection flNewProtect, out uint lpflOldProtect);
	}
}
