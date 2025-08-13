using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Newtonsoft.Json.Linq;
using WPFLauncher.Network.Protocol.LobbyGame;
using MicrosoftTranslator.DotNetTranstor.Tools;
using WPFLauncher.Code;

namespace DotNetTranstor.Hookevent
{
    public class RoomInfoWindow : Window
    {
        private HashSet<string> _existingCookies;
        private ListBox _playersList;
        private Dictionary<string, TextBlock> _infoBlocks;
        private CheckBox _enableNerk;
        private ListBox _logList;
        private WrapPanel _playersPanel;
        private ScrollViewer _playersScrollViewer;
        private TextBox _switchCookieInput;

        public RoomInfoWindow(EntityResponse<LobbyGameRoomEntity> roomInfo)
        {
            InitializeWindow();
            RoomManage_Left.SetRoomInfoWindow(this);
            UpdateRoomInfo(roomInfo);
        }
        
        private void InitializeWindow()
        {
            Title = "房间信息";
            Width = 600;
            Height = 500;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            Background = new SolidColorBrush(Colors.WhiteSmoke);
            
            var mainPanel = new StackPanel { Margin = new Thickness(20) };
            
            var scrollViewer = new ScrollViewer
            {
                Content = mainPanel,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };
            
            Content = scrollViewer;

            var titleBlock = new TextBlock
            {
                Text = "房间信息面板",
                FontSize = 24,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 20)
            };
            mainPanel.Children.Add(titleBlock);

            var infoPanel = CreateInfoPanel();
            mainPanel.Children.Add(infoPanel);
            
            var logPanel = CreateLogPanel();
            mainPanel.Children.Add(logPanel);
        }

        private GroupBox CreateInfoPanel()
        {
            var infoPanel = new GroupBox
            {
                Header = "房间基本信息",
                Margin = new Thickness(0, 0, 0, 10)
            };

            var grid = new Grid { Margin = new Thickness(10) };
            
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            for (int i = 0; i < 4; i++)
            {
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            }

            _infoBlocks = new Dictionary<string, TextBlock>();
            var infoItems = new[]
            {
                ("房间号:", "RoomNumber", 0, 0),
                ("房主:", "Owner", 0, 2),
                ("当前人数:", "CurrentPlayers", 1, 0),
                ("最大人数:", "MaxPlayers", 1, 2),
                ("密码保护:", "Password", 2, 0),
                ("允许保存:", "AllowSave", 2, 2),
                ("游戏状态:", "GameStatus", 3, 0),
                ("版本号:", "Version", 3, 2)
            };

            foreach (var (label, key, row, col) in infoItems)
            {
                var labelBlock = new TextBlock
                {
                    Text = label,
                    Margin = new Thickness(5),
                    VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetRow(labelBlock, row);
                Grid.SetColumn(labelBlock, col);
                grid.Children.Add(labelBlock);

                var valueBlock = new TextBlock
                {
                    Margin = new Thickness(5),
                    VerticalAlignment = VerticalAlignment.Center,
                    FontWeight = FontWeights.Bold
                };
                Grid.SetRow(valueBlock, row);
                Grid.SetColumn(valueBlock, col + 1);
                grid.Children.Add(valueBlock);
                _infoBlocks[key] = valueBlock;
            }

            infoPanel.Content = grid;
            return infoPanel;
        }

        private Button CreateStyledButton(string content, RoutedEventHandler clickHandler)
        {
            var button = new Button
            {
                Content = content,
                Margin = new Thickness(5),
                Padding = new Thickness(15, 5, 15, 5),
                Height = 30,
                Background = new SolidColorBrush(Color.FromRgb(33, 150, 243)),
                Foreground = new SolidColorBrush(Colors.White),
                BorderThickness = new Thickness(0)
            };

            button.Click += clickHandler;
            return button;
        }

        private void UpdateRoomInfo(EntityResponse<LobbyGameRoomEntity> roomInfo)
        {
            if (roomInfo?.entity == null) return;

            var entity = roomInfo.entity;
            
            JObject ownerInfo = X19Http.Get_Player_Info(entity.owner_id);
            
            _infoBlocks["RoomNumber"].Text = entity.room_name;
            _infoBlocks["Owner"].Text = $"{ownerInfo["entity"]["name"]} ({entity.owner_id})";
            _infoBlocks["CurrentPlayers"].Text = entity.cur_num.ToString();
            _infoBlocks["MaxPlayers"].Text = entity.max_count.ToString();
            _infoBlocks["Password"].Text = entity.password ? "是" : "否";
            _infoBlocks["AllowSave"].Text = entity.allow_save ? "是" : "否";
            _infoBlocks["GameStatus"].Text = entity.game_status == 1 ? "在线" : "离线";
            _infoBlocks["Version"].Text = entity.version;

            UpdatePlayersList(entity);
        }

        private async void UpdatePlayersList(LobbyGameRoomEntity entity)
        {
            if (entity?.fids == null) return;
            await Task.Run(() => UpdatePlayersList(entity.fids));
        }

        private GroupBox CreateLogPanel()
        {
            var logPanel = new GroupBox
            {
                Header = "房间动态",
                Margin = new Thickness(0, 10, 0, 0)
            };

            _logList = new ListBox
            {
                Margin = new Thickness(5),
                Height = 150,
                Background = new SolidColorBrush(Colors.White)
            };

            logPanel.Content = _logList;
            return logPanel;
        }

        public void AddLogEntry(string message, ConsoleColor color = ConsoleColor.Black)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(() => AddLogEntry(message, color));
                return;
            }

            var textBlock = new TextBlock
            {
                Text = $"[{DateTime.Now:HH:mm:ss}] {message}",
                Foreground = GetBrushFromConsoleColor(color),
                TextWrapping = TextWrapping.Wrap
            };

            _logList.Items.Insert(0, textBlock);
            while (_logList.Items.Count > 100)
            {
                _logList.Items.RemoveAt(_logList.Items.Count - 1);
            }
        }

        private static SolidColorBrush GetBrushFromConsoleColor(ConsoleColor color)
        {
            return color switch
            {
                ConsoleColor.Red => new SolidColorBrush(Colors.Red),
                ConsoleColor.Green => new SolidColorBrush(Colors.Green),
                ConsoleColor.Blue => new SolidColorBrush(Colors.Blue),
                ConsoleColor.Yellow => new SolidColorBrush(Colors.Orange),
                ConsoleColor.Cyan => new SolidColorBrush(Colors.Cyan),
                ConsoleColor.Magenta => new SolidColorBrush(Colors.Magenta),
                ConsoleColor.Gray => new SolidColorBrush(Colors.Gray),
                _ => new SolidColorBrush(Colors.Black)
            };
        }

        public void UpdateRoomInfoFromJson(JObject roomInfo)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(() => UpdateRoomInfoFromJson(roomInfo));
                return;
            }

            try
            {
                if (roomInfo.ContainsKey("game_status"))
                {
                    int gameStatus = roomInfo["game_status"].ToObject<int>();
                    _infoBlocks["GameStatus"].Text = gameStatus == 1 ? "在线" : "离线";
                    AddLogEntry($"游戏状态变更为: {(gameStatus == 1 ? "在线" : "离线")}", 
                        gameStatus == 1 ? ConsoleColor.Green : ConsoleColor.Red);
                }

                if (roomInfo.ContainsKey("owner_id"))
                {
                    string ownerId = roomInfo["owner_id"].ToString();
                    JObject ownerInfo = X19Http.Get_Player_Info(ownerId);
                    _infoBlocks["Owner"].Text = $"{ownerInfo["entity"]["name"]} ({ownerId})";
                    AddLogEntry($"房主更改为: {ownerInfo["entity"]["name"]} ({ownerId})", ConsoleColor.Yellow);
                }

                if (roomInfo.ContainsKey("room_name"))
                {
                    _infoBlocks["RoomNumber"].Text = roomInfo["room_name"].ToString();
                    AddLogEntry($"房间名称更改为: {roomInfo["room_name"]}", ConsoleColor.Cyan);
                }

                if (roomInfo.ContainsKey("password"))
                {
                    bool hasPassword = roomInfo["password"].ToObject<int>() != 0;
                    _infoBlocks["Password"].Text = hasPassword ? "是" : "否";
                    AddLogEntry($"密码保护更改为: {(hasPassword ? "是" : "否")}", ConsoleColor.Magenta);
                }

                if (roomInfo.ContainsKey("allow_save"))
                {
                    bool allowSave = roomInfo["allow_save"].ToObject<int>() != 0;
                    _infoBlocks["AllowSave"].Text = allowSave ? "是" : "否";
                    AddLogEntry($"允许保存更改为: {(allowSave ? "是" : "否")}", ConsoleColor.Blue);
                }

                if (roomInfo.ContainsKey("version"))
                {
                    _infoBlocks["Version"].Text = roomInfo["version"].ToString();
                    AddLogEntry($"版本号更改为: {roomInfo["version"]}", ConsoleColor.Gray);
                }
            }
            catch (Exception ex)
            {
                AddLogEntry($"更新房间信息时发生错误: {ex.Message}", ConsoleColor.Red);
            }
        }

        public async void UpdatePlayersList(List<string> playerIds)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(() => UpdatePlayersList(playerIds));
                return;
            }

            try
            {
                if (playerIds == null || playerIds.Count == 0)
                {
                    _infoBlocks["CurrentPlayers"].Text = "0";
                    return;
                }

                // 只更新当前人数显示
                _infoBlocks["CurrentPlayers"].Text = playerIds.Count.ToString();
            }
            catch (Exception ex)
            {
                AddLogEntry($"更新玩家列表时发生错误: {ex.Message}", ConsoleColor.Red);
            }
        }

        public async void HandlePlayerJoin(string playerId)
        {
            try
            {
                var playerInfo = await Task.Run(() => X19Http.Get_Player_Info(playerId));
                if (playerInfo != null && playerInfo["entity"] != null)
                {
                    string playerName = playerInfo["entity"]["name"].ToString();
                    AddLogEntry($"玩家 {playerName} ({playerId}) 加入了房间", ConsoleColor.Green);
                }
            }
            catch (Exception ex)
            {
                AddLogEntry($"处理玩家加入时发生错误: {ex.Message}", ConsoleColor.Red);
            }
        }

        public async void HandlePlayerLeave(string playerId)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(() => HandlePlayerLeave(playerId));
                return;
            }

            try
            {
                // 更新当前人数
                int currentCount;
                if (int.TryParse(_infoBlocks["CurrentPlayers"].Text, out currentCount) && currentCount > 0)
                {
                    _infoBlocks["CurrentPlayers"].Text = (currentCount - 1).ToString();
                }

                // 尝试获取玩家信息并记录日志
                try
                {
                    var playerInfo = await Task.Run(() => X19Http.Get_Player_Info(playerId));
                    if (playerInfo != null && 
                        playerInfo["entity"] != null && 
                        playerInfo["entity"]["name"] != null)
                    {
                        string playerName = playerInfo["entity"]["name"].ToString();
                        AddLogEntry($"玩家 {playerName} ({playerId}) 离开了房间", ConsoleColor.Yellow);
                    }
                    else
                    {
                        AddLogEntry($"玩家 (ID:{playerId}) 离开了房间", ConsoleColor.Yellow);
                    }
                }
                catch
                {
                    // 如果获取玩家信息失败，只记录ID
                    AddLogEntry($"玩家 (ID:{playerId}) 离开了房间", ConsoleColor.Yellow);
                }
            }
            catch (Exception ex)
            {
                AddLogEntry($"处理玩家离开时发生错误: {ex.Message}", ConsoleColor.Red);
            }
        }
    }
}