using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Newtonsoft.Json;
using DotNetTranstor.Tools;
using Newtonsoft.Json.Linq;
using WPFLauncher.Common;
using WPFLauncher.Manager.Configuration;
using WPFLauncher.Util;

namespace DotNetTranstor.Hookevent
{
    internal class BypassFever : IMethodHook
    {
        [DllImport("kernel32.dll")]
        private static extern bool AllocConsole();

        [OriginalMethod]
        public static void X19_Fever_bypass() { }

        [HookMethod("WPFLauncher.Manager.apm", "aj", "X19_Fever_bypass")]
        public bool Fever_False()
        {
            return false;
        }
    }
}