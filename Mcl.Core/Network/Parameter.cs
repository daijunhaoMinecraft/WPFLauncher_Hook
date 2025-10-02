using System;

namespace Mcl.Core.Network
{
	// Token: 0x02000017 RID: 23
	public class Parameter
	{
		// Token: 0x17000075 RID: 117
		// (get) Token: 0x0600014D RID: 333 RVA: 0x000053E9 File Offset: 0x000035E9
		// (set) Token: 0x0600014E RID: 334 RVA: 0x000053F1 File Offset: 0x000035F1
		public string Name { get; set; }

		// Token: 0x17000076 RID: 118
		// (get) Token: 0x0600014F RID: 335 RVA: 0x000053FA File Offset: 0x000035FA
		// (set) Token: 0x06000150 RID: 336 RVA: 0x00005402 File Offset: 0x00003602
		public object Value { get; set; }

		// Token: 0x17000077 RID: 119
		// (get) Token: 0x06000151 RID: 337 RVA: 0x0000540B File Offset: 0x0000360B
		// (set) Token: 0x06000152 RID: 338 RVA: 0x00005413 File Offset: 0x00003613
		public ParameterType Type { get; set; }

		// Token: 0x17000078 RID: 120
		// (get) Token: 0x06000153 RID: 339 RVA: 0x0000541C File Offset: 0x0000361C
		// (set) Token: 0x06000154 RID: 340 RVA: 0x00005424 File Offset: 0x00003624
		public string ContentType { get; set; }

		// Token: 0x06000155 RID: 341 RVA: 0x00005430 File Offset: 0x00003630
		public override string ToString()
		{
			return string.Format("{0}={1}", this.Name, this.Value);
		}
	}
}
