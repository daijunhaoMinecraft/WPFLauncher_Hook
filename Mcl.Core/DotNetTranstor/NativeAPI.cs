using System;
using System.Runtime.InteropServices;

namespace DotNetTranstor
{
	// Token: 0x02000011 RID: 17
	public class NativeAPI
	{
		// Token: 0x06000032 RID: 50
		[DllImport("kernel32")]
		public static extern bool VirtualProtect(IntPtr lpAddress, uint dwSize, Protection flNewProtect, out uint lpflOldProtect);
	}
}
