using System;

namespace Mcl.Core.Dotnetdetour
{
	[AttributeUsage(AttributeTargets.Parameter)]
	public class RememberTypeAttribute : Attribute
	{
		public RememberTypeAttribute(string fullName = null, bool isGeneric = false)
		{
			this.TypeFullNameOrNull = fullName;
			this.IsGeneric = isGeneric;
		}

		// (get) Token: 0x06000036 RID: 54 RVA: 0x000032AF File Offset: 0x000014AF
		// (set) Token: 0x06000037 RID: 55 RVA: 0x000032B7 File Offset: 0x000014B7
		public string TypeFullNameOrNull { get; private set; }

		// (get) Token: 0x06000038 RID: 56 RVA: 0x000032C0 File Offset: 0x000014C0
		// (set) Token: 0x06000039 RID: 57 RVA: 0x000032C8 File Offset: 0x000014C8
		public bool IsGeneric { get; private set; }
	}
}
