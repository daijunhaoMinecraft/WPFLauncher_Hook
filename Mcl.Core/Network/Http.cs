using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Cache;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Mcl.Core.Extensions;
using Mcl.Core.Network.Interface;

namespace Mcl.Core.Network
{
	// Token: 0x02000007 RID: 7
	public class Http : IHttp, IHttpFactory
	{
		// Token: 0x06000018 RID: 24 RVA: 0x000022A8 File Offset: 0x000004A8
		public HttpWebRequest DeleteAsync(Action<HttpResponse> action)
		{
			return this.GetStyleMethodInternalAsync("DELETE", action);
		}

		// Token: 0x06000019 RID: 25 RVA: 0x000022B6 File Offset: 0x000004B6
		public HttpWebRequest GetAsync(Action<HttpResponse> action)
		{
			return this.GetStyleMethodInternalAsync("GET", action);
		}

		// Token: 0x0600001A RID: 26 RVA: 0x000022C4 File Offset: 0x000004C4
		public HttpWebRequest HeadAsync(Action<HttpResponse> action)
		{
			return this.GetStyleMethodInternalAsync("HEAD", action);
		}

		// Token: 0x0600001B RID: 27 RVA: 0x000022D2 File Offset: 0x000004D2
		public HttpWebRequest PostAsync(Action<HttpResponse> action)
		{
			return this.PutPostInternalAsync("POST", action);
		}

		// Token: 0x0600001C RID: 28 RVA: 0x000022E0 File Offset: 0x000004E0
		public HttpWebRequest PutAsync(Action<HttpResponse> action)
		{
			return this.PutPostInternalAsync("PUT", action);
		}

		// Token: 0x0600001D RID: 29 RVA: 0x000022EE File Offset: 0x000004EE
		public HttpWebRequest PatchAsync(Action<HttpResponse> action)
		{
			return this.PutPostInternalAsync("PATCH", action);
		}

		// Token: 0x0600001E RID: 30 RVA: 0x000022FC File Offset: 0x000004FC
		public HttpWebRequest AsPostAsync(Action<HttpResponse> action, string httpMethod)
		{
			return this.PutPostInternalAsync(httpMethod.ToUpperInvariant(), action);
		}

		// Token: 0x0600001F RID: 31 RVA: 0x0000231C File Offset: 0x0000051C
		public HttpWebRequest AsGetAsync(Action<HttpResponse> action, string httpMethod)
		{
			return this.GetStyleMethodInternalAsync(httpMethod.ToUpperInvariant(), action);
		}

		// Token: 0x06000020 RID: 32 RVA: 0x0000233C File Offset: 0x0000053C
		protected virtual HttpWebRequest ConfigureAsyncWebRequest(string method, Uri url)
		{
			return this.ConfigureWebRequest(method, url);
		}

		// Token: 0x06000021 RID: 33 RVA: 0x00002358 File Offset: 0x00000558
		private void SetTimeout(IAsyncResult asyncResult, TimeOutState timeOutState)
		{
			bool flag = this.Timeout != 0;
			if (flag)
			{
				ThreadPool.RegisterWaitForSingleObject(asyncResult.AsyncWaitHandle, new WaitOrTimerCallback(Http.TimeoutCallback), timeOutState, this.Timeout, true);
			}
		}

		// Token: 0x06000022 RID: 34 RVA: 0x00002398 File Offset: 0x00000598
		private static void TimeoutCallback(object state, bool timedOut)
		{
			bool flag = !timedOut;
			if (!flag)
			{
				TimeOutState timeOutState = state as TimeOutState;
				bool flag2 = timeOutState == null;
				if (!flag2)
				{
					TimeOutState timeOutState2 = timeOutState;
					lock (timeOutState2)
					{
						timeOutState.TimedOut = true;
					}
					HttpWebRequest request = timeOutState.Request;
					if (request != null)
					{
						request.Abort();
					}
				}
			}
		}

		// Token: 0x06000023 RID: 35 RVA: 0x0000240C File Offset: 0x0000060C
		private static void GetRawResponseAsync(IAsyncResult result, Action<HttpWebResponse> callback)
		{
			HttpWebResponse httpWebResponse = null;
			try
			{
				HttpWebRequest httpWebRequest = (HttpWebRequest)result.AsyncState;
				httpWebResponse = httpWebRequest.EndGetResponse(result) as HttpWebResponse;
			}
			catch (WebException ex)
			{
				bool flag = ex.Status == WebExceptionStatus.RequestCanceled;
				if (flag)
				{
					throw;
				}
				bool flag2 = ex.Response is HttpWebResponse;
				if (!flag2)
				{
					throw;
				}
				httpWebResponse = ex.Response as HttpWebResponse;
			}
			callback(httpWebResponse);
			if (httpWebResponse != null)
			{
				httpWebResponse.Close();
			}
		}

		// Token: 0x06000024 RID: 36 RVA: 0x0000249C File Offset: 0x0000069C
		private HttpResponse CreateErrorResponse(Exception ex)
		{
			HttpResponse httpResponse = new HttpResponse();
			WebException ex2 = ex as WebException;
			bool flag = ex2 != null && ex2.Status == WebExceptionStatus.RequestCanceled;
			HttpResponse httpResponse2;
			if (flag)
			{
				httpResponse.ResponseStatus = (this.timeoutState.TimedOut ? ResponseStatus.TimedOut : ResponseStatus.Aborted);
				httpResponse2 = httpResponse;
			}
			else
			{
				httpResponse.ErrorMessage = ex.Message;
				httpResponse.ErrorException = ex;
				httpResponse.ResponseStatus = ResponseStatus.Error;
				httpResponse2 = httpResponse;
			}
			return httpResponse2;
		}

		// Token: 0x06000025 RID: 37 RVA: 0x0000250C File Offset: 0x0000070C
		private void ResponseCallback(IAsyncResult result, Action<HttpResponse> callback)
		{
			HttpResponse response = new HttpResponse
			{
				ResponseStatus = ResponseStatus.None
			};
			try
			{
				bool timedOut = this.timeoutState.TimedOut;
				if (timedOut)
				{
					response.ResponseStatus = ResponseStatus.TimedOut;
					Http.ExecuteCallback(response, callback);
				}
				else
				{
					Http.GetRawResponseAsync(result, delegate(HttpWebResponse webResponse)
					{
						try
						{
							this.ExtractResponseData(response, webResponse);
							Http.ExecuteCallback(response, callback);
						}
						catch
						{
						}
					});
				}
			}
			catch (Exception ex)
			{
				Http.ExecuteCallback(this.CreateErrorResponse(ex), callback);
			}
		}

		// Token: 0x06000026 RID: 38 RVA: 0x000025B4 File Offset: 0x000007B4
		private static void ExecuteCallback(HttpResponse response, Action<HttpResponse> callback)
		{
			Http.PopulateErrorForIncompleteResponse(response);
			callback(response);
		}

		// Token: 0x06000027 RID: 39 RVA: 0x000025C8 File Offset: 0x000007C8
		private static void PopulateErrorForIncompleteResponse(HttpResponse response)
		{
			bool flag = response.ResponseStatus != ResponseStatus.Completed && response.ErrorException == null;
			if (flag)
			{
				response.ErrorException = response.ResponseStatus.ToWebException();
				response.ErrorMessage = response.ErrorException.Message;
			}
		}

		// Token: 0x06000028 RID: 40 RVA: 0x00002618 File Offset: 0x00000818
		private long CalculateContentLength()
		{
			bool flag = this.RequestBodyBytes != null;
			long num;
			if (flag)
			{
				num = (long)this.RequestBodyBytes.Length;
			}
			else
			{
				bool flag2 = !this.HasFiles && !this.AlwaysMultipartFormData;
				if (flag2)
				{
					num = (long)this.Encoding.GetByteCount(this.RequestBody);
				}
				else
				{
					long num2 = 0L;
					foreach (HttpFile httpFile in this.Files)
					{
						num2 += (long)this.Encoding.GetByteCount(Http.GetMultipartFileHeader(httpFile));
						num2 += httpFile.ContentLength;
						num2 += (long)this.Encoding.GetByteCount("\r\n");
					}
					num2 = this.Parameters.Aggregate(num2, (long current, HttpParameter param) => current + (long)this.Encoding.GetByteCount(this.GetMultipartFormData(param)));
					num2 += (long)this.Encoding.GetByteCount(Http.GetMultipartFooter());
					num = num2;
				}
			}
			return num;
		}

		// Token: 0x06000029 RID: 41 RVA: 0x00002720 File Offset: 0x00000920
		private void RequestStreamCallback(IAsyncResult result, Action<HttpResponse> callback, HttpWebRequest httpWebRequest)
		{
			HttpWebRequest httpWebRequest2 = (HttpWebRequest)result.AsyncState;
			bool timedOut = this.timeoutState.TimedOut;
			if (timedOut)
			{
				HttpResponse httpResponse = new HttpResponse
				{
					ResponseStatus = ResponseStatus.TimedOut
				};
				Http.ExecuteCallback(httpResponse, callback);
			}
			else
			{
				try
				{
					using (Stream stream = httpWebRequest2.EndGetRequestStream(result))
					{
						bool flag = this.HasFiles || this.AlwaysMultipartFormData;
						if (flag)
						{
							this.WriteMultipartFormData(stream);
						}
						else
						{
							bool flag2 = this.RequestBodyBytes != null;
							if (flag2)
							{
								stream.Write(this.RequestBodyBytes, 0, this.RequestBodyBytes.Length);
							}
							else
							{
								bool flag3 = this.RequestBody != null;
								if (flag3)
								{
									this.WriteStringTo(stream, this.RequestBody);
								}
							}
						}
					}
				}
				catch (Exception ex)
				{
					HttpResponse httpResponse2 = this.CreateErrorResponse(ex);
					httpResponse2.ProtocolVersion = httpWebRequest.ProtocolVersion;
					Http.ExecuteCallback(httpResponse2, callback);
					return;
				}
				IAsyncResult asyncResult = httpWebRequest2.BeginGetResponse(delegate(IAsyncResult r)
				{
					this.ResponseCallback(r, callback);
				}, httpWebRequest2);
				this.SetTimeout(asyncResult, this.timeoutState);
			}
		}

		// Token: 0x0600002A RID: 42 RVA: 0x00002878 File Offset: 0x00000A78
		private void WriteRequestBodyAsync(HttpWebRequest webRequest, Action<HttpResponse> callback)
		{
			this.timeoutState = new TimeOutState
			{
				Request = webRequest
			};
			bool flag = this.HasBody || this.HasFiles || this.AlwaysMultipartFormData;
			IAsyncResult asyncResult;
			if (flag)
			{
				webRequest.ContentLength = this.CalculateContentLength();
				asyncResult = webRequest.BeginGetRequestStream(delegate(IAsyncResult result)
				{
					this.RequestStreamCallback(result, callback, webRequest);
				}, webRequest);
			}
			else
			{
				asyncResult = webRequest.BeginGetResponse(delegate(IAsyncResult r)
				{
					this.ResponseCallback(r, callback);
				}, webRequest);
			}
			this.SetTimeout(asyncResult, this.timeoutState);
		}

		// Token: 0x0600002B RID: 43 RVA: 0x00002938 File Offset: 0x00000B38
		private HttpWebRequest PutPostInternalAsync(string method, Action<HttpResponse> callback)
		{
			HttpWebRequest httpWebRequest = null;
			try
			{
				httpWebRequest = this.ConfigureAsyncWebRequest(method, this.Url);
				this.PreparePostBody(httpWebRequest);
				this.WriteRequestBodyAsync(httpWebRequest, callback);
			}
			catch (Exception ex)
			{
				Http.ExecuteCallback(this.CreateErrorResponse(ex), callback);
			}
			return httpWebRequest;
		}

		// Token: 0x0600002C RID: 44 RVA: 0x00002994 File Offset: 0x00000B94
		private HttpWebRequest GetStyleMethodInternalAsync(string method, Action<HttpResponse> callback)
		{
			HttpWebRequest httpWebRequest = null;
			try
			{
				Uri url = this.Url;
				httpWebRequest = this.ConfigureAsyncWebRequest(method, url);
				bool flag = this.HasBody && method == "DELETE";
				if (flag)
				{
					httpWebRequest.ContentType = this.RequestContentType;
					this.WriteRequestBodyAsync(httpWebRequest, callback);
				}
				else
				{
					this.timeoutState = new TimeOutState
					{
						Request = httpWebRequest
					};
					IAsyncResult asyncResult = httpWebRequest.BeginGetResponse(delegate(IAsyncResult result)
					{
						this.ResponseCallback(result, callback);
					}, httpWebRequest);
					this.SetTimeout(asyncResult, this.timeoutState);
				}
			}
			catch (Exception ex)
			{
				Http.ExecuteCallback(this.CreateErrorResponse(ex), callback);
			}
			return httpWebRequest;
		}

		// Token: 0x0600002D RID: 45 RVA: 0x00002A70 File Offset: 0x00000C70
		public Http()
		{
			this.Headers = new List<HttpHeader>();
			this.Files = new List<HttpFile>();
			this.Parameters = new List<HttpParameter>();
			this.Cookies = new List<HttpCookie>();
			this.restrictedHeaderActions = new Dictionary<string, Action<HttpWebRequest, string>>(StringComparer.OrdinalIgnoreCase);
			this.AddSharedHeaderActions();
			this.AddSyncHeaderActions();
		}

		// Token: 0x17000006 RID: 6
		// (get) Token: 0x0600002E RID: 46 RVA: 0x00002ADA File Offset: 0x00000CDA
		protected bool HasParameters
		{
			get
			{
				return this.Parameters.Any<HttpParameter>();
			}
		}

		// Token: 0x17000007 RID: 7
		// (get) Token: 0x0600002F RID: 47 RVA: 0x00002AE7 File Offset: 0x00000CE7
		protected bool HasCookies
		{
			get
			{
				return this.Cookies.Any<HttpCookie>();
			}
		}

		// Token: 0x17000008 RID: 8
		// (get) Token: 0x06000030 RID: 48 RVA: 0x00002AF4 File Offset: 0x00000CF4
		protected bool HasBody
		{
			get
			{
				return this.RequestBodyBytes != null || !string.IsNullOrEmpty(this.RequestBody);
			}
		}

		// Token: 0x17000009 RID: 9
		// (get) Token: 0x06000031 RID: 49 RVA: 0x00002B0F File Offset: 0x00000D0F
		protected bool HasFiles
		{
			get
			{
				return this.Files.Any<HttpFile>();
			}
		}

		// Token: 0x1700000A RID: 10
		// (get) Token: 0x06000032 RID: 50 RVA: 0x00002B1C File Offset: 0x00000D1C
		// (set) Token: 0x06000033 RID: 51 RVA: 0x00002B24 File Offset: 0x00000D24
		public bool AlwaysMultipartFormData { get; set; }

		// Token: 0x1700000B RID: 11
		// (get) Token: 0x06000034 RID: 52 RVA: 0x00002B2D File Offset: 0x00000D2D
		// (set) Token: 0x06000035 RID: 53 RVA: 0x00002B35 File Offset: 0x00000D35
		public string UserAgent { get; set; }

		// Token: 0x1700000C RID: 12
		// (get) Token: 0x06000036 RID: 54 RVA: 0x00002B3E File Offset: 0x00000D3E
		// (set) Token: 0x06000037 RID: 55 RVA: 0x00002B46 File Offset: 0x00000D46
		public int Timeout { get; set; }

		// Token: 0x1700000D RID: 13
		// (get) Token: 0x06000038 RID: 56 RVA: 0x00002B4F File Offset: 0x00000D4F
		// (set) Token: 0x06000039 RID: 57 RVA: 0x00002B57 File Offset: 0x00000D57
		public int ReadWriteTimeout { get; set; }

		// Token: 0x1700000E RID: 14
		// (get) Token: 0x0600003A RID: 58 RVA: 0x00002B60 File Offset: 0x00000D60
		// (set) Token: 0x0600003B RID: 59 RVA: 0x00002B68 File Offset: 0x00000D68
		public ICredentials Credentials { get; set; }

		// Token: 0x1700000F RID: 15
		// (get) Token: 0x0600003C RID: 60 RVA: 0x00002B71 File Offset: 0x00000D71
		// (set) Token: 0x0600003D RID: 61 RVA: 0x00002B79 File Offset: 0x00000D79
		public CookieContainer CookieContainer { get; set; }

		// Token: 0x17000010 RID: 16
		// (get) Token: 0x0600003E RID: 62 RVA: 0x00002B82 File Offset: 0x00000D82
		// (set) Token: 0x0600003F RID: 63 RVA: 0x00002B8A File Offset: 0x00000D8A
		public Action<Stream> ResponseWriter { get; set; }

		// Token: 0x17000011 RID: 17
		// (get) Token: 0x06000040 RID: 64 RVA: 0x00002B93 File Offset: 0x00000D93
		public IList<HttpFile> Files { get; }

		// Token: 0x17000012 RID: 18
		// (get) Token: 0x06000041 RID: 65 RVA: 0x00002B9B File Offset: 0x00000D9B
		// (set) Token: 0x06000042 RID: 66 RVA: 0x00002BA3 File Offset: 0x00000DA3
		public bool FollowRedirects { get; set; }

		// Token: 0x17000013 RID: 19
		// (get) Token: 0x06000043 RID: 67 RVA: 0x00002BAC File Offset: 0x00000DAC
		// (set) Token: 0x06000044 RID: 68 RVA: 0x00002BB4 File Offset: 0x00000DB4
		public X509CertificateCollection ClientCertificates { get; set; }

		// Token: 0x17000014 RID: 20
		// (get) Token: 0x06000045 RID: 69 RVA: 0x00002BBD File Offset: 0x00000DBD
		// (set) Token: 0x06000046 RID: 70 RVA: 0x00002BC5 File Offset: 0x00000DC5
		public int? MaxRedirects { get; set; }

		// Token: 0x17000015 RID: 21
		// (get) Token: 0x06000047 RID: 71 RVA: 0x00002BCE File Offset: 0x00000DCE
		// (set) Token: 0x06000048 RID: 72 RVA: 0x00002BD6 File Offset: 0x00000DD6
		public bool UseDefaultCredentials { get; set; }

		// Token: 0x17000016 RID: 22
		// (get) Token: 0x06000049 RID: 73 RVA: 0x00002BDF File Offset: 0x00000DDF
		// (set) Token: 0x0600004A RID: 74 RVA: 0x00002BE7 File Offset: 0x00000DE7
		public Encoding Encoding { get; set; } = Encoding.UTF8;

		// Token: 0x17000017 RID: 23
		// (get) Token: 0x0600004B RID: 75 RVA: 0x00002BF0 File Offset: 0x00000DF0
		public IList<HttpHeader> Headers { get; }

		// Token: 0x17000018 RID: 24
		// (get) Token: 0x0600004C RID: 76 RVA: 0x00002BF8 File Offset: 0x00000DF8
		public IList<HttpParameter> Parameters { get; }

		// Token: 0x17000019 RID: 25
		// (get) Token: 0x0600004D RID: 77 RVA: 0x00002C00 File Offset: 0x00000E00
		public IList<HttpCookie> Cookies { get; }

		// Token: 0x1700001A RID: 26
		// (get) Token: 0x0600004E RID: 78 RVA: 0x00002C08 File Offset: 0x00000E08
		// (set) Token: 0x0600004F RID: 79 RVA: 0x00002C10 File Offset: 0x00000E10
		public string RequestBody { get; set; }

		// Token: 0x1700001B RID: 27
		// (get) Token: 0x06000050 RID: 80 RVA: 0x00002C19 File Offset: 0x00000E19
		// (set) Token: 0x06000051 RID: 81 RVA: 0x00002C21 File Offset: 0x00000E21
		public string RequestContentType { get; set; }

		// Token: 0x1700001C RID: 28
		// (get) Token: 0x06000052 RID: 82 RVA: 0x00002C2A File Offset: 0x00000E2A
		// (set) Token: 0x06000053 RID: 83 RVA: 0x00002C32 File Offset: 0x00000E32
		public byte[] RequestBodyBytes { get; set; }

		// Token: 0x1700001D RID: 29
		// (get) Token: 0x06000054 RID: 84 RVA: 0x00002C3B File Offset: 0x00000E3B
		// (set) Token: 0x06000055 RID: 85 RVA: 0x00002C43 File Offset: 0x00000E43
		public Uri Url { get; set; }

		// Token: 0x1700001E RID: 30
		// (get) Token: 0x06000056 RID: 86 RVA: 0x00002C4C File Offset: 0x00000E4C
		// (set) Token: 0x06000057 RID: 87 RVA: 0x00002C54 File Offset: 0x00000E54
		public bool PreAuthenticate { get; set; }

		// Token: 0x1700001F RID: 31
		// (get) Token: 0x06000058 RID: 88 RVA: 0x00002C5D File Offset: 0x00000E5D
		// (set) Token: 0x06000059 RID: 89 RVA: 0x00002C65 File Offset: 0x00000E65
		public RequestCachePolicy CachePolicy { get; set; }

		// Token: 0x0600005A RID: 90 RVA: 0x00002C70 File Offset: 0x00000E70
		public IHttp Create()
		{
			return new Http();
		}

		// Token: 0x0600005B RID: 91 RVA: 0x00002C88 File Offset: 0x00000E88
		protected virtual HttpWebRequest CreateWebRequest(Uri url)
		{
			return (HttpWebRequest)WebRequest.Create(url);
		}

		// Token: 0x0600005C RID: 92 RVA: 0x00002CA8 File Offset: 0x00000EA8
		private void AddSyncHeaderActions()
		{
			this.restrictedHeaderActions.Add("Connection", delegate(HttpWebRequest r, string v)
			{
				r.KeepAlive = v.ToLower().Contains("keep-alive");
			});
			this.restrictedHeaderActions.Add("Content-Length", delegate(HttpWebRequest r, string v)
			{
				r.ContentLength = Convert.ToInt64(v);
			});
			this.restrictedHeaderActions.Add("Expect", delegate(HttpWebRequest r, string v)
			{
				r.Expect = v;
			});
			this.restrictedHeaderActions.Add("If-Modified-Since", delegate(HttpWebRequest r, string v)
			{
				r.IfModifiedSince = Convert.ToDateTime(v, CultureInfo.InvariantCulture);
			});
			this.restrictedHeaderActions.Add("Referer", delegate(HttpWebRequest r, string v)
			{
				r.Referer = v;
			});
			this.restrictedHeaderActions.Add("Transfer-Encoding", delegate(HttpWebRequest r, string v)
			{
				r.TransferEncoding = v;
				r.SendChunked = true;
			});
			this.restrictedHeaderActions.Add("User-Agent", delegate(HttpWebRequest r, string v)
			{
				r.UserAgent = v;
			});
		}

		// Token: 0x0600005D RID: 93 RVA: 0x00002E08 File Offset: 0x00001008
		private void AddSharedHeaderActions()
		{
			this.restrictedHeaderActions.Add("Accept", delegate(HttpWebRequest r, string v)
			{
				r.Accept = v;
			});
			this.restrictedHeaderActions.Add("Content-Type", delegate(HttpWebRequest r, string v)
			{
				r.ContentType = v;
			});
			this.restrictedHeaderActions.Add("Date", delegate(HttpWebRequest r, string v)
			{
				DateTime dateTime;
				bool flag = DateTime.TryParse(v, out dateTime);
				if (flag)
				{
					r.Date = dateTime;
				}
			});
			this.restrictedHeaderActions.Add("Host", delegate(HttpWebRequest r, string v)
			{
				r.Host = v;
			});
			this.restrictedHeaderActions.Add("Range", new Action<HttpWebRequest, string>(Http.AddRange));
		}

		// Token: 0x0600005E RID: 94 RVA: 0x00002EF4 File Offset: 0x000010F4
		private static string GetMultipartFormContentType()
		{
			return string.Format("multipart/form-data; boundary={0}", "-----------------------------28947758029299");
		}

		// Token: 0x0600005F RID: 95 RVA: 0x00002F18 File Offset: 0x00001118
		private static string GetMultipartFileHeader(HttpFile file)
		{
			return string.Format("--{0}{4}Content-Disposition: form-data; name=\"{1}\"; filename=\"{2}\"{4}Content-Type: {3}{4}{4}", new object[]
			{
				"-----------------------------28947758029299",
				file.Name,
				file.FileName,
				file.ContentType ?? "application/octet-stream",
				"\r\n"
			});
		}

		// Token: 0x06000060 RID: 96 RVA: 0x00002F70 File Offset: 0x00001170
		private string GetMultipartFormData(HttpParameter param)
		{
			string text = ((param.Name == this.RequestContentType) ? "--{0}{3}Content-Type: {4}{3}Content-Disposition: form-data; name=\"{1}\"{3}{3}{2}{3}" : "--{0}{3}Content-Disposition: form-data; name=\"{1}\"{3}{3}{2}{3}");
			return string.Format(text, new object[] { "-----------------------------28947758029299", param.Name, param.Value, "\r\n", param.ContentType });
		}

		// Token: 0x06000061 RID: 97 RVA: 0x00002FDC File Offset: 0x000011DC
		private static string GetMultipartFooter()
		{
			return string.Format("--{0}--{1}", "-----------------------------28947758029299", "\r\n");
		}

		// Token: 0x06000062 RID: 98 RVA: 0x00003004 File Offset: 0x00001204
		private void AppendHeaders(HttpWebRequest webRequest)
		{
			foreach (HttpHeader httpHeader in this.Headers)
			{
				bool flag = this.restrictedHeaderActions.ContainsKey(httpHeader.Name);
				if (flag)
				{
					this.restrictedHeaderActions[httpHeader.Name](webRequest, httpHeader.Value);
				}
				else
				{
					webRequest.Headers.Add(httpHeader.Name, httpHeader.Value);
				}
			}
		}

		// Token: 0x06000063 RID: 99 RVA: 0x000030A0 File Offset: 0x000012A0
		private void AppendCookies(HttpWebRequest webRequest)
		{
			webRequest.CookieContainer = this.CookieContainer ?? new CookieContainer();
			foreach (HttpCookie httpCookie in this.Cookies)
			{
				Cookie cookie = new Cookie
				{
					Name = httpCookie.Name,
					Value = httpCookie.Value,
					Domain = webRequest.RequestUri.Host
				};
				webRequest.CookieContainer.Add(cookie);
			}
		}

		// Token: 0x06000064 RID: 100 RVA: 0x00003140 File Offset: 0x00001340
		private string EncodeParameters()
		{
			StringBuilder stringBuilder = new StringBuilder();
			foreach (HttpParameter httpParameter in this.Parameters)
			{
				bool flag = stringBuilder.Length > 1;
				if (flag)
				{
					stringBuilder.Append("&");
				}
				stringBuilder.AppendFormat("{0}={1}", httpParameter.Name.UrlEncode(), httpParameter.Value.UrlEncode());
			}
			return stringBuilder.ToString();
		}

		// Token: 0x06000065 RID: 101 RVA: 0x000031DC File Offset: 0x000013DC
		private void PreparePostBody(HttpWebRequest webRequest)
		{
			// 不要为GET请求准备POST请求体
			if (webRequest.Method == "GET")
				return;
				
			bool flag = string.IsNullOrEmpty(webRequest.ContentType);
			bool flag2 = this.HasFiles || this.AlwaysMultipartFormData;
			if (flag2)
			{
				bool flag3 = flag;
				if (flag3)
				{
					webRequest.ContentType = Http.GetMultipartFormContentType();
				}
			}
			else
			{
				bool hasParameters = this.HasParameters;
				if (hasParameters)
				{
					bool flag4 = flag;
					if (flag4)
					{
						webRequest.ContentType = "application/x-www-form-urlencoded";
					}
					this.RequestBody = this.EncodeParameters();
				}
				else
				{
					bool hasBody = this.HasBody;
					if (hasBody)
					{
						bool flag5 = flag;
						if (flag5)
						{
							webRequest.ContentType = this.RequestContentType;
						}
					}
				}
			}
		}

		// Token: 0x06000066 RID: 102 RVA: 0x00003278 File Offset: 0x00001478
		private void WriteStringTo(Stream stream, string toWrite)
		{
			byte[] bytes = this.Encoding.GetBytes(toWrite);
			stream.Write(bytes, 0, bytes.Length);
		}

		// Token: 0x06000067 RID: 103 RVA: 0x000032A0 File Offset: 0x000014A0
		private void WriteMultipartFormData(Stream requestStream)
		{
			foreach (HttpParameter httpParameter in this.Parameters)
			{
				this.WriteStringTo(requestStream, this.GetMultipartFormData(httpParameter));
			}
			foreach (HttpFile httpFile in this.Files)
			{
				this.WriteStringTo(requestStream, Http.GetMultipartFileHeader(httpFile));
				httpFile.Writer(requestStream);
				this.WriteStringTo(requestStream, "\r\n");
			}
			this.WriteStringTo(requestStream, Http.GetMultipartFooter());
		}

		// Token: 0x06000068 RID: 104 RVA: 0x00003368 File Offset: 0x00001568
		private void ExtractResponseData(HttpResponse response, HttpWebResponse webResponse)
		{
			try
			{
				response.ContentEncoding = webResponse.ContentEncoding;
				response.Server = webResponse.Server;
				response.ContentType = webResponse.ContentType;
				response.ContentLength = webResponse.ContentLength;
				Stream responseStream = webResponse.GetResponseStream();
				this.ProcessResponseStream(responseStream, response);
				response.StatusCode = webResponse.StatusCode;
				response.StatusDescription = webResponse.StatusDescription;
				response.ResponseUri = webResponse.ResponseUri;
				response.ResponseStatus = ResponseStatus.Completed;
				bool flag = webResponse.Cookies != null;
				if (flag)
				{
					foreach (object obj in webResponse.Cookies)
					{
						Cookie cookie = (Cookie)obj;
						response.Cookies.Add(new HttpCookie
						{
							Comment = cookie.Comment,
							CommentUri = cookie.CommentUri,
							Discard = cookie.Discard,
							Domain = cookie.Domain,
							Expired = cookie.Expired,
							Expires = cookie.Expires,
							HttpOnly = cookie.HttpOnly,
							Name = cookie.Name,
							Path = cookie.Path,
							Port = cookie.Port,
							Secure = cookie.Secure,
							TimeStamp = cookie.TimeStamp,
							Value = cookie.Value,
							Version = cookie.Version
						});
					}
				}
				foreach (string text in webResponse.Headers.AllKeys)
				{
					string text2 = webResponse.Headers[text];
					response.Headers.Add(new HttpHeader
					{
						Name = text,
						Value = text2
					});
				}
				webResponse.Close();
			}
			finally
			{
				if (webResponse != null)
				{
					((IDisposable)webResponse).Dispose();
				}
			}
		}

		// Token: 0x06000069 RID: 105 RVA: 0x000035BC File Offset: 0x000017BC
		private void ProcessResponseStream(Stream webResponseStream, HttpResponse response)
		{
			bool flag = this.ResponseWriter == null;
			if (flag)
			{
				response.RawBytes = webResponseStream.ReadAsBytes();
			}
			else
			{
				this.ResponseWriter(webResponseStream);
			}
		}

		// Token: 0x0600006A RID: 106 RVA: 0x000035F8 File Offset: 0x000017F8
		private static void AddRange(HttpWebRequest r, string range)
		{
			Match match = Regex.Match(range, "(\\w+)=(\\d+)-(\\d+)$");
			bool flag = !match.Success;
			if (!flag)
			{
				string value = match.Groups[1].Value;
				long num = Convert.ToInt64(match.Groups[2].Value);
				long num2 = Convert.ToInt64(match.Groups[3].Value);
				r.AddRange(value, num, num2);
			}
		}

		// Token: 0x0600006B RID: 107 RVA: 0x0000366E File Offset: 0x0000186E
		public HttpResponse Delete()
		{
			return this.GetStyleMethodInternal("DELETE");
		}

		// Token: 0x0600006C RID: 108 RVA: 0x0000367B File Offset: 0x0000187B
		public HttpResponse Get()
		{
			return this.GetStyleMethodInternal("GET");
		}

		// Token: 0x0600006D RID: 109 RVA: 0x00003688 File Offset: 0x00001888
		public HttpResponse Head()
		{
			return this.GetStyleMethodInternal("HEAD");
		}

		// Token: 0x0600006E RID: 110 RVA: 0x00003695 File Offset: 0x00001895
		public HttpResponse Post()
		{
			return this.PostPutInternal("POST");
		}

		// Token: 0x0600006F RID: 111 RVA: 0x000036A2 File Offset: 0x000018A2
		public HttpResponse Put()
		{
			return this.PostPutInternal("PUT");
		}

		// Token: 0x06000070 RID: 112 RVA: 0x000036AF File Offset: 0x000018AF
		public HttpResponse Patch()
		{
			return this.PostPutInternal("PATCH");
		}

		// Token: 0x06000071 RID: 113 RVA: 0x000036BC File Offset: 0x000018BC
		public HttpResponse AsGet(string httpMethod)
		{
			return this.GetStyleMethodInternal(httpMethod.ToUpperInvariant());
		}

		// Token: 0x06000072 RID: 114 RVA: 0x000036CA File Offset: 0x000018CA
		public HttpResponse AsPost(string httpMethod)
		{
			return this.PostPutInternal(httpMethod.ToUpperInvariant());
		}

		// Token: 0x06000073 RID: 115 RVA: 0x000036D8 File Offset: 0x000018D8
		private HttpResponse GetStyleMethodInternal(string method)
		{
			HttpWebRequest httpWebRequest = this.ConfigureWebRequest(method, this.Url);
			bool flag = this.HasBody && method == "DELETE";
			if (flag)
			{
				httpWebRequest.ContentType = this.RequestContentType;
				this.WriteRequestBody(httpWebRequest);
			}
			return this.GetResponse(httpWebRequest);
		}

		// Token: 0x06000074 RID: 116 RVA: 0x00003730 File Offset: 0x00001930
		private HttpResponse PostPutInternal(string method)
		{
			// 只有在POST, PUT, PATCH方法时才执行PreparePostBody和WriteRequestBody
			if (method != "POST" && method != "PUT" && method != "PATCH")
			{
				return this.GetStyleMethodInternal(method);
			}
			
			HttpWebRequest httpWebRequest = this.ConfigureWebRequest(method, this.Url);
			this.PreparePostBody(httpWebRequest);
			this.WriteRequestBody(httpWebRequest);
			return this.GetResponse(httpWebRequest);
		}

		// Token: 0x06000075 RID: 117 RVA: 0x000037D4 File Offset: 0x000019D4
		private static void ExtractErrorResponse(IHttpResponse httpResponse, Exception ex)
		{
			WebException ex2 = ex as WebException;
			bool flag = ex2 != null && ex2.Status == WebExceptionStatus.Timeout;
			if (flag)
			{
				httpResponse.ResponseStatus = ResponseStatus.TimedOut;
				httpResponse.ErrorMessage = ex.Message;
				httpResponse.ErrorException = ex2;
			}
			else
			{
				httpResponse.ErrorMessage = ex.Message;
				httpResponse.ErrorException = ex;
				httpResponse.ResponseStatus = ResponseStatus.Error;
			}
		}

		// Token: 0x06000076 RID: 118 RVA: 0x00003828 File Offset: 0x00001A28
		private HttpResponse GetResponse(HttpWebRequest request)
		{
			HttpResponse httpResponse = new HttpResponse
			{
				ResponseStatus = ResponseStatus.None
			};
			try
			{
				HttpWebResponse rawResponse = Http.GetRawResponse(request);
				this.ExtractResponseData(httpResponse, rawResponse);
			}
			catch (Exception ex)
			{
				Http.ExtractErrorResponse(httpResponse, ex);
			}
			return httpResponse;
		}

		// Token: 0x06000077 RID: 119 RVA: 0x00003874 File Offset: 0x00001A74
		private static HttpWebResponse GetRawResponse(HttpWebRequest request)
		{
			HttpWebResponse httpWebResponse;
			try
			{
				httpWebResponse = (HttpWebResponse)request.GetResponse();
			}
			catch (WebException ex)
			{
				HttpWebResponse httpWebResponse2 = ex.Response as HttpWebResponse;
				bool flag = httpWebResponse2 != null;
				if (!flag)
				{
					throw;
				}
				httpWebResponse = httpWebResponse2;
			}
			return httpWebResponse;
		}

		// Token: 0x06000078 RID: 120 RVA: 0x000038A4 File Offset: 0x00001AA4
		private void WriteRequestBody(HttpWebRequest webRequest)
		{
			// GET请求不应该有请求体
			if (webRequest.Method == "GET")
				return;
				
			bool flag = this.HasBody || this.HasFiles || this.AlwaysMultipartFormData;
			if (flag)
			{
				webRequest.ContentLength = this.CalculateContentLength();
			}
			using (Stream requestStream = webRequest.GetRequestStream())
			{
				bool flag2 = this.HasFiles || this.AlwaysMultipartFormData;
				if (flag2)
				{
					this.WriteMultipartFormData(requestStream);
				}
				else
				{
					bool flag3 = this.RequestBodyBytes != null;
					if (flag3)
					{
						requestStream.Write(this.RequestBodyBytes, 0, this.RequestBodyBytes.Length);
					}
					else
					{
						bool flag4 = this.RequestBody != null;
						if (flag4)
						{
							this.WriteStringTo(requestStream, this.RequestBody);
						}
					}
				}
			}
		}

		// Token: 0x06000079 RID: 121 RVA: 0x0000393C File Offset: 0x00001B3C
		protected virtual HttpWebRequest ConfigureWebRequest(string method, Uri url)
		{
			HttpWebRequest httpWebRequest = this.CreateWebRequest(url);
			httpWebRequest.Proxy = null;
			httpWebRequest.UseDefaultCredentials = this.UseDefaultCredentials;
			httpWebRequest.PreAuthenticate = this.PreAuthenticate;
			this.AppendHeaders(httpWebRequest);
			this.AppendCookies(httpWebRequest);
			httpWebRequest.Method = method;
			bool flag = !this.HasFiles && !this.AlwaysMultipartFormData && method != "GET";
			if (flag)
			{
				httpWebRequest.ContentLength = 0L;
			}
			bool flag2 = this.Credentials != null;
			if (flag2)
			{
				httpWebRequest.Credentials = this.Credentials;
			}
			bool flag3 = this.UserAgent.HasValue();
			if (flag3)
			{
				httpWebRequest.UserAgent = this.UserAgent;
			}
			bool flag4 = this.ClientCertificates != null;
			if (flag4)
			{
				httpWebRequest.ClientCertificates.AddRange(this.ClientCertificates);
			}
			httpWebRequest.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
			bool flag5 = this.Timeout != 0;
			if (flag5)
			{
				httpWebRequest.Timeout = this.Timeout;
			}
			bool flag6 = this.ReadWriteTimeout != 0;
			if (flag6)
			{
				httpWebRequest.ReadWriteTimeout = this.ReadWriteTimeout;
			}
			bool flag7 = this.CachePolicy != null;
			if (flag7)
			{
				httpWebRequest.CachePolicy = this.CachePolicy;
			}
			httpWebRequest.AllowAutoRedirect = this.FollowRedirects;
			bool flag8 = this.FollowRedirects && this.MaxRedirects != null;
			if (flag8)
			{
				httpWebRequest.MaximumAutomaticRedirections = this.MaxRedirects.Value;
			}
			return httpWebRequest;
		}

		// Token: 0x0400000B RID: 11
		private TimeOutState timeoutState;

		// Token: 0x0400000C RID: 12
		private const string LINE_BREAK = "\r\n";

		// Token: 0x0400000D RID: 13
		private const string FORM_BOUNDARY = "-----------------------------28947758029299";

		// Token: 0x0400000E RID: 14
		private readonly IDictionary<string, Action<HttpWebRequest, string>> restrictedHeaderActions;
	}
}
