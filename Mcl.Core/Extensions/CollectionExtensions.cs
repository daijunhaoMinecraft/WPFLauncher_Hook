using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;

namespace Mcl.Core.Extensions;

public static class CollectionExtensions
{
    public static IEnumerable<T> AsEnumerable<T>(this T item)
    {
        return new[] { item };
    }

    public static IEnumerable<T> And<T>(this T item, T other)
    {
        return new[] { item, other };
    }

    public static IEnumerable<T> And<T>(this IEnumerable<T> items, T item)
    {
        foreach (var i in items) yield return i;
        yield return item;
        yield break;
    }

    public static TK TryWithKey<T, TK>(this IDictionary<T, TK> dictionary, T key)
    {
        return dictionary.ContainsKey(key) ? dictionary[key] : default;
    }

    public static IEnumerable<T> ToEnumerable<T>(this object[] items) where T : class
    {
        return items.Select(item => item as T);
    }

    public static void ForEach<T>(this IEnumerable<T> items, Action<T> action)
    {
        foreach (var t in items) action(t);
    }

    public static void AddRange(this IDictionary<string, string> collection, NameValueCollection range)
    {
        foreach (var text in range.AllKeys) collection.Add(text, range[text]);
    }

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