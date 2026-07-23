using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using WPFLauncher.Common;
using WPFLauncher.Util;

namespace Mcl.Core.Dotnetdetour.Features.IdentitySpoofing;

// Token: 0x020006EC RID: 1772
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

    // Token: 0x17000C77 RID: 3191
    // (get) Token: 0x06003074 RID: 12404 RVA: 0x0001805C File Offset: 0x0001625C
    private static string ResourcePath => AppDomain.CurrentDomain.BaseDirectory + "\\Resource\\Res\\";

    // Token: 0x06003075 RID: 12405 RVA: 0x000B52E0 File Offset: 0x000B34E0
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

    // Token: 0x06003076 RID: 12406 RVA: 0x000B535C File Offset: 0x000B355C
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

    // Token: 0x06003077 RID: 12407 RVA: 0x000B53D8 File Offset: 0x000B35D8
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

    // Token: 0x06003078 RID: 12408 RVA: 0x000B5444 File Offset: 0x000B3644
    public static string GenerateRandomName(string exclude = null)
    {
        return GenerateWithCheck(GenerateRandomNameInternal, exclude, IsCanGenerateName);
    }

    // Token: 0x06003079 RID: 12409 RVA: 0x000B5488 File Offset: 0x000B3688
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

    // Token: 0x0600307A RID: 12410 RVA: 0x000B555C File Offset: 0x000B375C
    private static string GeneratePreNameV()
    {
        return GetRandomWord("pre") + GetRandomWord("name") + GetRandomWord("v");
    }

    // Token: 0x0600307B RID: 12411 RVA: 0x000B5594 File Offset: 0x000B3794
    private static string GeneratePreSubV()
    {
        return GetRandomWord("pre") + GetRandomWord("sub") + GetRandomWord("v");
    }

    // Token: 0x0600307C RID: 12412 RVA: 0x000B55CC File Offset: 0x000B37CC
    private static string GenerateAdjNameV()
    {
        return GetRandomWord("adj") + GetRandomWord("name") + GetRandomWord("v");
    }

    // Token: 0x0600307D RID: 12413 RVA: 0x000B5604 File Offset: 0x000B3804
    private static string GenerateAdjSubV()
    {
        return GetRandomWord("adj") + GetRandomWord("sub") + GetRandomWord("v");
    }

    // Token: 0x0600307E RID: 12414 RVA: 0x000B563C File Offset: 0x000B383C
    private static string GenerateVPreName()
    {
        return GetRandomWord("v") + "的" + GetRandomWord("pre") + GetRandomWord("name");
    }

    // Token: 0x0600307F RID: 12415 RVA: 0x000B5678 File Offset: 0x000B3878
    private static string GenerateVPreSub()
    {
        return GetRandomWord("v") + "的" + GetRandomWord("pre") + GetRandomWord("sub");
    }

    // Token: 0x06003080 RID: 12416 RVA: 0x000B56B4 File Offset: 0x000B38B4
    private static string GenerateNameSubV()
    {
        return GetRandomWord("name") + "的" + GetRandomWord("sub") + GetRandomWord("v");
    }

    // Token: 0x06003081 RID: 12417 RVA: 0x000B56F0 File Offset: 0x000B38F0
    public static string GetLoadingTip(string exclude = null)
    {
        return GenerateWithCheck(GetRandomLoadingTip, exclude, null, false);
    }

    // Token: 0x06003082 RID: 12418 RVA: 0x000B5714 File Offset: 0x000B3914
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