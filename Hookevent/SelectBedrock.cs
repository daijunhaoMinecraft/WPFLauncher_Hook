using DotNetTranstor;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Forms;
using WPFLauncher.Model;
using WPFLauncher.Model.Game;
using System.Windows.Media.Effects;
using WPFLauncher.Model.Game.GameClient;
using MessageBox = System.Windows.MessageBox;
using Application = System.Windows.Application;
using HorizontalAlignment = System.Windows.HorizontalAlignment;
using ListBox = System.Windows.Controls.ListBox;
using TextBox = System.Windows.Controls.TextBox;

namespace MicrosoftTranslator.DotNetTranstor.Hookevent
{
    public class BedrockPathWindow : Window
    {
        private TextBox _pathTextBox;
        private ListBox _versionListBox;
        private string _selectedPath;
        private string _configPath = Path.Combine(Directory.GetCurrentDirectory(), "BedrockConfig.txt");

        public BedrockPathWindow()
        {
            InitializeWindow();
            LoadSavedPath();
            ScanVersions();
        }

        private void InitializeWindow()
        {
            Title = "选择基岩版路径";
            Width = 600;
            Height = 500;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            Background = new SolidColorBrush(Colors.WhiteSmoke);
            
            var mainPanel = new StackPanel { Margin = new Thickness(20) };
            
            // 标题
            var titleBlock = new TextBlock
            {
                Text = "基岩版路径选择",
                FontSize = 24,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 20),
                Foreground = new SolidColorBrush(Color.FromRgb(51, 51, 51))
            };
            mainPanel.Children.Add(titleBlock);

            // 版本信息面板
            var versionPanel = new Grid { Margin = new Thickness(0, 0, 0, 20) };
            versionPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            versionPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var versionLabel = new TextBlock
            {
                Text = "当前基岩版版本:",
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 10, 0),
                Foreground = new SolidColorBrush(Color.FromRgb(51, 51, 51))
            };
            Grid.SetColumn(versionLabel, 0);

            var versionValue = new TextBlock
            {
                Text = "NONE",
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 10, 0),
                Foreground = new SolidColorBrush(Color.FromRgb(33, 150, 243)),
                FontWeight = FontWeights.Bold
            };
            Grid.SetColumn(versionValue, 1);

            versionPanel.Children.Add(versionLabel);
            versionPanel.Children.Add(versionValue);
            mainPanel.Children.Add(versionPanel);

            // 路径选择面板
            var pathPanel = new Grid { Margin = new Thickness(0, 0, 0, 20) };
            pathPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            pathPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            pathPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var pathLabel = new TextBlock
            {
                Text = "当前基岩版路径:",
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 10, 0),
                Foreground = new SolidColorBrush(Color.FromRgb(51, 51, 51))
            };
            Grid.SetColumn(pathLabel, 0);

            _pathTextBox = new TextBox
            {
                IsReadOnly = true,
                Height = 30,
                VerticalContentAlignment = VerticalAlignment.Center,
                Padding = new Thickness(5),
                Margin = new Thickness(0, 0, 10, 0),
                Background = new SolidColorBrush(Colors.White),
                BorderBrush = new SolidColorBrush(Color.FromRgb(200, 200, 200))
            };
            Grid.SetColumn(_pathTextBox, 1);

            var selectButton = new System.Windows.Controls.Button
            {
                Content = "修改路径",
                Height = 30,
                Padding = new Thickness(15, 0, 15, 0),
                Background = new SolidColorBrush(Color.FromRgb(33, 150, 243)),
                Foreground = new SolidColorBrush(Colors.White),
                BorderThickness = new Thickness(0),
                Effect = new DropShadowEffect
                {
                    ShadowDepth = 2,
                    BlurRadius = 4,
                    Opacity = 0.3
                }
            };
            selectButton.Click += SelectButton_Click;
            Grid.SetColumn(selectButton, 2);

            pathPanel.Children.Add(pathLabel);
            pathPanel.Children.Add(_pathTextBox);
            pathPanel.Children.Add(selectButton);
            mainPanel.Children.Add(pathPanel);

            // 版本列表
            var listLabel = new TextBlock
            {
                Text = "可用版本:",
                Margin = new Thickness(0, 0, 0, 10),
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(51, 51, 51))
            };
            mainPanel.Children.Add(listLabel);

            _versionListBox = new ListBox
            {
                Height = 300,
                Background = new SolidColorBrush(Colors.White),
                BorderBrush = new SolidColorBrush(Color.FromRgb(200, 200, 200))
            };
            _versionListBox.SelectionChanged += VersionListBox_SelectionChanged;
            mainPanel.Children.Add(_versionListBox);

            Content = mainPanel;
        }

        private void LoadSavedPath()
        {
            if (File.Exists(_configPath))
            {
                string savedPath = File.ReadAllText(_configPath);
                if (Directory.Exists(savedPath))
                {
                    _pathTextBox.Text = savedPath;
                    _selectedPath = savedPath;
                    ScanVersions();
                }
            }
            else
            {
                string swsPath = Path.Combine(Directory.GetCurrentDirectory(), "sw.s");
                if (File.Exists(swsPath))
                {
                    string path = File.ReadAllText(swsPath);
                    _pathTextBox.Text = path;
                    _selectedPath = path;
                }
            }
        }

        private void SelectButton_Click(object sender, RoutedEventArgs e)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.Description = "请选择基岩版安装路径";
                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    _selectedPath = dialog.SelectedPath;
                    _pathTextBox.Text = _selectedPath;
                    ScanVersions();
                }
            }
        }

        private void ScanVersions()
        {
            _versionListBox.Items.Clear();
            if (string.IsNullOrEmpty(_selectedPath) || !Directory.Exists(_selectedPath))
            {
                return;
            }

            try
            {
                foreach (var dir in Directory.GetDirectories(_selectedPath))
                {
                    if (File.Exists(Path.Combine(dir, "Minecraft.Windows.exe")))
                    {
                        var versionCard = CreateVersionCard(dir);
                        _versionListBox.Items.Add(versionCard);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"扫描版本时出错: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private Border CreateVersionCard(string path)
        {
            var card = new Border
            {
                Background = new SolidColorBrush(Colors.White),
                BorderBrush = new SolidColorBrush(Color.FromRgb(200, 200, 200)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(4),
                Margin = new Thickness(0, 0, 0, 5),
                Padding = new Thickness(10),
                Effect = new DropShadowEffect
                {
                    ShadowDepth = 1,
                    BlurRadius = 4,
                    Opacity = 0.2
                }
            };

            var panel = new StackPanel();
            
            var versionName = new TextBlock
            {
                Text = Path.GetFileName(path),
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 5),
                Foreground = new SolidColorBrush(Color.FromRgb(51, 51, 51))
            };
            panel.Children.Add(versionName);

            var pathText = new TextBlock
            {
                Text = path,
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromRgb(119, 119, 119))
            };
            panel.Children.Add(pathText);

            card.Child = panel;
            card.Tag = path;

            return card;
        }

        private void VersionListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_versionListBox.SelectedItem is Border selectedCard)
            {
                string selectedPath = selectedCard.Tag as string;
                if (!string.IsNullOrEmpty(selectedPath))
                {
                    DialogResult = true;
                    Close();
                }
            }
        }

        public ald GetSelectedVersion()
        {
            if (_versionListBox.SelectedItem is Border selectedCard)
            {
                string selectedPath = selectedCard.Tag as string;
                return new ald(new alo()
                {
                    PacklistField = "PcCppX64",
                    ExeDirName = Path.GetFileName(selectedPath),
                    Version = GameVersion.V_X64_CPP
                });
            }
            return null;
        }
    }

    public class SelectBedrock : IMethodHook
    {
        [OriginalMethod]
        public ald SelectBedrock_Original(GameVersion GameInfo)
        {
            return null;
        }

        [HookMethod("WPFLauncher.Manager.aph", "c", "SelectBedrock_Original")]
        public ald SelectBedrock_Hook(GameVersion GameInfo)
        {
            // BedrockPathWindow window = null;
            // ald result = null;
            //
            // Application.Current.Dispatcher.Invoke(() =>
            // {
            //     window = new BedrockPathWindow(GameInfo);
            //     if (window.ShowDialog() == true)
            //     {
            //         result = window.GetSelectedVersion();
            //     }
            // });

            return SelectBedrock_Original(GameInfo);
        }
    }
}