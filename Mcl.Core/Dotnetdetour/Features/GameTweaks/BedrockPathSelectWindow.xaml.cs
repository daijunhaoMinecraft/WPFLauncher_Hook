using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using Mcl.Core.Dotnetdetour.Models.Config;
using WPFLauncher.Util;
// 区分 WPF 的 MessageBox 和 WinForms 的 FolderBrowserDialog
using FormsDialogResult = System.Windows.Forms.DialogResult;
using FolderBrowserDialog = System.Windows.Forms.FolderBrowserDialog;

namespace Mcl.Core.Dotnetdetour.Features.GeneralHooks
{
    // 用于绑定的数据模型
    public class BedrockVersionItem
    {
        public string Name { get; set; }
        public string Path { get; set; }
    }

    public partial class BedrockPathSelectWindow : Window
    {
        private readonly Action<string> _onVersionSelected;
        private string _selectedPath;

        public BedrockPathSelectWindow(Action<string> onVersionSelected)
        {
            InitializeComponent();
            _onVersionSelected = onVersionSelected;
            
            // 初始化置顶状态
            TopMostCheck.IsChecked = WpfConfig.IsWindowTopMost;
            Topmost = WpfConfig.IsWindowTopMost;

            LoadSavedPath();
        }

        private void OnTopMostChanged(object sender, RoutedEventArgs e)
        {
            Topmost = TopMostCheck.IsChecked == true;
        }

        private void LoadSavedPath()
        {
            // tb.s 应该是你代码里的某个全局静态路径
            if (Directory.Exists(tb.s))
            {
                _selectedPath = tb.s;
            }
            else if (Directory.Exists(WpfConfig.BedrockPath))
            {
                _selectedPath = WpfConfig.BedrockPath;
            }

            if (!string.IsNullOrEmpty(_selectedPath))
            {
                PathTextBox.Text = _selectedPath;
                ScanVersions();
            }
        }

        private void SelectButton_Click(object sender, RoutedEventArgs e)
        {
            using (var dialog = new FolderBrowserDialog { Description = "请选择基岩版安装路径" })
            {
                if (dialog.ShowDialog() == FormsDialogResult.OK)
                {
                    _selectedPath = dialog.SelectedPath;
                    PathTextBox.Text = _selectedPath;

                    try
                    {
                        WpfConfig.BedrockPath = _selectedPath;
                        ConfigManager.Save();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"保存路径时出错: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    
                    ScanVersions();
                }
            }
        }

        private void ScanVersions()
        {
            var items = new List<BedrockVersionItem>();
            if (string.IsNullOrEmpty(_selectedPath) || !Directory.Exists(_selectedPath)) return;

            try
            {
                foreach (var dir in Directory.GetDirectories(_selectedPath))
                {
                    if (File.Exists(Path.Combine(dir, "Minecraft.Windows.exe")))
                    {
                        items.Add(new BedrockVersionItem 
                        { 
                            Name = Path.GetFileName(dir), 
                            Path = dir 
                        });
                    }
                }
                
                // 将数据源绑定到 UI 列表
                VersionListBox.ItemsSource = items;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"扫描版本时出错: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void VersionListBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (VersionListBox.SelectedItem is BedrockVersionItem selectedItem)
            {
                _onVersionSelected?.Invoke(selectedItem.Path);
                DialogResult = true;
                // Close(); // 设置 DialogResult = true 会自动关闭窗口，不需要手动 Close()
            }
        }
    }
}