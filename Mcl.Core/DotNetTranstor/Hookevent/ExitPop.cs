using System;
using System.Reflection;
using System.Windows;
using DotNetTranstor;
using DotNetTranstor.Hookevent;
using WPFLauncher.Common;
using WPFLauncher.Manager;
using WPFLauncher.View.Launcher.LobbyGame;

namespace Mcl.Core.DotNetTranstor.Hookevent;

public class ExitPop : IMethodHook
{
    [OriginalMethod]
    private void ExitRoomPrivate(object args)
    {
        
    }
    [HookMethod("WPFLauncher.ViewModel.LobbyGame.jp", "o","ExitRoomPrivate")]
    private void ExitRoom(object args)
    {
        if (!Path_Bool.NoTwoExitMessage)
        { 
            ExitRoomPrivate(args);
            return;
        }
        // 使用反射方法直接关闭
        LobbyGameRoomManagerView lobbyGameRoomManagerView = aze<apn>.Instance.k<LobbyGameRoomManagerView>();
        if (lobbyGameRoomManagerView != null)
        {
            // 使用Dispatcher确保在UI线程上访问DataContext
            object roomManageMainWindow = null;
            Type dataContextType = null;
            bool invokeSuccess = false;
            
            // 在UI线程上获取DataContext
            Application.Current.Dispatcher.Invoke(() => {
                if (lobbyGameRoomManagerView.DataContext != null)
                {
                    roomManageMainWindow = lobbyGameRoomManagerView.DataContext;
                    dataContextType = roomManageMainWindow.GetType();
                    invokeSuccess = true;
                }
            });
            
            // 如果成功获取到DataContext，则继续执行
            if (invokeSuccess && roomManageMainWindow != null && dataContextType != null)
            {
                // 获取基类类型
                Type baseType = dataContextType.BaseType;
                
                // 使用Dispatcher在UI线程上调用方法
                Application.Current.Dispatcher.Invoke(() => {
                    // 查找 View 属性
                    PropertyInfo viewProperty = baseType.GetProperty("View", 
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    
                    if (viewProperty != null)
                    {
                        // 获取 View 对象
                        object viewObject = viewProperty.GetValue(roomManageMainWindow);
                        if (viewObject != null)
                        {
                            // 查找 c 方法
                            MethodInfo cMethod = viewObject.GetType().GetMethod("c",
                                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                            
                            if (cMethod != null)
                            {
                                // 调用 c() 方法
                                cMethod.Invoke(viewObject, new object[] { });
                            }
                        }
                    }
                });
            }
        }
    }
}