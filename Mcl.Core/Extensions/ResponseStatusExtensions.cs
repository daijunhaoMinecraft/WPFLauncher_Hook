using System;
using System.Net;
using Mcl.Core.Network;

namespace Mcl.Core.Extensions
{
	// Token: 0x02000025 RID: 37
	public static class ResponseStatusExtensions
	{
		// Token: 0x0600026F RID: 623 RVA: 0x00006088 File Offset: 0x00004288
		public static WebException ToWebException(this ResponseStatus responseStatus)
		{
			switch (responseStatus)
			{
			case ResponseStatus.None:
				return new WebException("The request could not be processed.", WebExceptionStatus.ServerProtocolViolation);
			case ResponseStatus.Error:
				return new WebException("An error occurred while processing the request.", WebExceptionStatus.ServerProtocolViolation);
			case ResponseStatus.TimedOut:
				return new WebException("The request timed-out.", WebExceptionStatus.Timeout);
			case ResponseStatus.Aborted:
				return new WebException("The request was aborted.", WebExceptionStatus.Timeout);
			}
			throw new ArgumentOutOfRangeException("responseStatus");
		}
	}
}
