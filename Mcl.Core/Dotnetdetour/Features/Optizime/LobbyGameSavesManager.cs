using System;
using System.Linq;
using System.Reflection;
using System.Windows;
using Mcl.Core.Dotnetdetour.CoreEngine.Attributes;
using Mcl.Core.Dotnetdetour.CoreEngine.Interfaces;
using Mcl.Core.Dotnetdetour.Models.Config;
using Mcl.Core.Dotnetdetour.UI.Windows;
using Mcl.Core.NeteaseProtocol;
using WPFControls.Helpers;
using WPFLauncher.Manager.Game;
using WPFLauncher.Model.Component;
using WPFLauncher.Model.LobbyGame;
using WPFLauncher.ViewModel.Launcher;
using WPFLauncher.ViewModel.LobbyGame;
namespace Mcl.Core.Dotnetdetour.Features.Optizime;

public class LobbyGameSavesManager : IMethodHook
{
    [OriginalMethod]
    private void LoadSavesEvent(object sender)
    {
        
    }
    
    [HookMethod("WPFLauncher.ViewModel.Launcher.kg", "g", "LoadSavesEvent")]
    private void LoadSavesEventHook(object sender)
    {
        if (WpfConfig.AdvancedSavesManager)
        {
            // 使用 Dispatcher 确保在主 UI 线程上创建和操作 WPF 窗口
            Application.Current.Dispatcher.Invoke(() =>
            {
                // 1. 创建窗口实例
                var saveWindow = new SaveManagerWindow();
                bool? result = saveWindow.ShowDialog();

                // 3. 判断用户是点击了“载入存档”还是“取消/关闭”
                if (result == true)
                {
                    var selectedSave = saveWindow.SelectedSaveData;
                    
                    if (selectedSave != null)
                    {
                        OnlineMapM onlineMapM = WPFLauncher.Common.azf<aum>.Instance.LobbyMaps.SingleOrDefault<OnlineMapM>(
                            delegate(OnlineMapM mapM)
                            {
                                return mapM.ID == selectedSave.ResId;
                            });

                        aks selectedBackup = new aks();
                        selectedBackup.BackupId = selectedSave.BackupId.ToString();
                        selectedBackup.SaveId = selectedSave.SaveId.ToString();
                        selectedBackup.BackupTimestamp = (ulong)selectedSave.OriginalData.Timestamp;
                        selectedBackup.ExpiredTimeStamp = (ulong)selectedSave.OriginalData.ExpireTime;
                        selectedBackup.Name = selectedSave.Name;
                        selectedBackup.Size = (ulong)selectedSave.OriginalData.Size;
                        selectedBackup.ItemId = selectedSave.ResId;

                        // ================== 反射调用开始 ==================
                        try
                        {
                            // 1. 获取内部类 WPFLauncher.Manager.aqg 的 Type
                            Type aqgType = typeof(LaunchGamePage).Assembly.GetType("WPFLauncher.Manager.aqg");

                            // 2. 获取 Singleton<aqg>.Instance 属性的值
                            PropertyInfo instanceProp = aqgType.BaseType.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy) 
                                                     ?? aqgType.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
                            object aqgInstance = instanceProp.GetValue(null);

                            // 3. 获取页面管理器 f
                            object fManager = null;
                            FieldInfo fField = aqgType.GetField("f", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                            if (fField != null) fManager = fField.GetValue(aqgInstance);
                            else 
                            {
                                PropertyInfo fProp = aqgType.GetProperty("f", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                                fManager = fProp.GetValue(aqgInstance);
                            }

                            // 构建传递的参数元组
                            var tupleArgs = new Tuple<OnlineMapM, aks, WPFLauncher.Common.ayx<OnlineMapM>>(
                                onlineMapM, selectedBackup, WPFLauncher.Common.azf<aum>.Instance.LobbyMaps);
                                
                            // 🌟 修复此处：通过获取所有方法并筛选参数数量为 3 的方法，解决“不明确的匹配”问题
                            MethodInfo cMethod = fManager.GetType()
                                .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                                .FirstOrDefault(m => m.Name == "c" && m.GetParameters().Length == 3);

                            if (cMethod == null)
                            {
                                MessageBox.Show("未能找到对应的页面跳转方法 c，可能版本已更新。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                                return;
                            }

                            // 唤起重载跳转方法
                            cMethod.Invoke(fManager, new object[] { LaunchGamePage.CREATE_LOBBY_GAME_PAGE, tupleArgs, -1 });

                            // 4. 获取目标页面的 ViewModel 并锁定地图选择
                            MethodInfo fMethod = aqgType.GetMethod("f", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, new Type[] { typeof(LaunchGamePage) }, null);
                            object joInstance = fMethod.Invoke(aqgInstance, new object[] { LaunchGamePage.CREATE_LOBBY_GAME_PAGE });

                            PropertyInfo canSelectProp = joInstance.GetType().GetProperty("CanSelectedOnlineMap", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                            if (canSelectProp != null)
                            {
                                canSelectProp.SetValue(joInstance, false);
                            }
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"页面跳转失败 (反射异常):\n{ex.Message}\n{ex.StackTrace}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                        // ================== 反射调用结束 ==================
                    }
                }
                else
                {
                }
            });
        }
        else
        {
            LoadSavesEvent(sender);
        }
    }

    [OriginalMethod]
    private void SaveSavesEvent(object sender)
    {
        
    }

    [HookMethod("WPFLauncher.ViewModel.LobbyGame.jp", "i", "SaveSavesEvent")]
    private void SaveSavesEventHook(object sender)
    {
        if (WpfConfig.AdvancedSavesManager)
        {
            // 使用 Dispatcher 强制切回主 UI 线程
            Application.Current.Dispatcher.Invoke(() =>
            {
                try
                {
                    // 1. 创建我们编写的保存/备份窗口
                    var backupWindow = new SaveBackupWindow();
                
                    // 可选：将网易启动器的主窗口设置为父窗口，这样在备份窗口打开时，用户无法点击背后的启动器界面
                    if (Application.Current.MainWindow != null)
                    {
                        backupWindow.Owner = Application.Current.MainWindow;
                    }

                    // 2. 以模态方式显示窗口 (阻塞背后的界面直到窗口关闭)
                    backupWindow.ShowDialog();
                }
                catch (Exception ex)
                {
                    // 加上 try-catch 防止意外报错导致整个启动器崩溃
                    MessageBox.Show($"打开保存存档界面失败:\n{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            });
        }
        else
        {
            SaveSavesEvent(sender);
        }
    }
}