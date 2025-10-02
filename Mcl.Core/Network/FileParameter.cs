using System;
using System.IO;

namespace Mcl.Core.Network
{
	// Token: 0x02000006 RID: 6
	public class FileParameter
	{
		// Token: 0x0600000B RID: 11 RVA: 0x000021D8 File Offset: 0x000003D8
		public static FileParameter Create(string name, byte[] data, string filename, string contentType)
		{
			long num = (long)data.Length;
			return new FileParameter
			{
				Writer = delegate(Stream s)
				{
					s.Write(data, 0, data.Length);
				},
				FileName = filename,
				ContentType = contentType,
				ContentLength = num,
				Name = name
			};
		}

		// Token: 0x0600000C RID: 12 RVA: 0x00002238 File Offset: 0x00000438
		public static FileParameter Create(string name, byte[] data, string filename)
		{
			return FileParameter.Create(name, data, filename, null);
		}

		// Token: 0x17000001 RID: 1
		// (get) Token: 0x0600000D RID: 13 RVA: 0x00002253 File Offset: 0x00000453
		// (set) Token: 0x0600000E RID: 14 RVA: 0x0000225B File Offset: 0x0000045B
		public long ContentLength { get; set; }

		// Token: 0x17000002 RID: 2
		// (get) Token: 0x0600000F RID: 15 RVA: 0x00002264 File Offset: 0x00000464
		// (set) Token: 0x06000010 RID: 16 RVA: 0x0000226C File Offset: 0x0000046C
		public Action<Stream> Writer { get; set; }

		// Token: 0x17000003 RID: 3
		// (get) Token: 0x06000011 RID: 17 RVA: 0x00002275 File Offset: 0x00000475
		// (set) Token: 0x06000012 RID: 18 RVA: 0x0000227D File Offset: 0x0000047D
		public string FileName { get; set; }

		// Token: 0x17000004 RID: 4
		// (get) Token: 0x06000013 RID: 19 RVA: 0x00002286 File Offset: 0x00000486
		// (set) Token: 0x06000014 RID: 20 RVA: 0x0000228E File Offset: 0x0000048E
		public string ContentType { get; set; }

		// Token: 0x17000005 RID: 5
		// (get) Token: 0x06000015 RID: 21 RVA: 0x00002297 File Offset: 0x00000497
		// (set) Token: 0x06000016 RID: 22 RVA: 0x0000229F File Offset: 0x0000049F
		public string Name { get; set; }
	}
}
