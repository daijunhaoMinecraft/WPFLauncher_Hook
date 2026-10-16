using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace Mcl.Core.Updater
{
    /// <summary>
    /// 在专用 STA 线程上显示更新窗口并等待关闭（启动器更新与 Mcl.Core.dll 自更新共用）。
    /// </summary>
    internal static class UpdateWindowRunner
    {
        public static Task<TResult> RunAsync<TWindow, TResult>(Func<TWindow> windowFactory, Func<TWindow, TResult> getResult)
            where TWindow : Window
        {
            var completion = new TaskCompletionSource<TResult>();

            var uiThread = new Thread(() =>
            {
                try
                {
                    System.Windows.Media.RenderOptions.ProcessRenderMode = System.Windows.Interop.RenderMode.SoftwareOnly;
                    if (Application.Current == null)
                    {
                        new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
                    }

                    var window = windowFactory();
                    window.Closed += (_, __) =>
                    {
                        try
                        {
                            completion.TrySetResult(getResult(window));
                        }
                        catch (Exception ex)
                        {
                            completion.TrySetException(ex);
                        }
                        finally
                        {
                            Dispatcher.ExitAllFrames();
                        }
                    };

                    window.Show();
                    Dispatcher.Run();
                }
                catch (Exception ex)
                {
                    completion.TrySetException(ex);
                }
            });

            uiThread.SetApartmentState(ApartmentState.STA);
            uiThread.IsBackground = true;
            uiThread.Start();
            return completion.Task;
        }

        public static Task RunAsync<TWindow>(Func<TWindow> windowFactory) where TWindow : Window
        {
            return RunAsync(windowFactory, _ => true);
        }
    }
}
