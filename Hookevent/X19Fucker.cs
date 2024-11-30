using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System;
using System.Net.WebSockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace DotNetTranstor.Hookevent
{
    internal class X19Fucker : IMethodHook
    {
        // 导入 AllocConsole 函数
        [DllImport("kernel32.dll")]
        public static extern bool AllocConsole();
        
        //X19_发烧平台绕过Hook
        [OriginalMethod]
        public static void X19_Fever_bypass()
        {
        }

        [CompilerGenerated]
        [HookMethod("WPFLauncher.Manager.PCChannel.asq", "b", "X19_Fever_bypass")]
        public bool b()
        {
            // 分配一个新的控制台
            AllocConsole();
            // 重定向输出流到控制台
            var writer = new StreamWriter(Console.OpenStandardOutput());
            writer.AutoFlush = true; // 设置为自动刷新，确保每次写入都立即输出
            Console.SetOut(writer);
            // 设置控制台输出编码为 UTF-8
            Console.OutputEncoding = Encoding.UTF8;
            Console.WriteLine("[INFO]控制台输出成功启动!");
            Console.WriteLine("[INFO]成功绕过发烧平台!");
            return false;
        }
        
    }
}