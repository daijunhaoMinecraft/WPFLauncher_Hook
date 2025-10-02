using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;
using Mcl.Core.Network.Interface;

namespace Mcl.Core.Network
{
	// Token: 0x02000019 RID: 25
	public class NetRequest : INetRequest
	{
		// Token: 0x0600015A RID: 346 RVA: 0x00005488 File Offset: 0x00003688
		public NetRequest()
		{
			this.RequestFormat = DataFormat.Json;
			this.Method = Method.GET;
			this.Parameters = new List<Parameter>();
			this.Files = new List<FileParameter>();
			this.alloweDecompressionMethods = new List<DecompressionMethods>();
			this.Timeout = NetRequest.DETAULT_TIMEOUT;
		}

		// Token: 0x0600015B RID: 347 RVA: 0x000054DA File Offset: 0x000036DA
		public NetRequest(Method method)
			: this()
		{
			this.Method = method;
		}

		// Token: 0x0600015C RID: 348 RVA: 0x000054EC File Offset: 0x000036EC
		public NetRequest(string resource)
			: this(resource, Method.GET)
		{
		}

		// Token: 0x0600015D RID: 349 RVA: 0x000054F8 File Offset: 0x000036F8
		public NetRequest(string resource, Method method)
			: this()
		{
			this.Resource = resource;
			this.Method = method;
		}

		// Token: 0x0600015E RID: 350 RVA: 0x00005512 File Offset: 0x00003712
		public NetRequest(Uri resource)
			: this(resource, Method.GET)
		{
		}

		// Token: 0x0600015F RID: 351 RVA: 0x0000551E File Offset: 0x0000371E
		public NetRequest(Uri resource, Method method)
			: this(resource.IsAbsoluteUri ? (resource.AbsolutePath + resource.Query) : resource.OriginalString, method)
		{
		}

		// Token: 0x17000079 RID: 121
		// (get) Token: 0x06000160 RID: 352 RVA: 0x0000554C File Offset: 0x0000374C
		public IList<DecompressionMethods> AllowedDecompressionMethods
		{
			get
			{
				IList<DecompressionMethods> list2;
				if (!this.alloweDecompressionMethods.Any<DecompressionMethods>())
				{
					IList<DecompressionMethods> list = new DecompressionMethods[]
					{
						DecompressionMethods.None,
						DecompressionMethods.Deflate,
						DecompressionMethods.GZip
					};
					list2 = list;
				}
				else
				{
					list2 = this.alloweDecompressionMethods;
				}
				return list2;
			}
		}

		// Token: 0x1700007A RID: 122
		// (get) Token: 0x06000161 RID: 353 RVA: 0x0000557E File Offset: 0x0000377E
		// (set) Token: 0x06000162 RID: 354 RVA: 0x00005586 File Offset: 0x00003786
		public object UserState { get; set; }

		// Token: 0x1700007B RID: 123
		// (get) Token: 0x06000163 RID: 355 RVA: 0x0000558F File Offset: 0x0000378F
		// (set) Token: 0x06000164 RID: 356 RVA: 0x00005597 File Offset: 0x00003797
		public bool AlwaysMultipartFormData { get; set; }

		// Token: 0x1700007C RID: 124
		// (get) Token: 0x06000165 RID: 357 RVA: 0x000055A0 File Offset: 0x000037A0
		// (set) Token: 0x06000166 RID: 358 RVA: 0x000055A8 File Offset: 0x000037A8
		public Action<Stream> ResponseWriter { get; set; }

		// Token: 0x1700007D RID: 125
		// (get) Token: 0x06000167 RID: 359 RVA: 0x000055B1 File Offset: 0x000037B1
		// (set) Token: 0x06000168 RID: 360 RVA: 0x000055B9 File Offset: 0x000037B9
		public bool UseDefaultCredentials { get; set; }

		// Token: 0x06000169 RID: 361 RVA: 0x000055C4 File Offset: 0x000037C4
		public INetRequest AddFile(string name, string path, string contentType = null)
		{
			FileInfo fileInfo = new FileInfo(path);
			long length = fileInfo.Length;
			return this.AddFile(new FileParameter
			{
				Name = name,
				FileName = Path.GetFileName(path),
				ContentLength = length,
				Writer = delegate(Stream s)
				{
					using (StreamReader streamReader = new StreamReader(new FileStream(path, FileMode.Open)))
					{
						streamReader.BaseStream.CopyTo(s);
					}
				},
				ContentType = contentType
			});
		}

		// Token: 0x0600016A RID: 362 RVA: 0x00005640 File Offset: 0x00003840
		public INetRequest AddFile(string name, byte[] bytes, string fileName, string contentType = null)
		{
			return this.AddFile(FileParameter.Create(name, bytes, fileName, contentType));
		}

		// Token: 0x0600016B RID: 363 RVA: 0x00005664 File Offset: 0x00003864
		public INetRequest AddFile(string name, Action<Stream> writer, string fileName, long contentLength, string contentType = null)
		{
			return this.AddFile(new FileParameter
			{
				Name = name,
				Writer = writer,
				FileName = fileName,
				ContentLength = contentLength,
				ContentType = contentType
			});
		}

		// Token: 0x0600016C RID: 364 RVA: 0x000056AC File Offset: 0x000038AC
		public INetRequest AddFileBytes(string name, byte[] bytes, string filename, string contentType = "application/x-gzip")
		{
			long num = (long)bytes.Length;
			return this.AddFile(new FileParameter
			{
				Name = name,
				FileName = filename,
				ContentLength = num,
				ContentType = contentType,
				Writer = delegate(Stream s)
				{
					using (StreamReader streamReader = new StreamReader(new MemoryStream(bytes)))
					{
						streamReader.BaseStream.CopyTo(s);
					}
				}
			});
		}

		// Token: 0x0600016D RID: 365 RVA: 0x00005714 File Offset: 0x00003914
		public INetRequest AddBody(object obj, string contentType)
		{
			return this.AddParameter(contentType, obj, ParameterType.RequestBody);
		}

		// Token: 0x0600016E RID: 366 RVA: 0x00005730 File Offset: 0x00003930
		public INetRequest AddBody(object obj)
		{
			return this.AddBody(obj, "");
		}

		// Token: 0x0600016F RID: 367 RVA: 0x00005750 File Offset: 0x00003950
		public INetRequest AddJsonBody(object obj)
		{
			this.RequestFormat = DataFormat.Json;
			return this.AddBody(obj, "");
		}

		// Token: 0x06000170 RID: 368 RVA: 0x00005778 File Offset: 0x00003978
		public INetRequest AddXmlBody(object obj)
		{
			this.RequestFormat = DataFormat.Xml;
			return this.AddBody(obj, "");
		}

		// Token: 0x06000171 RID: 369 RVA: 0x000057A0 File Offset: 0x000039A0
		public INetRequest AddXmlBody(object obj, string xmlNamespace)
		{
			this.RequestFormat = DataFormat.Xml;
			return this.AddBody(obj, xmlNamespace);
		}

		// Token: 0x06000172 RID: 370 RVA: 0x000057C4 File Offset: 0x000039C4
		public INetRequest AddObject(object obj, params string[] includedProperties)
		{
			Type type = obj.GetType();
			PropertyInfo[] properties = type.GetProperties();
			foreach (PropertyInfo propertyInfo in properties)
			{
				bool flag = includedProperties.Length == 0 || (includedProperties.Length != 0 && includedProperties.Contains(propertyInfo.Name));
				bool flag2 = !flag;
				if (!flag2)
				{
					Type propertyType = propertyInfo.PropertyType;
					object obj2 = propertyInfo.GetValue(obj, null);
					bool flag3 = obj2 == null;
					if (!flag3)
					{
						bool isArray = propertyType.IsArray;
						if (isArray)
						{
							Type elementType = propertyType.GetElementType();
							bool flag4 = ((Array)obj2).Length > 0 && elementType != null && (elementType.IsPrimitive || elementType.IsValueType || elementType == typeof(string));
							if (flag4)
							{
								string[] array2 = (from object item in (Array)obj2
									select item.ToString()).ToArray<string>();
								obj2 = string.Join(",", array2);
							}
							else
							{
								obj2 = string.Join(",", (string[])obj2);
							}
						}
						this.AddParameter(propertyInfo.Name, obj2);
					}
				}
			}
			return this;
		}

		// Token: 0x06000173 RID: 371 RVA: 0x00005928 File Offset: 0x00003B28
		public INetRequest AddObject(object obj)
		{
			this.AddObject(obj, new string[0]);
			return this;
		}

		// Token: 0x06000174 RID: 372 RVA: 0x0000594C File Offset: 0x00003B4C
		public INetRequest AddParameter(Parameter p)
		{
			this.Parameters.Add(p);
			return this;
		}

		// Token: 0x06000175 RID: 373 RVA: 0x0000596C File Offset: 0x00003B6C
		public INetRequest AddParameter(string name, object value)
		{
			return this.AddParameter(new Parameter
			{
				Name = name,
				Value = value,
				Type = ParameterType.GetOrPost
			});
		}

		// Token: 0x06000176 RID: 374 RVA: 0x000059A4 File Offset: 0x00003BA4
		public INetRequest AddParameter(string name, object value, ParameterType type)
		{
			return this.AddParameter(new Parameter
			{
				Name = name,
				Value = value,
				Type = type
			});
		}

		// Token: 0x06000177 RID: 375 RVA: 0x000059DC File Offset: 0x00003BDC
		public INetRequest AddParameter(string name, object value, string contentType, ParameterType type)
		{
			return this.AddParameter(new Parameter
			{
				Name = name,
				Value = value,
				ContentType = contentType,
				Type = type
			});
		}

		// Token: 0x06000178 RID: 376 RVA: 0x00005A1C File Offset: 0x00003C1C
		public INetRequest AddOrUpdateParameter(Parameter p)
		{
			bool flag = this.Parameters.Any((Parameter param) => param.Name == p.Name);
			INetRequest netRequest;
			if (flag)
			{
				Parameter parameter = this.Parameters.First((Parameter param) => param.Name == p.Name);
				parameter.Value = p.Value;
				netRequest = this;
			}
			else
			{
				this.Parameters.Add(p);
				netRequest = this;
			}
			return netRequest;
		}

		// Token: 0x06000179 RID: 377 RVA: 0x00005A98 File Offset: 0x00003C98
		public INetRequest AddOrUpdateParameter(string name, object value)
		{
			return this.AddOrUpdateParameter(new Parameter
			{
				Name = name,
				Value = value,
				Type = ParameterType.GetOrPost
			});
		}

		// Token: 0x0600017A RID: 378 RVA: 0x00005AD0 File Offset: 0x00003CD0
		public INetRequest AddOrUpdateParameter(string name, object value, ParameterType type)
		{
			return this.AddOrUpdateParameter(new Parameter
			{
				Name = name,
				Value = value,
				Type = type
			});
		}

		// Token: 0x0600017B RID: 379 RVA: 0x00005B08 File Offset: 0x00003D08
		public INetRequest AddOrUpdateParameter(string name, object value, string contentType, ParameterType type)
		{
			return this.AddOrUpdateParameter(new Parameter
			{
				Name = name,
				Value = value,
				ContentType = contentType,
				Type = type
			});
		}

		// Token: 0x0600017C RID: 380 RVA: 0x00005B48 File Offset: 0x00003D48
		public INetRequest AddHeader(string name, string value)
		{
			Func<string, bool> func = (string host) => Uri.CheckHostName(Regex.Split(host, ":\\d+")[0]) == UriHostNameType.Unknown;
			bool flag = name == "Host" && func(value);
			if (flag)
			{
				throw new ArgumentException("The specified value is not a valid Host header string.", "value");
			}
			return this.AddParameter(name, value, ParameterType.HttpHeader);
		}

		// Token: 0x0600017D RID: 381 RVA: 0x00005BB0 File Offset: 0x00003DB0
		public INetRequest AddCookie(string name, string value)
		{
			return this.AddParameter(name, value, ParameterType.Cookie);
		}

		// Token: 0x0600017E RID: 382 RVA: 0x00005BCC File Offset: 0x00003DCC
		public INetRequest AddUrlSegment(string name, string value)
		{
			return this.AddParameter(name, value, ParameterType.UrlSegment);
		}

		// Token: 0x0600017F RID: 383 RVA: 0x00005BE8 File Offset: 0x00003DE8
		public INetRequest AddQueryParameter(string name, string value)
		{
			return this.AddParameter(name, value, ParameterType.QueryString);
		}

		// Token: 0x06000180 RID: 384 RVA: 0x00005C04 File Offset: 0x00003E04
		public INetRequest AddDecompressionMethod(DecompressionMethods decompressionMethod)
		{
			bool flag = !this.alloweDecompressionMethods.Contains(decompressionMethod);
			if (flag)
			{
				this.alloweDecompressionMethods.Add(decompressionMethod);
			}
			return this;
		}

		// Token: 0x1700007E RID: 126
		// (get) Token: 0x06000181 RID: 385 RVA: 0x00005C39 File Offset: 0x00003E39
		public List<Parameter> Parameters { get; }

		// Token: 0x1700007F RID: 127
		// (get) Token: 0x06000182 RID: 386 RVA: 0x00005C41 File Offset: 0x00003E41
		public List<FileParameter> Files { get; }

		// Token: 0x17000080 RID: 128
		// (get) Token: 0x06000183 RID: 387 RVA: 0x00005C49 File Offset: 0x00003E49
		// (set) Token: 0x06000184 RID: 388 RVA: 0x00005C51 File Offset: 0x00003E51
		public Method Method { get; set; }

		// Token: 0x17000081 RID: 129
		// (get) Token: 0x06000185 RID: 389 RVA: 0x00005C5A File Offset: 0x00003E5A
		// (set) Token: 0x06000186 RID: 390 RVA: 0x00005C62 File Offset: 0x00003E62
		public string Resource { get; set; }

		// Token: 0x17000082 RID: 130
		// (get) Token: 0x06000187 RID: 391 RVA: 0x00005C6B File Offset: 0x00003E6B
		// (set) Token: 0x06000188 RID: 392 RVA: 0x00005C73 File Offset: 0x00003E73
		public DataFormat RequestFormat { get; set; }

		// Token: 0x17000083 RID: 131
		// (get) Token: 0x06000189 RID: 393 RVA: 0x00005C7C File Offset: 0x00003E7C
		// (set) Token: 0x0600018A RID: 394 RVA: 0x00005C84 File Offset: 0x00003E84
		public ICredentials Credentials { get; set; }

		// Token: 0x17000084 RID: 132
		// (get) Token: 0x0600018B RID: 395 RVA: 0x00005C8D File Offset: 0x00003E8D
		// (set) Token: 0x0600018C RID: 396 RVA: 0x00005C95 File Offset: 0x00003E95
		public int Timeout { get; set; }

		// Token: 0x17000085 RID: 133
		// (get) Token: 0x0600018D RID: 397 RVA: 0x00005C9E File Offset: 0x00003E9E
		// (set) Token: 0x0600018E RID: 398 RVA: 0x00005CA6 File Offset: 0x00003EA6
		public int ReadWriteTimeout { get; set; }

		// Token: 0x0600018F RID: 399 RVA: 0x00005CB0 File Offset: 0x00003EB0
		public void IncreaseNumAttempts()
		{
			int attempts = this.Attempts;
			this.Attempts = attempts + 1;
		}

		// Token: 0x17000086 RID: 134
		// (get) Token: 0x06000190 RID: 400 RVA: 0x00005CCF File Offset: 0x00003ECF
		// (set) Token: 0x06000191 RID: 401 RVA: 0x00005CD7 File Offset: 0x00003ED7
		public int Attempts { get; private set; }

		// Token: 0x06000192 RID: 402 RVA: 0x00005CE0 File Offset: 0x00003EE0
		private INetRequest AddFile(FileParameter file)
		{
			this.Files.Add(file);
			return this;
		}

		// Token: 0x06000193 RID: 403 RVA: 0x00005D00 File Offset: 0x00003F00
		public INetRequest AddUrlSegment(string name, object value)
		{
			return this.AddParameter(name, value, ParameterType.UrlSegment);
		}

		// Token: 0x0400009F RID: 159
		public static int DETAULT_TIMEOUT = int.MaxValue;

		// Token: 0x040000A0 RID: 160
		private readonly IList<DecompressionMethods> alloweDecompressionMethods;
	}
}
