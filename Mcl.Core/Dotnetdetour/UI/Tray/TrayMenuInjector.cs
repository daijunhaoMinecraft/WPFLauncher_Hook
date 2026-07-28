using System;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Mcl.Core.Dotnetdetour.Features.GeneralHooks;
using Mcl.Core.Dotnetdetour.Models.Config;
using Mcl.Core.Network.Interface;
using Mcl.Core.Tools;
using Mcl.Core.Updater;
using Newtonsoft.Json;
using WPFLauncher.Common;
using WPFLauncher.Manager.Configuration;
using WPFLauncher.Network.Launcher;
using WPFLauncher.Network.Protocol;

namespace Mcl.Core.Dotnetdetour.UI.Tray
{
    public static class TrayMenuInjector
    {
        public static void Start()
        {
            Thread watcherThread = new Thread(() =>
            {
                bool isInjected = false;

                while (!isInjected)
                {
                    Thread.Sleep(1000);

                    if (Application.Current == null || Application.Current.Dispatcher == null)
                        continue;

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        try
                        {
                            foreach (Window window in Application.Current.Windows)
                            {
                                if (window.GetType().Name == "MainWindow")
                                {
                                    ContextMenu contextMenu = window.FindResource("NotifyIconMenu") as ContextMenu;
                                    
                                    if (contextMenu != null)
                                    {
                                        // 使用 Tag 属性来判断是否已经注入过
                                        bool isAlreadyAdded = false;
                                        foreach (var item in contextMenu.Items)
                                        {
                                            if (item is MenuItem mi && mi.Tag != null && mi.Tag.ToString() == "MclInjected")
                                            {
                                                isAlreadyAdded = true;
                                                break;
                                            }
                                        }

                                        if (!isAlreadyAdded)
                                        {
                                            // 1. 插件设置按钮
                                            MenuItem settingsMenu = CreateNativeStyledMenuItem("插件设置", "⚙", Brushes.DodgerBlue);
                                            settingsMenu.PreviewMouseLeftButtonDown += (s, e) => 
                                            {
                                                e.Handled = true; // 拦截事件，防止主程序捣乱
                                                contextMenu.IsOpen = false; // 手动关闭右键菜单
                                                InitHook.ShowConfigWindow();
                                                // 3. 应用运行逻辑
                                                InitHook.ApplyRuntimeSettings();
                                            };

                                            // 2. 更新检查按钮
                                            MenuItem updateMenu = CreateNativeStyledMenuItem("更新检查", "🔄", Brushes.ForestGreen);
                                            updateMenu.PreviewMouseLeftButtonDown += (s, e) => 
                                            {
                                                e.Handled = true;
                                                contextMenu.IsOpen = false;
                                                UpdateManager.ShowConfigUI(); 
                                            };

                                            // 插入到最上方，加一根分割线
                                            contextMenu.Items.Insert(0, settingsMenu);
                                            contextMenu.Items.Insert(1, updateMenu);
                                            contextMenu.Items.Insert(2, new Separator { Margin = new Thickness(0, 2, 0, 2) });
                                            
                                            WpfConfig.DefaultLogger.Info("[UI] 成功复刻宿主样式并注入托盘菜单！");
                                        }

                                        isInjected = true; 
                                        break; 
                                    }
                                }
                            }
                        }
                        catch
                        {
                            // 忽略轮询异常
                        }
                    });
                }
            });
            
            watcherThread.IsBackground = true;
            watcherThread.Start();
        }

        // ==========================================
        // 核心：完全 1:1 还原宿主的 XAML 结构
        // ==========================================
        private static MenuItem CreateNativeStyledMenuItem(string text, string emojiIcon, Brush textColor)
        {
            var menuItem = new MenuItem();
            
            // 打个标记，防止重复注入
            menuItem.Tag = "MclInjected"; 

            // 1. 还原 <MenuItem.Icon>
            // XAML中为: <Image Width="12" Height="12" Margin="10,0,0,0" />
            // 我们没有他的静态图片资源，用 TextBlock 塞个 emoji 顶替，但尺寸和边距绝对保持一致
            var iconBlock = new TextBlock
            {
                Text = emojiIcon,
                Width = 12,
                Height = 12,
                Margin = new Thickness(10, 0, 0, 0),
                FontSize = 12,
                Foreground = textColor,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            menuItem.Icon = iconBlock;

            // 2. 还原 Header
            var headerBlock = new TextBlock
            {
                Text = text,
                Foreground = textColor,
                VerticalAlignment = VerticalAlignment.Center
            };
            menuItem.Header = headerBlock;

            return menuItem;
        }
    }
}