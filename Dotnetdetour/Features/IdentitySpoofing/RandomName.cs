using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using WPFLauncher.Common;
using WPFLauncher.Util;

namespace Mcl.Core.Dotnetdetour.Tools;

public class RandomNameGenerator
{
    private const int MAX_ATTEMPTS = 10000;

    private static readonly Dictionary<string, Dictionary<string, Dictionary<string, string>>> dictCache = new();

    private static readonly Random random =
        new((int)(DateTime.Now.Ticks & 4294967295L) | (int)(DateTime.Now.Ticks >> 32));

    private static readonly int[] weights = new[] { 1, 1, 1, 1, 1, 1, 1 };

    private static readonly NameGenerator[] generators = new[]
    {
        GeneratePreNameV,
        GeneratePreSubV,
        GenerateAdjNameV,
        GenerateAdjSubV,
        GenerateVPreName,
        GenerateVPreSub,
        new NameGenerator(GenerateNameSubV)
    };

    // (get) Token: 0x06003074 RID: 12404 RVA: 0x0001805C File Offset: 0x0001625C
    private static string ResourcePath => AppDomain.CurrentDomain.BaseDirectory + "\\Resource\\Res\\";

    private static bool LoadDictionary(string dictName)
    {
        Dictionary<string, Dictionary<string, string>> dictionary = null;
        bool flag;
        if (dictCache.TryGetValue(dictName, out dictionary) && dictionary.Count > 0)
        {
            flag = true;
        }
        else
        {
            try
            {
                var text = ResourcePath + dictName + ".json";
                var value = we.i(text).value;
                dictionary = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, string>>>(value);
                dictCache.Add(dictName, dictionary);
            }
            catch (Exception)
            {
                return false;
            }

            flag = true;
        }

        return flag;
    }

    private static string GetRandomWord(string dictName, int index = 0)
    {
        Dictionary<string, Dictionary<string, string>> dictionary = null;
        dictCache.TryGetValue(dictName, out dictionary);
        string text;
        if (dictionary == null)
        {
            text = null;
        }
        else
        {
            var count = dictionary.Count;
            var num = random.Next(1, count + 1);
            if (index != 0) num = index;
            Dictionary<string, string> dictionary2 = null;
            dictionary.TryGetValue(num.ToString(), out dictionary2);
            if (dictionary2 == null)
            {
                text = null;
            }
            else
            {
                string text2 = null;
                dictionary2.TryGetValue("word", out text2);
                text = text2;
            }
        }

        return text;
    }

    private static string GenerateWithCheck(Func<string> generator, string exclude = null,
        Func<string, bool> validator = null, bool checkDuplicate = true)
    {
        var i = 0;
        while (i < MAX_ATTEMPTS)
        {
            var text = generator();
            if (!string.IsNullOrEmpty(text))
            {
                if ((!checkDuplicate || !azf<ul>.Instance.g(text)) && !(text == exclude))
                {
                    if (validator == null) return text;
                    if (validator(text)) return text;
                }

                i++;
                continue;
            }

            return null;
        }

        return null;
    }

    public static string GenerateRandomName(string exclude = null)
    {
        return GenerateWithCheck(GenerateRandomNameInternal, exclude, IsCanGenerateName);
    }

    private static string GenerateRandomNameInternal()
    {
        string text;
        if (!LoadDictionary("adj") || !LoadDictionary("v") || !LoadDictionary("pre") || !LoadDictionary("sub") ||
            !LoadDictionary("item") || !LoadDictionary("name"))
        {
            text = null;
        }
        else
        {
            var num = 0;
            foreach (var num2 in weights) num += num2;
            var num3 = random.Next(0, num);
            num = 0;
            for (var j = 0; j < weights.Length; j++)
            {
                num += weights[j];
                if (num3 < num) return generators[j]();
            }

            text = null;
        }

        return text;
    }

    private static string GeneratePreNameV()
    {
        return GetRandomWord("pre") + GetRandomWord("name") + GetRandomWord("v");
    }

    private static string GeneratePreSubV()
    {
        return GetRandomWord("pre") + GetRandomWord("sub") + GetRandomWord("v");
    }

    private static string GenerateAdjNameV()
    {
        return GetRandomWord("adj") + GetRandomWord("name") + GetRandomWord("v");
    }

    private static string GenerateAdjSubV()
    {
        return GetRandomWord("adj") + GetRandomWord("sub") + GetRandomWord("v");
    }

    private static string GenerateVPreName()
    {
        return GetRandomWord("v") + "的" + GetRandomWord("pre") + GetRandomWord("name");
    }

    private static string GenerateVPreSub()
    {
        return GetRandomWord("v") + "的" + GetRandomWord("pre") + GetRandomWord("sub");
    }

    private static string GenerateNameSubV()
    {
        return GetRandomWord("name") + "的" + GetRandomWord("sub") + GetRandomWord("v");
    }

    public static string GetLoadingTip(string exclude = null)
    {
        return GenerateWithCheck(GetRandomLoadingTip, exclude, null, false);
    }

    private static string GetRandomLoadingTip()
    {
        string text;
        if (!LoadDictionary("loadingtips"))
            text = null;
        else
            text = GetRandomWord("loadingtips");
        return text;
    }

    public static bool IsCanGenerateName(string word)
    {
        var num = ur.a(word);
        return num >= 3f && num <= 12f;
    }

    private delegate string NameGenerator();
}