using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

// 引用目标窗口和控件命名空间
using WPFLauncher.View.SysSetting;
using WPFControls.Controls.CustomButton;
using WPFControls.Controls.CustomCheckBox;
using WPFControls.Helpers;

// 引入你的配置管理器命名空间
using Mcl.Core.Dotnetdetour.Models.Config;
// 如果 WpfConfig 在其他命名空间，请在这里 using 它 (例如 Mcl.Core.Dotnetdetour.Config)

namespace Mcl.Core.Dotnetdetour.UI.Injector
{
    public class SettingsInjector
    {
        public static void Start()
        {
            Thread checkThread = new Thread(CheckWindowLoop)
            {
                IsBackground = true,
                Name = "SysSettingWindowChecker"
            };
            checkThread.Start();
        }

        private static void CheckWindowLoop()
        {
            while (true)
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
                            if (window is SysSettingMainPage targetWindow)
                            {
                                InjectCustomControls(targetWindow);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("注入设置界面时发生异常: " + ex.Message);
                    }
                });
            }
        }

        private static void InjectCustomControls(SysSettingMainPage window)
        {
            FieldInfo tabControlField = typeof(SysSettingMainPage).GetField("SetTabControl",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

            if (tabControlField == null) return;
            TabControl setTabControl = tabControlField.GetValue(window) as TabControl;
            if (setTabControl == null) return;

            string customTabHeader = "MCL 扩展设置";
            foreach (object item in setTabControl.Items)
            {
                if (item is TabItem tab && tab.Header != null && tab.Header.ToString() == customTabHeader)
                {
                    return; // 已经注入过，直接退出
                }
            }

            // 1. 【抽取官方 UI 样式与图标资源】
            Style leftTabItemStyle = window.TryFindResource("LeftTabItemStyle") as Style;
            Style checkBoxStyle = window.TryFindResource("CheckBoxStyle02") as Style;
            Style buttonStyle = window.TryFindResource("SimpleButtonStyle02_142x30") as Style;
            Brush textColorA = window.TryFindResource("TextColorA") as Brush ?? Brushes.White;
            
            // 🌟 更换图标：改用 icon29（官方自带的齿轮图标）或者 icon45(音频) / icon46(账户)
            ImageSource tabIcon = window.TryFindResource("icon29") as ImageSource; 

            // 2. 【创建 TabItem】
            TabItem customTab = new TabItem
            {
                Header = customTabHeader,
                Cursor = Cursors.Hand,
                Style = leftTabItemStyle
            };

            if (tabIcon != null)
            {
                AttachPropertyHelper.SetTabItemImage(customTab, tabIcon);
            }

            // 3. 【创建内容视图】
            ScrollViewer scrollViewer = new ScrollViewer
            {
                Margin = new Thickness(40, 20, 0, 0),
                Cursor = Cursors.Arrow,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };

            StackPanel mainPanel = new StackPanel { Orientation = Orientation.Vertical };
            
            // 标题
            mainPanel.Children.Add(new Label
            {
                Content = "MCL 高级核心配置",
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                Foreground = textColorA,
                Margin = new Thickness(0, 0, 0, 15)
            });

            // 4. 【动态生成配置项 UI】
            // 用字典存储生成的控件，方便保存时读取
            Dictionary<string, FrameworkElement> controlMap = new Dictionary<string, FrameworkElement>();
            var categories = ConfigManager.Registry.Select(x => x.Category).Distinct();

            foreach (var cat in categories)
            {
                // 分类标题
                mainPanel.Children.Add(new Label
                {
                    Content = $"[{cat}]",
                    FontSize = 14,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = textColorA,
                    Margin = new Thickness(0, 10, 0, 5)
                });

                // 遍历当前分类下的配置
                foreach (var item in ConfigManager.Registry.Where(x => x.Category == cat))
                {
                    // 反射获取 WpfConfig 里的当前值
                    var field = typeof(WpfConfig).GetField(item.Key, BindingFlags.Public | BindingFlags.Static);
                    object val = field?.GetValue(null);

                    if (item.FieldType == typeof(bool))
                    {
                        // 布尔值 -> 官方复选框
                        SimpleCheckBox cb = new SimpleCheckBox
                        {
                            Content = item.Description,
                            Style = checkBoxStyle,
                            IsChecked = val as bool? ?? false,
                            Margin = new Thickness(15, 6, 0, 6) // 左侧缩进 15 区分层级
                        };
                        mainPanel.Children.Add(cb);
                        controlMap[item.Key] = cb;
                    }
                    else
                    {
                        // 其他值 -> Label + TextBox
                        StackPanel sp = new StackPanel { Margin = new Thickness(15, 6, 0, 6) };
                        sp.Children.Add(new Label
                        {
                            Content = item.Description, 
                            Foreground = textColorA,
                            FontSize = 12,
                            Padding = new Thickness(0,0,0,2)
                        });
                        TextBox tb = new TextBox
                        {
                            Text = val?.ToString(),
                            Width = 260,
                            Height = 26,
                            HorizontalAlignment = HorizontalAlignment.Left,
                            VerticalContentAlignment = VerticalAlignment.Center,
                            Padding = new Thickness(4,0,4,0),
                            // 简单的透明化样式融入官方 UI
                            Background = new SolidColorBrush(Color.FromArgb(30, 255, 255, 255)),
                            Foreground = textColorA,
                            BorderBrush = new SolidColorBrush(Color.FromArgb(100, 150, 150, 150))
                        };
                        sp.Children.Add(tb);
                        mainPanel.Children.Add(sp);
                        controlMap[item.Key] = tb;
                    }
                }
            }
            
            mainPanel.Children.Add(new Label
            {
                Content = "[更新设置]",
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                Foreground = textColorA,
                Margin = new Thickness(0, 10, 0, 5)
            });

            SimpleCheckBox chkDisableUpdate = new SimpleCheckBox
            {
                Content = "禁用插件自动更新检查",
                Style = checkBoxStyle,
                IsChecked = Mcl.Core.Updater.UpdateManager.CurrentConfig.DisableUpdate,
                Margin = new Thickness(15, 6, 0, 6)
            };
            mainPanel.Children.Add(chkDisableUpdate);

            SimpleCheckBox chkBuildChannel = new SimpleCheckBox
            {
                Content = "接收 Build 版 (开发版) 更新推送, 不勾选则是 Release (稳定版) 更新推送",
                Style = checkBoxStyle,
                IsChecked = Mcl.Core.Updater.UpdateManager.CurrentConfig.IsBuildChannel,
                Margin = new Thickness(15, 6, 0, 6)
            };
            mainPanel.Children.Add(chkBuildChannel);

            // 5. 【保存按钮】
            CustomButton saveBtn = new CustomButton
            {
                Content = "保存并应用配置",
                Width = 140,
                Height = 30,
                Style = buttonStyle,
                HorizontalAlignment = HorizontalAlignment.Left,
                Margin = new Thickness(0, 25, 0, 30) // 底部留点空隙
            };

            // 绑定保存逻辑
            saveBtn.Click += (s, e) =>
            {
                // 将 UI 数据回写到 WpfConfig
                foreach (var item in ConfigManager.Registry)
                {
                    if (!controlMap.TryGetValue(item.Key, out var ctrl)) continue;
                    var field = typeof(WpfConfig).GetField(item.Key, BindingFlags.Public | BindingFlags.Static);
                    if (field == null) continue;

                    try
                    {
                        if (ctrl is SimpleCheckBox cb)
                        {
                            field.SetValue(null, cb.IsChecked ?? false);
                        }
                        else if (ctrl is TextBox tb)
                        {
                            field.SetValue(null, Convert.ChangeType(tb.Text, item.FieldType));
                        }
                    }
                    catch
                    {
                        // 忽略类型转换异常(例如把字母填进int框里)
                    }
                }
                Mcl.Core.Updater.UpdateManager.CurrentConfig.DisableUpdate = chkDisableUpdate.IsChecked ?? false;
                Mcl.Core.Updater.UpdateManager.CurrentConfig.IsBuildChannel = chkBuildChannel.IsChecked ?? false;
                Mcl.Core.Updater.UpdateManager.SaveConfig();

                // 保存到 json 文件
                ConfigManager.Save();
                
                // 应用运行时设置 (调用你的 InitHook 方法)
                Mcl.Core.Dotnetdetour.Features.GeneralHooks.InitHook.ApplyRuntimeSettings();

                MessageBox.Show("MCL 扩展配置已保存并生效！", "设置成功", MessageBoxButton.OK, MessageBoxImage.Information);
            };
            mainPanel.Children.Add(saveBtn);

            // 6. 【注入】
            scrollViewer.Content = mainPanel;
            customTab.Content = scrollViewer;
            setTabControl.Items.Add(customTab);

            // 7. 【修复底部按钮遮挡问题】(拉长窗口)
            double extraHeightNeeded = 55.0;
            if (double.IsNaN(window.Height))
                window.Height = window.ActualHeight + extraHeightNeeded;
            else
                window.Height += extraHeightNeeded;
        }
    }
}