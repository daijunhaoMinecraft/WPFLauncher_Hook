using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Mcl.Core.Dotnetdetour.Features.NetworkAndRoom;
using Mcl.Core.Dotnetdetour.Models.Config;
using Mcl.Core.Dotnetdetour.Models.Entity;
using Mcl.Core.Dotnetdetour.UI.Dialogs;
using Mcl.Core.NeteaseProtocol;
using Newtonsoft.Json;

namespace Mcl.Core.Dotnetdetour.UI.Windows
{
    public partial class SaveManagerWindow : Window
    {
        // 使用 ObservableCollection 自动更新 UI 列表
        public ObservableCollection<SaveItemViewModel> SaveItems { get; set; } = new ObservableCollection<SaveItemViewModel>();
        
        public static readonly DependencyProperty StorageUsageTextProperty =
            DependencyProperty.Register("StorageUsageText", typeof(string), typeof(SaveManagerWindow), new PropertyMetadata("计算中..."));

        public string StorageUsageText
        {
            get { return (string)GetValue(StorageUsageTextProperty); }
            set { SetValue(StorageUsageTextProperty, value); }
        }

        public static readonly DependencyProperty StorageUsagePercentageProperty =
            DependencyProperty.Register("StorageUsagePercentage", typeof(double), typeof(SaveManagerWindow), new PropertyMetadata(0.0));

        public double StorageUsagePercentage
        {
            get { return (double)GetValue(StorageUsagePercentageProperty); }
            set { SetValue(StorageUsagePercentageProperty, value); }
        }
        
        public SaveItemViewModel SelectedSaveData { get; private set; }

        public SaveManagerWindow()
        {
            InitializeComponent();
            SaveListView.ItemsSource = SaveItems;
        }

        // 窗口加载时获取列表
        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            ChatConnectionPacket.BackupEventManager.OnBackupCompleted += OnBackupCompletedHandler;

            await LoadSavesAsync();
        }
        
        private void Window_Closed(object sender, EventArgs e)
        {
            ChatConnectionPacket.BackupEventManager.OnBackupCompleted -= OnBackupCompletedHandler;
        }
        
        private void OnBackupCompletedHandler(int backupId, bool isSuccess)
        {
            // 确保切回 UI 线程执行
            Dispatcher.InvokeAsync(async () =>
            {
                // 如果收到成功的消息，说明云端存档发生了变化，我们静默刷新列表即可
                if (isSuccess)
                {
                    // 可以选择在这里加一个小日志
                    WpfConfig.DefaultLogger.Info($"[载入窗口] 检测到槽位 {backupId} 备份成功，正在自动刷新列表...");
                    
                    await LoadSavesAsync();
                }
            });
        }

        // 1. 获取并载入存档列表
        private async Task LoadSavesAsync()
        {
            try
            {
                SaveItems.Clear(); // 清空旧数据
                // 模拟 UI 处于加载状态(可以通过增加一个 Loading 圈来实现，这里简略)
                
                string responseStr = await Task.Run(() => 
                    X19Http.Get("/online-lobby-backup/query/list-by-user")
                );

                var response = JsonConvert.DeserializeObject<BaseResponse<SaveEntity>>(responseStr);

                long totalSizeKb = 0; // 记录总大小(KB)
                
                if (response != null && response.Code == 0 && response.Entities != null)
                {
                    foreach (var entity in response.Entities)
                    {
                        SaveItems.Add(new SaveItemViewModel(entity));
                        totalSizeKb += entity.Size;
                    }
                }
                else
                {
                    MessageBox.Show($"获取存档失败: {response?.Message ?? "未知错误"}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                double totalMb = totalSizeKb / 1024.0;
                double maxMb = 200.0;
        
                StorageUsageText = $"{totalMb:F2} MB / {maxMb} MB";
                    
                double percentage = (totalMb / maxMb) * 100;
                StorageUsagePercentage = Math.Min(100, Math.Max(0, percentage));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"网络请求异常: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private async void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn)
            {
                btn.IsEnabled = false; // 暂时禁用按钮，防止狂点狂刷
                btn.Content = "刷新中...";
        
                await LoadSavesAsync();
        
                btn.Content = "刷新列表";
                btn.IsEnabled = true;  // 恢复按钮
            }
        }

        // 2. 载入选中存档的动作
        private void BtnLoad_Click(object sender, RoutedEventArgs e)
        {
            if (SaveListView.SelectedItem is SaveItemViewModel selectedSave)
            {
                this.SelectedSaveData = selectedSave;
                this.DialogResult = true;
            }
            else
            {
                MessageBox.Show("请先选择一个槽位！", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        // 3. 重命名存档
        private async void BtnRename_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int backupId)
            {
                var currentSave = SaveItems.FirstOrDefault(x => x.BackupId == backupId);
                string oldName = currentSave != null ? currentSave.Name : "";

                var inputDialog = new InputDialogWindow("重命名存档", oldName) { Owner = this };

                if (inputDialog.ShowDialog() == true)
                {
                    string newName = inputDialog.InputText;
                    if (newName == oldName) return;

                    var payload = new { name = newName, backup_id = backupId.ToString() };
                    string jsonBody = JsonConvert.SerializeObject(payload);

                    try
                    {
                        string resStr = await Task.Run(() => X19Http.Post("/online-lobby-backup/update", jsonBody));
                        var response = JsonConvert.DeserializeObject<BaseResponse<SaveEntity>>(resStr);

                        if (response != null && response.Code == 0)
                        {
                            await LoadSavesAsync(); 
                        }
                        else
                        {
                            MessageBox.Show($"重命名失败: {response?.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                    catch (Exception ex) { MessageBox.Show($"请求异常: {ex.Message}"); }
                }
            }
        }

        // 4. 删除存档
        private async void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int backupId)
            {
                var result = MessageBox.Show($"确定要删除槽位 {backupId} 的存档吗？此操作不可逆！", "警告", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result != MessageBoxResult.Yes) return;

                var payload = new { backup_id = backupId.ToString() };
                string jsonBody = JsonConvert.SerializeObject(payload);

                try
                {
                    string resStr = await Task.Run(() => 
                        X19Http.Post("/online-lobby-backup/delete", jsonBody)
                    );

                    var response = JsonConvert.DeserializeObject<BaseResponse<SaveEntity>>(resStr);

                    if (response != null && response.Code == 0)
                    {
                        MessageBox.Show("删除成功！", "成功");
                        await Task.Delay(800); 

                        await LoadSavesAsync(); // 重新加载列表
                    }
                    else
                    {
                        MessageBox.Show($"删除失败: {response?.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"请求异常: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // 支持拖动无边框窗口
        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }

        // 关闭窗口
        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}