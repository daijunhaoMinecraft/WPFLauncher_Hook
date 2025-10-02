using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Cache;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Mcl.Core.Extensions;
using Mcl.Core.Network.Interface;

namespace Mcl.Core.Network
{
	// Token: 0x02000012 RID: 18
	public class NetClient : INetClient
	{
		// Token: 0x060000CE RID: 206 RVA: 0x00003DB4 File Offset: 0x00001FB4
		public virtual NetRequestAsyncHandle ExecuteAsync(INetRequest request, Action<INetResponse, NetRequestAsyncHandle> callback)
		{
			string name = Enum.GetName(typeof(Method), request.Method);
			Method method = request.Method;
			NetRequestAsyncHandle netRequestAsyncHandle;
			if (method - Method.POST > 1 && method != Method.PATCH)
			{
				netRequestAsyncHandle = this.ExecuteAsync(request, callback, name, new Func<IHttp, Action<HttpResponse>, string, HttpWebRequest>(NetClient.DoAsGetAsync));
			}
			else
			{
				netRequestAsyncHandle = this.ExecuteAsync(request, callback, name, new Func<IHttp, Action<HttpResponse>, string, HttpWebRequest>(NetClient.DoAsPostAsync));
			}
			return netRequestAsyncHandle;
		}

		// Token: 0x060000CF RID: 207 RVA: 0x00003E24 File Offset: 0x00002024
		public virtual NetRequestAsyncHandle ExecuteAsyncGet(INetRequest request, Action<INetResponse, NetRequestAsyncHandle> callback, string httpMethod)
		{
			return this.ExecuteAsync(request, callback, httpMethod, new Func<IHttp, Action<HttpResponse>, string, HttpWebRequest>(NetClient.DoAsGetAsync));
		}

		// Token: 0x060000D0 RID: 208 RVA: 0x00003E4C File Offset: 0x0000204C
		public virtual NetRequestAsyncHandle ExecuteAsyncPost(INetRequest request, Action<INetResponse, NetRequestAsyncHandle> callback, string httpMethod)
		{
			request.Method = Method.POST;
			return this.ExecuteAsync(request, callback, httpMethod, new Func<IHttp, Action<HttpResponse>, string, HttpWebRequest>(NetClient.DoAsPostAsync));
		}

		// Token: 0x060000D1 RID: 209 RVA: 0x00003E7C File Offset: 0x0000207C
		public virtual Task<INetResponse> ExecuteTaskAsync(INetRequest request)
		{
			return this.ExecuteTaskAsync(request, CancellationToken.None);
		}

		// Token: 0x060000D2 RID: 210 RVA: 0x00003E9C File Offset: 0x0000209C
		public virtual Task<INetResponse> ExecuteGetTaskAsync(INetRequest request)
		{
			return this.ExecuteGetTaskAsync(request, CancellationToken.None);
		}

		// Token: 0x060000D3 RID: 211 RVA: 0x00003EBC File Offset: 0x000020BC
		public virtual Task<INetResponse> ExecuteGetTaskAsync(INetRequest request, CancellationToken token)
		{
			bool flag = request == null;
			if (flag)
			{
				throw new ArgumentNullException("request");
			}
			request.Method = Method.GET;
			return this.ExecuteTaskAsync(request, token);
		}

		// Token: 0x060000D4 RID: 212 RVA: 0x00003EF4 File Offset: 0x000020F4
		public virtual Task<INetResponse> ExecutePostTaskAsync(INetRequest request)
		{
			return this.ExecutePostTaskAsync(request, CancellationToken.None);
		}

		// Token: 0x060000D5 RID: 213 RVA: 0x00003F14 File Offset: 0x00002114
		public virtual Task<INetResponse> ExecutePostTaskAsync(INetRequest request, CancellationToken token)
		{
			bool flag = request == null;
			if (flag)
			{
				throw new ArgumentNullException("request");
			}
			request.Method = Method.POST;
			return this.ExecuteTaskAsync(request, token);
		}

		// Token: 0x060000D6 RID: 214 RVA: 0x00003F4C File Offset: 0x0000214C
		public virtual Task<INetResponse> ExecuteTaskAsync(INetRequest request, CancellationToken token)
		{
			bool flag = request == null;
			if (flag)
			{
				throw new ArgumentNullException("request");
			}
			TaskCompletionSource<INetResponse> taskCompletionSource = new TaskCompletionSource<INetResponse>();
			try
			{
				NetRequestAsyncHandle async = this.ExecuteAsync(request, delegate(INetResponse response, NetRequestAsyncHandle _)
				{
					bool isCancellationRequested = token.IsCancellationRequested;
					if (isCancellationRequested)
					{
						taskCompletionSource.TrySetCanceled();
					}
					else
					{
						taskCompletionSource.TrySetResult(response);
					}
				});
				CancellationTokenRegistration registration = token.Register(delegate
				{
					async.Abort();
					taskCompletionSource.TrySetCanceled();
				});
				taskCompletionSource.Task.ContinueWith(delegate(Task<INetResponse> t)
				{
					registration.Dispose();
				}, token);
			}
			catch (Exception ex)
			{
				taskCompletionSource.TrySetException(ex);
			}
			return taskCompletionSource.Task;
		}

		// Token: 0x060000D7 RID: 215 RVA: 0x00004038 File Offset: 0x00002238
		private NetRequestAsyncHandle ExecuteAsync(INetRequest request, Action<INetResponse, NetRequestAsyncHandle> callback, string httpMethod, Func<IHttp, Action<HttpResponse>, string, HttpWebRequest> getWebRequest)
		{
			IHttp http = this.HttpFactory.Create();
			this.ConfigureHttp(request, http);
			NetRequestAsyncHandle asyncHandle = new NetRequestAsyncHandle();
			Action<HttpResponse> action = delegate(HttpResponse r)
			{
				NetClient.ProcessResponse(request, r, asyncHandle, callback);
			};
			bool flag = this.UseSynchronizationContext && SynchronizationContext.Current != null;
			if (flag)
			{
				SynchronizationContext ctx = SynchronizationContext.Current;
				Action<HttpResponse> cb = action;
				action = delegate(HttpResponse resp)
				{
					ctx.Post(delegate(object s)
					{
						cb(resp);
					}, null);
				};
			}
			asyncHandle.WebRequest = getWebRequest(http, action, httpMethod);
			return asyncHandle;
		}

		// Token: 0x060000D8 RID: 216 RVA: 0x000040F0 File Offset: 0x000022F0
		private static HttpWebRequest DoAsGetAsync(IHttp http, Action<HttpResponse> responseCb, string method)
		{
			return http.AsGetAsync(responseCb, method);
		}

		// Token: 0x060000D9 RID: 217 RVA: 0x0000410C File Offset: 0x0000230C
		private static HttpWebRequest DoAsPostAsync(IHttp http, Action<HttpResponse> responseCb, string method)
		{
			return http.AsPostAsync(responseCb, method);
		}

		// Token: 0x060000DA RID: 218 RVA: 0x00004128 File Offset: 0x00002328
		private static void ProcessResponse(INetRequest request, HttpResponse httpResponse, NetRequestAsyncHandle asyncHandle, Action<INetResponse, NetRequestAsyncHandle> callback)
		{
			NetResponse netResponse = NetClient.ConvertToNetResponse(request, httpResponse);
			callback(netResponse, asyncHandle);
		}

		// Token: 0x17000047 RID: 71
		// (get) Token: 0x060000DB RID: 219 RVA: 0x00004147 File Offset: 0x00002347
		// (set) Token: 0x060000DC RID: 220 RVA: 0x0000414F File Offset: 0x0000234F
		public int? MaxRedirects { get; set; }

		// Token: 0x17000048 RID: 72
		// (get) Token: 0x060000DD RID: 221 RVA: 0x00004158 File Offset: 0x00002358
		// (set) Token: 0x060000DE RID: 222 RVA: 0x00004160 File Offset: 0x00002360
		public X509CertificateCollection ClientCertificates { get; set; }

		// Token: 0x17000049 RID: 73
		// (get) Token: 0x060000DF RID: 223 RVA: 0x00004169 File Offset: 0x00002369
		// (set) Token: 0x060000E0 RID: 224 RVA: 0x00004171 File Offset: 0x00002371
		public RequestCachePolicy CachePolicy { get; set; }

		// Token: 0x1700004A RID: 74
		// (get) Token: 0x060000E1 RID: 225 RVA: 0x0000417A File Offset: 0x0000237A
		// (set) Token: 0x060000E2 RID: 226 RVA: 0x00004182 File Offset: 0x00002382
		public bool FollowRedirects { get; set; }

		// Token: 0x1700004B RID: 75
		// (get) Token: 0x060000E3 RID: 227 RVA: 0x0000418B File Offset: 0x0000238B
		// (set) Token: 0x060000E4 RID: 228 RVA: 0x00004193 File Offset: 0x00002393
		public CookieContainer CookieContainer { get; set; }

		// Token: 0x1700004C RID: 76
		// (get) Token: 0x060000E5 RID: 229 RVA: 0x0000419C File Offset: 0x0000239C
		// (set) Token: 0x060000E6 RID: 230 RVA: 0x000041A4 File Offset: 0x000023A4
		public string UserAgent { get; set; }

		// Token: 0x1700004D RID: 77
		// (get) Token: 0x060000E7 RID: 231 RVA: 0x000041AD File Offset: 0x000023AD
		// (set) Token: 0x060000E8 RID: 232 RVA: 0x000041B5 File Offset: 0x000023B5
		public int Timeout { get; set; }

		// Token: 0x1700004E RID: 78
		// (get) Token: 0x060000E9 RID: 233 RVA: 0x000041BE File Offset: 0x000023BE
		// (set) Token: 0x060000EA RID: 234 RVA: 0x000041C6 File Offset: 0x000023C6
		public int ReadWriteTimeout { get; set; }

		// Token: 0x1700004F RID: 79
		// (get) Token: 0x060000EB RID: 235 RVA: 0x000041CF File Offset: 0x000023CF
		// (set) Token: 0x060000EC RID: 236 RVA: 0x000041D7 File Offset: 0x000023D7
		public bool UseSynchronizationContext { get; set; }

		// Token: 0x17000050 RID: 80
		// (get) Token: 0x060000ED RID: 237 RVA: 0x000041E0 File Offset: 0x000023E0
		// (set) Token: 0x060000EE RID: 238 RVA: 0x000041E8 File Offset: 0x000023E8
		public virtual Uri BaseUrl { get; set; }

		// Token: 0x17000051 RID: 81
		// (get) Token: 0x060000EF RID: 239 RVA: 0x000041F1 File Offset: 0x000023F1
		// (set) Token: 0x060000F0 RID: 240 RVA: 0x000041F9 File Offset: 0x000023F9
		public Encoding Encoding { get; set; }

		// Token: 0x17000052 RID: 82
		// (get) Token: 0x060000F1 RID: 241 RVA: 0x00004202 File Offset: 0x00002402
		// (set) Token: 0x060000F2 RID: 242 RVA: 0x0000420A File Offset: 0x0000240A
		public bool PreAuthenticate { get; set; }

		// Token: 0x060000F3 RID: 243 RVA: 0x00004214 File Offset: 0x00002414
		public NetClient()
		{
			this.Encoding = Encoding.UTF8;
			this.AcceptTypes = new List<string>();
			this.DefaultParameters = new List<Parameter>();
			this.FollowRedirects = true;
		}

		// Token: 0x060000F4 RID: 244 RVA: 0x00004282 File Offset: 0x00002482
		public NetClient(Uri baseUrl)
			: this()
		{
			this.BaseUrl = baseUrl;
		}

		// Token: 0x060000F5 RID: 245 RVA: 0x00004294 File Offset: 0x00002494
		public NetClient(string baseUrl)
			: this()
		{
			bool flag = string.IsNullOrEmpty(baseUrl);
			if (flag)
			{
				throw new ArgumentNullException("baseUrl");
			}
			this.BaseUrl = new Uri(baseUrl);
		}

		// Token: 0x17000053 RID: 83
		// (get) Token: 0x060000F6 RID: 246 RVA: 0x000042CC File Offset: 0x000024CC
		// (set) Token: 0x060000F7 RID: 247 RVA: 0x000042D4 File Offset: 0x000024D4
		private IList<string> AcceptTypes { get; set; }

		// Token: 0x17000054 RID: 84
		// (get) Token: 0x060000F8 RID: 248 RVA: 0x000042DD File Offset: 0x000024DD
		// (set) Token: 0x060000F9 RID: 249 RVA: 0x000042E5 File Offset: 0x000024E5
		public IList<Parameter> DefaultParameters { get; private set; }

		// Token: 0x060000FA RID: 250 RVA: 0x000042F0 File Offset: 0x000024F0
		public Uri BuildUri(INetRequest request)
		{
			bool flag = this.BaseUrl == null;
			if (flag)
			{
				throw new NullReferenceException("NetClient must contain a value for BaseUrl");
			}
			string text = request.Resource;
			IEnumerable<Parameter> enumerable = request.Parameters.Where((Parameter p) => p.Type == ParameterType.UrlSegment);
			UriBuilder uriBuilder = new UriBuilder(this.BaseUrl);
			foreach (Parameter parameter in enumerable)
			{
				bool flag2 = parameter.Value == null;
				if (flag2)
				{
					throw new ArgumentException(string.Format("Cannot build uri when url segment parameter '{0}' value is null.", parameter.Name), "request");
				}
				bool flag3 = !string.IsNullOrEmpty(text);
				if (flag3)
				{
					text = text.Replace("{" + parameter.Name + "}", parameter.Value.ToString().UrlEncode());
				}
				uriBuilder.Path = uriBuilder.Path.UrlDecode().Replace("{" + parameter.Name + "}", parameter.Value.ToString().UrlEncode());
			}
			this.BaseUrl = new Uri(uriBuilder.ToString());
			bool flag4 = !string.IsNullOrEmpty(text) && text.StartsWith("/");
			if (flag4)
			{
				text = text.Substring(1);
			}
			bool flag5 = this.BaseUrl != null && !string.IsNullOrEmpty(this.BaseUrl.AbsoluteUri);
			if (flag5)
			{
				bool flag6 = !this.BaseUrl.AbsoluteUri.EndsWith("/") && !string.IsNullOrEmpty(text);
				if (flag6)
				{
					text = "/" + text;
				}
				text = (string.IsNullOrEmpty(text) ? this.BaseUrl.AbsoluteUri : string.Format("{0}{1}", this.BaseUrl, text));
			}
			bool flag7 = request.Method != Method.POST && request.Method != Method.PUT && request.Method != Method.PATCH;
			IEnumerable<Parameter> enumerable2;
			if (flag7)
			{
				enumerable2 = request.Parameters.Where((Parameter p) => p.Type == ParameterType.GetOrPost || p.Type == ParameterType.QueryString).ToList<Parameter>();
			}
			else
			{
				enumerable2 = request.Parameters.Where((Parameter p) => p.Type == ParameterType.QueryString).ToList<Parameter>();
			}
			bool flag8 = !enumerable2.Any<Parameter>();
			Uri uri;
			if (flag8)
			{
				uri = new Uri(text);
			}
			else
			{
				string text2 = NetClient.EncodeParameters(enumerable2);
				string text3 = ((text != null && text.Contains("?")) ? "&" : "?");
				text = text + text3 + text2;
				uri = new Uri(text);
			}
			return uri;
		}

		// Token: 0x060000FB RID: 251 RVA: 0x000045F0 File Offset: 0x000027F0
		private static string EncodeParameters(IEnumerable<Parameter> parameters)
		{
			return string.Join("&", parameters.Select(new Func<Parameter, string>(NetClient.EncodeParameter)).ToArray<string>());
		}

		// Token: 0x060000FC RID: 252 RVA: 0x00004624 File Offset: 0x00002824
		private static string EncodeParameter(Parameter parameter)
		{
			return (parameter.Value == null) ? (parameter.Name.UrlEncode() + "=") : (parameter.Name.UrlEncode() + "=" + parameter.Value.ToString().UrlEncode());
		}

		// Token: 0x060000FD RID: 253 RVA: 0x0000467C File Offset: 0x0000287C
		private void ConfigureHttp(INetRequest request, IHttp http)
		{
			http.Encoding = this.Encoding;
			http.AlwaysMultipartFormData = request.AlwaysMultipartFormData;
			http.UseDefaultCredentials = request.UseDefaultCredentials;
			http.ResponseWriter = request.ResponseWriter;
			http.CookieContainer = this.CookieContainer;
			using (IEnumerator<Parameter> enumerator = this.DefaultParameters.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					Parameter p2 = enumerator.Current;
					bool flag = request.Parameters.Any((Parameter p) => p.Name == p2.Name && p.Type == p2.Type);
					if (!flag)
					{
						request.AddParameter(p2);
					}
				}
			}
			bool flag2 = request.Parameters.All((Parameter p) => p.Name.ToLowerInvariant() != "accept");
			if (flag2)
			{
				string text = string.Join(", ", this.AcceptTypes.ToArray<string>());
				request.AddParameter("Accept", text, ParameterType.HttpHeader);
			}
			http.Url = this.BuildUri(request);
			http.PreAuthenticate = this.PreAuthenticate;
			string text2 = this.UserAgent ?? http.UserAgent;
			http.UserAgent = ((!string.IsNullOrEmpty(text2)) ? text2 : ("WPFLauncher/" + NetClient.version));
			int num = ((request.Timeout > 0) ? request.Timeout : this.Timeout);
			bool flag3 = num > 0;
			if (flag3)
			{
				http.Timeout = num;
			}
			int num2 = ((request.ReadWriteTimeout > 0) ? request.ReadWriteTimeout : this.ReadWriteTimeout);
			bool flag4 = num2 > 0;
			if (flag4)
			{
				http.ReadWriteTimeout = num2;
			}
			http.FollowRedirects = this.FollowRedirects;
			bool flag5 = this.ClientCertificates != null;
			if (flag5)
			{
				http.ClientCertificates = this.ClientCertificates;
			}
			http.MaxRedirects = this.MaxRedirects;
			http.CachePolicy = this.CachePolicy;
			bool flag6 = request.Credentials != null;
			if (flag6)
			{
				http.Credentials = request.Credentials;
			}
			IEnumerable<HttpHeader> enumerable = from p in request.Parameters
				where p.Type == ParameterType.HttpHeader
				select new HttpHeader
				{
					Name = p.Name,
					Value = Convert.ToString(p.Value)
				};
			foreach (HttpHeader httpHeader in enumerable)
			{
				http.Headers.Add(httpHeader);
			}
			IEnumerable<HttpCookie> enumerable2 = from p in request.Parameters
				where p.Type == ParameterType.Cookie
				select new HttpCookie
				{
					Name = p.Name,
					Value = Convert.ToString(p.Value)
				};
			foreach (HttpCookie httpCookie in enumerable2)
			{
				http.Cookies.Add(httpCookie);
			}
			IEnumerable<HttpParameter> enumerable3 = from p in request.Parameters
				where p.Type == ParameterType.GetOrPost && p.Value != null
				select new HttpParameter
				{
					Name = p.Name,
					Value = Convert.ToString(p.Value)
				};
			foreach (HttpParameter httpParameter in enumerable3)
			{
				http.Parameters.Add(httpParameter);
			}
			foreach (FileParameter fileParameter in request.Files)
			{
				http.Files.Add(new HttpFile
				{
					Name = fileParameter.Name,
					ContentType = fileParameter.ContentType,
					Writer = fileParameter.Writer,
					FileName = fileParameter.FileName,
					ContentLength = fileParameter.ContentLength
				});
			}
			Parameter parameter = request.Parameters.FirstOrDefault((Parameter p) => p.Type == ParameterType.RequestBody);
			bool flag7 = parameter != null;
			if (flag7)
			{
				http.RequestContentType = parameter.Name;
				bool flag8 = !http.Files.Any<HttpFile>();
				if (flag8)
				{
					object value = parameter.Value;
					bool flag9 = value is byte[];
					if (flag9)
					{
						http.RequestBodyBytes = (byte[])value;
					}
					else
					{
						http.RequestBody = Convert.ToString(parameter.Value);
					}
				}
				else
				{
					http.Parameters.Add(new HttpParameter
					{
						Name = parameter.Name,
						Value = Convert.ToString(parameter.Value),
						ContentType = parameter.ContentType
					});
				}
			}
		}

		// Token: 0x060000FE RID: 254 RVA: 0x00004BF0 File Offset: 0x00002DF0
		private static NetResponse ConvertToNetResponse(INetRequest request, HttpResponse httpResponse)
		{
			NetResponse netResponse = new NetResponse
			{
				Content = httpResponse.Content,
				ContentEncoding = httpResponse.ContentEncoding,
				ContentLength = httpResponse.ContentLength,
				ContentType = httpResponse.ContentType,
				ErrorException = httpResponse.ErrorException,
				ErrorMessage = httpResponse.ErrorMessage,
				RawBytes = httpResponse.RawBytes,
				ResponseStatus = httpResponse.ResponseStatus,
				ResponseUri = httpResponse.ResponseUri,
				Server = httpResponse.Server,
				StatusCode = httpResponse.StatusCode,
				StatusDescription = httpResponse.StatusDescription,
				Request = request,
				ProtocolVersion = httpResponse.ProtocolVersion
			};
			foreach (HttpHeader httpHeader in httpResponse.Headers)
			{
				netResponse.Headers.Add(new Parameter
				{
					Name = httpHeader.Name,
					Value = httpHeader.Value,
					Type = ParameterType.HttpHeader
				});
			}
			foreach (HttpCookie httpCookie in httpResponse.Cookies)
			{
				netResponse.Cookies.Add(new NetResponseCookie
				{
					Comment = httpCookie.Comment,
					CommentUri = httpCookie.CommentUri,
					Discard = httpCookie.Discard,
					Domain = httpCookie.Domain,
					Expired = httpCookie.Expired,
					Expires = httpCookie.Expires,
					HttpOnly = httpCookie.HttpOnly,
					Name = httpCookie.Name,
					Path = httpCookie.Path,
					Port = httpCookie.Port,
					Secure = httpCookie.Secure,
					TimeStamp = httpCookie.TimeStamp,
					Value = httpCookie.Value,
					Version = httpCookie.Version
				});
			}
			return netResponse;
		}

		// Token: 0x060000FF RID: 255 RVA: 0x00004E44 File Offset: 0x00003044
		public byte[] DownloadData(INetRequest request)
		{
			return this.DownloadData(request, false);
		}

		// Token: 0x06000100 RID: 256 RVA: 0x00004E50 File Offset: 0x00003050
		public byte[] DownloadData(INetRequest request, bool throwOnError)
		{
			INetResponse netResponse = this.Execute(request);
			bool flag = netResponse.ResponseStatus == ResponseStatus.Error && throwOnError;
			if (flag)
			{
				throw netResponse.ErrorException;
			}
			return netResponse.RawBytes;
		}

		// Token: 0x06000101 RID: 257 RVA: 0x00004E88 File Offset: 0x00003088
		public virtual INetResponse Execute(INetRequest request)
		{
			string name = Enum.GetName(typeof(Method), request.Method);
			Method method = request.Method;
			INetResponse netResponse;
			if (method - Method.POST > 1 && method != Method.PATCH)
			{
				netResponse = this.Execute(request, name, new Func<IHttp, string, HttpResponse>(NetClient.DoExecuteAsGet));
			}
			else
			{
				netResponse = this.Execute(request, name, new Func<IHttp, string, HttpResponse>(NetClient.DoExecuteAsPost));
			}
			return netResponse;
		}

		// Token: 0x06000102 RID: 258 RVA: 0x00004EF8 File Offset: 0x000030F8
		private INetResponse Execute(INetRequest request, string httpMethod, Func<IHttp, string, HttpResponse> getResponse)
		{
			INetResponse netResponse = new NetResponse();
			try
			{
				IHttp http = this.HttpFactory.Create();
				this.ConfigureHttp(request, http);
				netResponse = NetClient.ConvertToNetResponse(request, getResponse(http, httpMethod));
				netResponse.Request = request;
				netResponse.Request.IncreaseNumAttempts();
			}
			catch (Exception ex)
			{
				netResponse.ResponseStatus = ResponseStatus.Error;
				netResponse.ErrorMessage = ex.Message;
				netResponse.ErrorException = ex;
			}
			return netResponse;
		}

		// Token: 0x06000103 RID: 259 RVA: 0x00004F80 File Offset: 0x00003180
		public INetResponse ExecuteAsGet(INetRequest request, string httpMethod)
		{
			return this.Execute(request, httpMethod, new Func<IHttp, string, HttpResponse>(NetClient.DoExecuteAsGet));
		}

		// Token: 0x06000104 RID: 260 RVA: 0x00004FA8 File Offset: 0x000031A8
		public INetResponse ExecuteAsPost(INetRequest request, string httpMethod)
		{
			request.Method = Method.POST;
			return this.Execute(request, httpMethod, new Func<IHttp, string, HttpResponse>(NetClient.DoExecuteAsPost));
		}

		// Token: 0x06000105 RID: 261 RVA: 0x00004FD8 File Offset: 0x000031D8
		private static HttpResponse DoExecuteAsGet(IHttp http, string method)
		{
			return http.AsGet(method);
		}

		// Token: 0x06000106 RID: 262 RVA: 0x00004FF4 File Offset: 0x000031F4
		private static HttpResponse DoExecuteAsPost(IHttp http, string method)
		{
			return http.AsPost(method);
		}

		// Token: 0x04000069 RID: 105
		private static readonly Version version = new AssemblyName(Assembly.GetExecutingAssembly().FullName).Version;

		// Token: 0x0400006A RID: 106
		public IHttpFactory HttpFactory = new SimpleFactory<Http>();

		// Token: 0x04000079 RID: 121
		private readonly Regex structuredSyntaxSuffixRegex = new Regex("\\+\\w+$", RegexOptions.Compiled);

		// Token: 0x0400007A RID: 122
		private readonly Regex structuredSyntaxSuffixWildcardRegex = new Regex("^\\*\\+\\w+$", RegexOptions.Compiled);
	}
}
