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
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace DotNetTranstor.Hookevent
{
    internal class X19Fucker : IMethodHook
    {
        //X19_发烧平台绕过Hook
        [OriginalMethod]
        public static void X19_Fever_bypass()
        {
        }

        [CompilerGenerated]
        [HookMethod("WPFLauncher.Manager.PCChannel.asq", "b", "X19_Fever_bypass")]
        public bool b()
        {
            return false;
            X19Fucker.X19_Fever_bypass();
        }
        
    }
}