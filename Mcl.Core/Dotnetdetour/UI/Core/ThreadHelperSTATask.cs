using System;
using System.Threading;

namespace Mcl.Core.Dotnetdetour.UI.Core
{
    public static class ThreadHelperSTATask
    {
        /// <summary>
        /// 在一个独立的 STA 线程中运行代码，专门用于在非 UI 线程中呼出 WPF 窗口
        /// </summary>
        public static void Run(Action worker)
        {
            var thread = new Thread(() => worker())
            {
                IsBackground = true
            };
            // 必须设置为 STA，否则 WPF Window.ShowDialog() 会崩溃
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join(); // 挂起调用者线程，直到 UI 线程结束
        }
    }
}