using System;
using System.Runtime.CompilerServices;
using WPFLauncher;
using MicrosoftTranslator.DotNetTranstor.Tools;

namespace DotNetTranstor.Hookevent
{
    /// <summary>
    /// 网易敏感词检测拦截类
    /// 整合了所有敏感词检测相关的拦截功能
    /// </summary>
    internal class SensitiveWordDetection : IMethodHook
    {
        #region 初始化敏感词功能拦截

        [OriginalMethod]
        public static void No_Sensitive_word_Init()
        {
        }

        [CompilerGenerated]
        [HookMethod("WPFLauncher.cn", "a", "No_Sensitive_word_Init")]
        public static void InitHookA()
        {
            if (Path_Bool.IsDebug)
            {
                Console.WriteLine("[INFO]发现网易正在初始化敏感词功能已被制止");
            }
        }

        [CompilerGenerated]
        [HookMethod("WPFLauncher.cn", "b", "No_Sensitive_word_Init")]
        public static void InitHookB()
        {
            if (Path_Bool.IsDebug)
            {
                Console.WriteLine("[INFO]发现网易正在初始化敏感词功能已被制止");
            }
        }

        #endregion

        #region 敏感词检测拦截 - 字符串返回类型

        [OriginalMethod]
        public static string No_Sensitive_word_String(string content)
        {
            return content;
        }

        [CompilerGenerated]
        [HookMethod("WPFLauncher.cn", "c", "No_Sensitive_word_String")]
        public static string StringHookC(string content)
        {
            if (Path_Bool.IsDebug)
            {
                Console.WriteLine($"[INFO]发现网易检测敏感词已被制止,检测的内容为:{content}");
            }
            return content;
        }

        [CompilerGenerated]
        [HookMethod("WPFLauncher.cn", "e", "No_Sensitive_word_String")]
        public static string StringHookE(string content)
        {
            if (Path_Bool.IsDebug)
            {
                Console.WriteLine($"[INFO]发现网易检测敏感词已被制止,检测的内容为:{content}");
            }
            return content;
        }

        #endregion

        #region 敏感词检测拦截 - 布尔返回类型

        [OriginalMethod]
        public static bool No_Sensitive_word_Bool(string content)
        {
            return true;
        }

        [CompilerGenerated]
        [HookMethod("WPFLauncher.cn", "d", "No_Sensitive_word_Bool")]
        public static bool BoolHookD(string content)
        {
            if (Path_Bool.IsDebug)
            {
                Console.WriteLine($"[INFO]发现网易检测敏感词已被制止,检测的内容为:{content}");
            }
            return true;
        }

        [CompilerGenerated]
        [HookMethod("WPFLauncher.cn", "f", "No_Sensitive_word_Bool")]
        public static bool BoolHookF(string content)
        {
            if (Path_Bool.IsDebug)
            {
                Console.WriteLine($"[INFO]发现网易检测敏感词已被制止,检测的内容为:{content}");
            }
            return true;
        }

        #endregion
    }
}