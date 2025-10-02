using System;

namespace DotNetTranstor
{
	// Token: 0x02000013 RID: 19
	[AttributeUsage(AttributeTargets.Parameter)]
	public class RememberTypeAttribute : Attribute
	{
		// Token: 0x06000035 RID: 53 RVA: 0x00003295 File Offset: 0x00001495
		public RememberTypeAttribute(string fullName = null, bool isGeneric = false)
		{
			this.TypeFullNameOrNull = fullName;
			this.IsGeneric = isGeneric;
		}

		// Token: 0x17000005 RID: 5
		// (get) Token: 0x06000036 RID: 54 RVA: 0x000032AF File Offset: 0x000014AF
		// (set) Token: 0x06000037 RID: 55 RVA: 0x000032B7 File Offset: 0x000014B7
		public string TypeFullNameOrNull { get; private set; }

		// Token: 0x17000006 RID: 6
		// (get) Token: 0x06000038 RID: 56 RVA: 0x000032C0 File Offset: 0x000014C0
		// (set) Token: 0x06000039 RID: 57 RVA: 0x000032C8 File Offset: 0x000014C8
		public bool IsGeneric { get; private set; }
	}
}
