using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Forms;
using System.Drawing;
using System.Linq;
using System.Text;
using Mcl.Core.DotNetTranstor.Tools.Network;
using Mcl.Core.DotNetTranstor.Var;
using Mcl.Core.DotNetTranstor.Model;

namespace Mcl.Core.DotNetTranstor.Window
{
    public class NetworkMonitorForm : Form
    {
        private DataGridView _dataGridView;
        private Button _btnStop;
        private Button _btnRefresh;
        private Label _lblStatus;
        private ContextMenuStrip _rowContextMenu;
        private System.Windows.Forms.Timer _refreshTimer;
        
        // [新增] 声明 ToolTip 组件
        private ToolTip _toolTip; 

        public NetworkMonitorForm()
        {
            InitializeComponent();
            SetupTimer();
            RefreshPlayerData(); 
        }
        
        private void InitializeComponent()
        {
            // [新增] 初始化 ToolTip 组件
            _toolTip = new ToolTip();
            _toolTip.InitialDelay = 500; // 鼠标悬停多久后显示 (毫秒)
            _toolTip.ReshowDelay = 100;  // 从一个控件移到另一个控件的显示延迟
            _toolTip.AutoPopDelay = 5000; // 显示持续时间

            this.Text = "异地组网状态查看器 - Mcl Network Monitor";
            this.Size = new Size(900, 600);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.White;
            this.MinimumSize = new Size(650, 450);

            // --- 1. 顶部状态栏 ---
            _lblStatus = new Label
            {
                Text = "正在初始化...",
                Dock = DockStyle.Top,
                Height = 50,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(20, 0, 0, 0),
                Font = new Font("Microsoft YaHei", 11, FontStyle.Bold),
                ForeColor = Color.FromArgb(32, 32, 32),
                BackColor = Color.FromArgb(245, 245, 245)
            };

            // --- 2. 数据表格 ---
            _dataGridView = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                AllowUserToAddRows = false,
                ReadOnly = true,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle { BackColor = Color.FromArgb(250, 250, 250) },
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle 
                { 
                    BackColor = Color.FromArgb(240, 240, 240), 
                    Font = new Font("Microsoft YaHei", 9, FontStyle.Bold),
                    ForeColor = Color.FromArgb(64, 64, 64)
                },
                AllowUserToResizeRows = false,
                StandardTab = true
            };

            _dataGridView.Columns.Add("Name", "玩家名称");
            _dataGridView.Columns.Add("UserID", "User ID");
            _dataGridView.Columns.Add("PeerId", "WebRTC PeerId");
            _dataGridView.Columns.Add("VirtualIp", "内网 IP 地址");
            _dataGridView.Columns.Add("Status", "连接状态");

            _dataGridView.Columns["Name"].Width = 150;
            _dataGridView.Columns["VirtualIp"].Width = 130;
            _dataGridView.Columns["Status"].Width = 100;

            // --- 3. 右键菜单配置 ---
            _rowContextMenu = new ContextMenuStrip();
            
            var miCopyIp = new ToolStripMenuItem("复制内网 IP", null, (s, e) => CopySelectedIp());
            // 简单图标处理，如果 SystemIcons.Application 报错可去掉 Image 赋值
            try { miCopyIp.Image = SystemIcons.Application.ToBitmap(); } catch { }
            
            var miCopyInfo = new ToolStripMenuItem("复制玩家详细信息", null, (s, e) => CopySelectedInfo());
            miCopyInfo.Font = new Font(miCopyInfo.Font, FontStyle.Bold);

            var miCopyPeerId = new ToolStripMenuItem("复制 PeerID", null, (s, e) => CopySelectedPeerId());

            var sep = new ToolStripSeparator();
            
            var miRefresh = new ToolStripMenuItem("刷新列表", null, (s, e) => RefreshPlayerData());
            miRefresh.ShortcutKeys = Keys.F5;

            _rowContextMenu.Items.AddRange(new ToolStripItem[] { 
                miCopyIp, miCopyPeerId, miCopyInfo, sep, miRefresh 
            });

            _rowContextMenu.Opening += (s, e) => {
                if (_dataGridView.SelectedRows.Count == 0)
                {
                    e.Cancel = true;
                    return;
                }
                bool hasValidIp = !string.IsNullOrEmpty(GetSelectedPlayer()?.VirtualIp);
                miCopyIp.Enabled = hasValidIp;
            };

            _dataGridView.ContextMenuStrip = _rowContextMenu;

            // --- 4. 底部控制栏 ---
            Panel bottomPanel = new Panel 
            { 
                Dock = DockStyle.Bottom, 
                Height = 70, 
                BackColor = Color.FromArgb(245, 245, 245),
                Padding = new Padding(20)
            };
            
            // [修改] 刷新按钮 - 移除 ToolTipText 属性
            _btnRefresh = new Button
            {
                Text = "🔄 刷新列表",
                Size = new Size(120, 45),
                Location = new Point(20, 12),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Microsoft YaHei", 10, FontStyle.Bold),
                Cursor = Cursors.Hand
                // ToolTipText = "手动刷新玩家列表 (F5)" // <--- 删除这行
            };
            _btnRefresh.FlatAppearance.BorderSize = 0;
            _btnRefresh.Click += (s, e) => RefreshPlayerData();
            
            // [新增] 使用 ToolTip 组件设置提示
            _toolTip.SetToolTip(_btnRefresh, "手动刷新玩家列表 (F5)");

            // 停止按钮
            _btnStop = new Button
            {
                Text = "⏹ 停止服务",
                Size = new Size(140, 45),
                BackColor = Color.FromArgb(209, 52, 56),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Microsoft YaHei", 10, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            _btnStop.FlatAppearance.BorderSize = 0;
            _btnStop.Click += BtnStop_Click;
            
            // 也可以给停止按钮加个提示
            _toolTip.SetToolTip(_btnStop, "停止组网服务并断开所有连接");

            // 布局管理
            _btnRefresh.Anchor = AnchorStyles.Left | AnchorStyles.Bottom;
            _btnRefresh.Location = new Point(20, 12);

            _btnStop.Anchor = AnchorStyles.Right | AnchorStyles.Bottom;
            _btnStop.Location = new Point(bottomPanel.ClientSize.Width - _btnStop.Width - 20, 12);

            bottomPanel.Controls.Add(_btnRefresh);
            bottomPanel.Controls.Add(_btnStop);

            this.Resize += (s, e) => {
                _btnStop.Location = new Point(bottomPanel.ClientSize.Width - _btnStop.Width - 20, 12);
            };

            this.Controls.Add(_dataGridView);
            this.Controls.Add(_lblStatus);
            this.Controls.Add(bottomPanel);
        }

        // --- 辅助方法：获取当前选中的玩家对象 ---
        private LanGamePlayerInfo GetSelectedPlayer()
        {
            if (_dataGridView.SelectedRows.Count > 0)
            {
                int index = _dataGridView.SelectedRows[0].Index;
                if (index >= 0 && index < WebRtcVar.PlayerList.Count)
                {
                    return WebRtcVar.PlayerList[index];
                }
            }
            return null;
        }

        // --- 复制功能实现 ---
        private void CopySelectedIp()
        {
            var player = GetSelectedPlayer();
            if (player != null && !string.IsNullOrEmpty(player.VirtualIp))
            {
                Clipboard.SetText(player.VirtualIp);
                ShowTooltip($"已复制 IP: {player.VirtualIp}");
            }
        }

        private void CopySelectedPeerId()
        {
            var player = GetSelectedPlayer();
            if (player != null)
            {
                Clipboard.SetText(player.PeerId.ToString());
                ShowTooltip($"已复制 PeerID");
            }
        }

        private void CopySelectedInfo()
        {
            var player = GetSelectedPlayer();
            if (player != null)
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine($"玩家名称：{player.Name}");
                sb.AppendLine($"User ID:   {player.UserID}");
                sb.AppendLine($"Peer ID:   {player.PeerId}");
                sb.AppendLine($"内网 IP:    {player.VirtualIp}");
                sb.AppendLine($"状  态：    {player.Status}");
                
                string info = sb.ToString();
                Clipboard.SetText(info);
                ShowTooltip("已复制玩家详细信息");
            }
        }

        private void ShowTooltip(string message)
        {
            var originalText = _lblStatus.Text;
            var originalColor = _lblStatus.ForeColor;
            
            _lblStatus.Text = $"✅ {message}";
            _lblStatus.ForeColor = Color.Green;
            
            var timer = new System.Windows.Forms.Timer();
            timer.Interval = 2000;
            timer.Tick += (s, e) => {
                _lblStatus.Text = originalText;
                _lblStatus.ForeColor = originalColor;
                timer.Stop();
                timer.Dispose();
            };
            timer.Start();
        }

        private void SetupTimer()
        {
            // _refreshTimer = new System.Windows.Forms.Timer();
            // _refreshTimer.Interval = 2000; 
            // _refreshTimer.Tick += (s, e) => RefreshPlayerData();
            // _refreshTimer.Start();
        }
        public void RefreshPlayerData()
        {
            // 1. 跨线程检查：如果当前不是 UI 线程，则转发给 UI 线程执行
            if (this.InvokeRequired)
            {
                // 使用 BeginInvoke 异步执行，避免后台线程被 UI 线程卡住（防止死锁）
                this.BeginInvoke(new Action(RefreshPlayerData));
                return;
            }

            // 2. 状态检查：如果窗口正在关闭或已释放，直接返回
            if (this.IsDisposed || this.Disposing) return;

            try
            {
                // 3. 数据快照：在锁保护下提取数据（假设 PlayerList 可能在后台被修改）
                // 如果 PlayerList 是 List<T>，多线程下直接 ToArray 仍有极小概率报错，
                // 但在这里获取快照是正确的方向。
                var players = WebRtcVar.PlayerList.ToArray();
                string myPeerId = WebRtcVar.MyPeerId;
                string localIp = WintunRouterService.Instance?.LocalVirtualIp ?? "未分配";

                // 更新状态标签
                _lblStatus.Text = $"本地状态：● 运行中 | 本地虚拟 IP: {localIp} | 在线节点数：{players.Length}";
                _lblStatus.ForeColor = Color.FromArgb(0, 120, 215);

                // 4. UI 更新锁定：减少闪烁并停止重绘逻辑
                _dataGridView.SuspendLayout();
                
                // 清除旧数据前彻底停止编辑模式，防止单元格状态冲突
                _dataGridView.CancelEdit();
                _dataGridView.Rows.Clear();

                foreach (var p in players)
                {
                    if (p == null) continue;

                    string statusText = p.Status ?? "在线";
                    bool isMe = (p.PeerId.ToString() == myPeerId);
                    if (isMe) statusText = "本机 (我)";

                    // 添加行
                    int rowIndex = _dataGridView.Rows.Add(
                        p.Name ?? "未知",
                        p.UserID ?? "-",
                        p.PeerId.ToString(),
                        p.VirtualIp ?? "-",
                        statusText
                    );

                    // 5. 样式设置
                    var currentRow = _dataGridView.Rows[rowIndex];
                    if (isMe)
                    {
                        currentRow.DefaultCellStyle.BackColor = Color.FromArgb(230, 240, 255);
                        // 建议将 Font 对象预先定义为静态常量，避免频繁创建销毁
                        currentRow.DefaultCellStyle.Font = new Font("Microsoft YaHei", 9, FontStyle.Bold);
                    }
                    else
                    {
                        currentRow.DefaultCellStyle.BackColor = Color.White; // 显式设置背景色
                        currentRow.DefaultCellStyle.Font = new Font("Microsoft YaHei", 9, FontStyle.Regular);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[刷新玩家列表异常] {ex.Message}");
            }
            finally
            {
                // 6. 强制恢复布局：无论是否发生异常，都必须恢复布局，否则 DataGridView 会变白板
                if (!_dataGridView.IsDisposed)
                {
                    _dataGridView.ResumeLayout(true);
                }
            }
        }
        private void BtnStop_Click(object sender, EventArgs e)
        {
            var result = MessageBox.Show(
                "确定要停止组网服务并关闭虚拟网卡吗？\n\n所有连接将会中断。", 
                "确认停止", 
                MessageBoxButtons.YesNo, 
                MessageBoxIcon.Warning
            );

            if (result == DialogResult.Yes)
            {
                _refreshTimer?.Stop();
                try 
                {
                    WebRtcVar.StopForwarder(); 
                    WintunRouterService.Instance?.Stop();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"停止服务时发生错误:\n{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                this.Close();
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _refreshTimer?.Stop();
            base.OnFormClosing(e);
        }
    }
}