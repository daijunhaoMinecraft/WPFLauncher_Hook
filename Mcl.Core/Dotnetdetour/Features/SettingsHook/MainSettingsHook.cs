using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using Mcl.Core.Dotnetdetour.CoreEngine.Attributes;
using Mcl.Core.Dotnetdetour.CoreEngine.Interfaces;
using WPFLauncher.View.SysSetting;

// 确保引用了目标程序的集

namespace Mcl.Core.Dotnetdetour.Features.SettingsHook
{
    public class SysSettingMainPageMonitor : IMethodHook
    {
        // 目标方法签名必须与原方法完全一致
        [HookMethod("WPFLauncher.View.SysSetting", "SysSettingMainPage")]
        protected void OnContentRendered(object instance, EventArgs e)
        {
            // 1. 先调用原程序的 OnContentRendered 方法，保证程序原有逻辑正常运行
            Original(e);

            SysSettingMainPage mainWindow = instance as SysSettingMainPage;
            if (mainWindow == null) return;

            // 3. 执行添加自定义控件的逻辑
            AddCustomControl(mainWindow);
        }

        // 定义 Original 方法用于调用原函数
        [MethodImpl(MethodImplOptions.NoInlining)]
        [Original]
        protected void Original(EventArgs e)
        {
            // DotNetDetour 会在此处替换原始方法的调用
            return;
        }

        // 添加控件的具体逻辑
        private void AddCustomControl(SysSettingMainPage window)
        {
            try
            {
                // 因为目标程序的 SetTabControl 是 internal 字段，我们可以直接访问或通过反射获取
                FieldInfo tabControlField = typeof(SysSettingMainPage).GetField("SetTabControl", 
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                
                if (tabControlField != null)
                {
                    TabControl setTabControl = tabControlField.GetValue(window) as TabControl;

                    if (setTabControl != null)
                    {
                        // 方案 A：在 TabControl 中新增一个选项卡 (TabItem)
                        TabItem customTab = new TabItem();
                        customTab.Header = "我的自定义设置";

                        // 在新选项卡里放一个按钮
                        Button myBtn = new Button();
                        myBtn.Content = "点击我执行自定义功能";
                        myBtn.Width = 150;
                        myBtn.Height = 35;
                        myBtn.Click += (s, e) => {
                            MessageBox.Show("自定义按钮被点击了！");
                        };

                        customTab.Content = myBtn;
                        
                        // 将新选项卡加入到现有的 TabControl 中
                        setTabControl.Items.Add(customTab);
                    }
                }
            }
            catch (Exception ex)
            {
                // 记录日志，防止 Hook 抛出异常导致主程序崩溃
                Console.WriteLine("添加控件失败: " + ex.Message);
            }
        }
    }
}