using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;

namespace Mcl.Core.Extensions;

// Token: 0x02000023 RID: 35
public static class CollectionExtensions
{
    // Token: 0x06000265 RID: 613 RVA: 0x00005D68 File Offset: 0x00003F68
    public static IEnumerable<T> AsEnumerable<T>(this T item)
    {
        return new[] { item };
    }

    // Token: 0x06000266 RID: 614 RVA: 0x00005D88 File Offset: 0x00003F88
    public static IEnumerable<T> And<T>(this T item, T other)
    {
        return new[] { item, other };
    }

    // Token: 0x06000267 RID: 615 RVA: 0x00005DB0 File Offset: 0x00003FB0
    public static IEnumerable<T> And<T>(this IEnumerable<T> items, T item)
    {
        foreach (var i in items) yield return i;
        yield return item;
        yield break;
    }

    // Token: 0x06000268 RID: 616 RVA: 0x00005DC8 File Offset: 0x00003FC8
    public static TK TryWithKey<T, TK>(this IDictionary<T, TK> dictionary, T key)
    {
        return dictionary.ContainsKey(key) ? dictionary[key] : default;
    }

    // Token: 0x06000269 RID: 617 RVA: 0x00005DF8 File Offset: 0x00003FF8
    public static IEnumerable<T> ToEnumerable<T>(this object[] items) where T : class
    {
        return items.Select(item => item as T);
    }

    // Token: 0x0600026A RID: 618 RVA: 0x00005E30 File Offset: 0x00004030
    public static void ForEach<T>(this IEnumerable<T> items, Action<T> action)
    {
        foreach (var t in items) action(t);
    }

    // Token: 0x0600026B RID: 619 RVA: 0x00005E80 File Offset: 0x00004080
    public static void AddRange(this IDictionary<string, string> collection, NameValueCollection range)
    {
        foreach (var text in range.AllKeys) collection.Add(text, range[text]);
    }

    // Token: 0x0600026C RID: 620 RVA: 0x00005EBC File Offset: 0x000040BC
    public static string ToQueryString(this NameValueCollection collection)
    {
        var stringBuilder = new StringBuilder();
        var flag = collection.Count > 0;
        if (flag) stringBuilder.Append("?");
        var num = 0;
        foreach (var text in collection.AllKeys)
        {
            stringBuilder.AppendFormat("{0}={1}", text, collection[text].UrlEncode());
            num++;
            var flag2 = num >= collection.Count;
            if (!flag2) stringBuilder.Append("&");
        }

        return stringBuilder.ToString();
    }
}