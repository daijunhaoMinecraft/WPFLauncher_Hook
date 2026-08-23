using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Mcl.Core.Dotnetdetour.Features.NetworkAndRoom;
using Mcl.Core.Dotnetdetour.Models.Entity;
using Mcl.Core.Dotnetdetour.UI.Dialogs;
using Mcl.Core.NeteaseProtocol;
using Newtonsoft.Json;

namespace Mcl.Core.Dotnetdetour.UI.Windows
{
    public partial class SaveBackupWindow : Window
    {
        public ObservableCollection<SaveItemViewModel> SaveItems { get; set; } = new ObservableCollection<SaveItemViewModel>();
        
        public static readonly DependencyProperty StorageUsageTextProperty =
            DependencyProperty.Register("StorageUsageText", typeof(string), typeof(SaveBackupWindow), new PropertyMetadata("计算中..."));

        public string StorageUsageText
        {
            get { return (string)GetValue(StorageUsageTextProperty); }
            set { SetValue(StorageUsageTextProperty, value); }
        }

        public static readonly DependencyProperty StorageUsagePercentageProperty =
            DependencyProperty.Register("StorageUsagePercentage", typeof(double), typeof(SaveBackupWindow), new PropertyMetadata(0.0));

        public double StorageUsagePercentage
        {
            get { return (double)GetValue(StorageUsagePercentageProperty); }
            set { SetValue(StorageUsagePercentageProperty, value); }
        }

        // 记录正在等待备份回调的槽位（防止重复点击）
        private int _pendingBackupId = -1;
        
        private LoadingDialogWindow _loadingDialog;

        public SaveBackupWindow()
        {
            InitializeComponent();
            SaveListView.ItemsSource = SaveItems;
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // 订阅全局的备份完成事件
            ChatConnectionPacket.BackupEventManager.OnBackupCompleted += OnBackupCompletedHandler;
            await LoadSavesAsync();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            // 务必取消订阅，防止内存泄漏
            ChatConnectionPacket.BackupEventManager.OnBackupCompleted -= OnBackupCompletedHandler;
        }

        // ================= 数据加载 (与载入窗口相同) =================
        private async Task LoadSavesAsync()
        {
            try
            {
                SaveItems.Clear();
                string responseStr = await Task.Run(() => X19Http.Get("/online-lobby-backup/query/list-by-user"));
                var response = JsonConvert.DeserializeObject<BaseResponse<SaveEntity>>(responseStr);

                long totalSizeKb = 0;
                
                if (response != null && response.Code == 0 && response.Entities != null)
                {
                    foreach (var entity in response.Entities)
                    {
                        SaveItems.Add(new SaveItemViewModel(entity));
                        totalSizeKb += entity.Size;
                    }
                }
                double totalMb = totalSizeKb / 1024.0;
                double maxMb = 200.0;
        
                StorageUsageText = $"{totalMb:F2} MB / {maxMb} MB";
        
                double percentage = (totalMb / maxMb) * 100;
                StorageUsagePercentage = Math.Min(100, Math.Max(0, percentage));
            }
            catch (Exception ex) { MessageBox.Show($"获取失败: {ex.Message}"); }
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

        // ================= 核心：发起备份请求逻辑 =================
        private async Task StartBackupAsync(int targetBackupId, string saveName)
        {
            if (_pendingBackupId != -1)
            {
                MessageBox.Show("当前已有备份任务正在进行中，请稍候...", "提示");
                return;
            }

            var payload = new { name = saveName, backup_id = targetBackupId.ToString() };
            string jsonBody = JsonConvert.SerializeObject(payload);

            try
            {
                _pendingBackupId = targetBackupId;
            
                string resStr = await Task.Run(() => X19Http.Post("/online-lobby-backup/create", jsonBody));
                var response = JsonConvert.DeserializeObject<BaseResponse<SaveEntity>>(resStr);

                if (response == null || response.Code != 0)
                {
                    _pendingBackupId = -1;
                    if (response?.Code == 12006)
                        MessageBox.Show("游戏非运行中，不能备份存档！", "无法备份", MessageBoxButton.OK, MessageBoxImage.Warning);
                    else
                        MessageBox.Show($"备份请求失败: {response?.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                else
                {
                    // 🌟 请求发送成功，弹出等待动画框 (阻塞UI线程，等待回调)
                    _loadingDialog = new LoadingDialogWindow { Owner = this };
                    _loadingDialog.ShowDialog();
                
                    // ShowDialog 会在这里阻塞，直到 _loadingDialog.Close() 被调用
                }
            }
            catch (Exception ex)
            {
                _pendingBackupId = -1;
                MessageBox.Show($"请求异常: {ex.Message}");
            }
        }

        // ================= 事件：回调处理 =================
        private void OnBackupCompletedHandler(int backupId, bool isSuccess)
        {
            Dispatcher.InvokeAsync(async () =>
            {
                if (_pendingBackupId == backupId)
                {
                    _pendingBackupId = -1; // 解除锁定

                    // 🌟 收到网络响应，自动关闭等待动画框
                    if (_loadingDialog != null)
                    {
                        _loadingDialog.Close();
                        _loadingDialog = null;
                    }

                    // 提示结果并刷新列表
                    if (isSuccess)
                    {
                        MessageBox.Show($"槽位 {backupId} 备份成功！", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                        await LoadSavesAsync(); 
                    }
                    else
                    {
                        MessageBox.Show($"槽位 {backupId} 备份失败，请重试。", "失败", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            });
        }

        // ================= UI 操作 =================
        
        // 1. 新建备份
        private async void BtnNewBackup_Click(object sender, RoutedEventArgs e)
        {
            // 提供一个默认的名字 (比如当前时间)
            string defaultName = DateTime.Now.ToString("yyyyMMdd_HHmm");
            var inputDialog = new InputDialogWindow("新建备份存档", defaultName) { Owner = this };

            if (inputDialog.ShowDialog() == true)
            {
                int nextId = SaveItems.Count > 0 ? SaveItems.Max(x => x.BackupId) + 1 : 1;
                await StartBackupAsync(nextId, inputDialog.InputText);
            }
        }

        // 2. 覆盖当前槽位
        private async void BtnOverwrite_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int backupId)
            {
                var currentSave = SaveItems.FirstOrDefault(x => x.BackupId == backupId);
                string defaultName = currentSave != null ? currentSave.Name : "";

                // 先弹出系统提示框警告，确认后再弹出命名框
                var result = MessageBox.Show($"确定要覆盖槽位 {backupId} 吗？旧的存档将被永久替换！", "覆盖确认", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.Yes)
                {
                    var inputDialog = new InputDialogWindow($"覆盖槽位 {backupId}", defaultName) { Owner = this };
                    
                    if (inputDialog.ShowDialog() == true)
                    {
                        await StartBackupAsync(backupId, inputDialog.InputText);
                    }
                }
            }
        }

        // 3. 重命名 (代码与 SaveManagerWindow 里的 BtnRename_Click 完全一样，可以直接复制过来)
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
                        await LoadSavesAsync(); // 重新加载列表
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"请求异常: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        private void BtnClose_Click(object sender, RoutedEventArgs e) { this.Close(); }
        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) { if (e.LeftButton == MouseButtonState.Pressed) DragMove(); }
    }
}