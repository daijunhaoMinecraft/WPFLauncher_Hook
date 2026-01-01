using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using DotNetTranstor.Hookevent;
using Newtonsoft.Json;
using WPFLauncher.Code;

namespace DotNetTranstor.Tools
{
    /// <summary>
    /// HTML资源管理类，用于存储和获取HTML内容
    /// </summary>
    public static class HtmlResource
    {
        
        // 获取RoomManage.html内容
        public static string GetRoomManageHtml()
        {
            // 直接返回嵌入的HTML内容，不再尝试从文件读取
            return GetDefaultRoomManageHtml();
        }


        // 获取默认的RoomManage.html内容
        private static string GetDefaultRoomManageHtml()
        {
            // 在这里放入默认的HTML内容，仅用作备份，通常应该使用现有的HTML文件
            return $@"<!DOCTYPE html>
<html lang=""zh-CN"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>房间管理系统</title>
    <style>
        :root {{
            --primary-color: #3498db;
            --secondary-color: #2ecc71;
            --danger-color: #e74c3c;
            --dark-color: #34495e;
            --light-color: #ecf0f1;
            --text-color: #2c3e50;
            --border-radius: 8px;
            --box-shadow: 0 4px 6px rgba(0,0,0,0.1);
            --transition: all 0.3s ease;
        }}
        
        * {{
            margin: 0;
            padding: 0;
            box-sizing: border-box;
        }}
        
        body {{
            font-family: ""Helvetica Neue"", Arial, sans-serif;
            line-height: 1.6;
            color: var(--text-color);
            background: linear-gradient(135deg, #f5f7fa 0%, #c3cfe2 100%);
            min-height: 100vh;
            padding: 20px;
        }}
        
        .container {{
            max-width: 1200px;
            margin: 0 auto;
            background-color: white;
            border-radius: var(--border-radius);
            box-shadow: var(--box-shadow);
            overflow: hidden;
        }}
        
        .header {{
            background-color: var(--primary-color);
            color: white;
            padding: 20px;
            text-align: center;
        }}
        
        .header h1 {{
            margin: 0;
            font-size: 28px;
        }}
        
        .ws-config {{
            background-color: var(--light-color);
            padding: 10px 20px;
            display: flex;
            align-items: center;
            flex-wrap: wrap;
            justify-content: center;
            border-bottom: 1px solid #ddd;
        }}
        
        .ws-config-label {{
            font-weight: bold;
            margin-right: 10px;
        }}
        
        .ws-config-input {{
            flex: 1;
            max-width: 400px;
            padding: 8px 12px;
            border: 1px solid #ddd;
            border-radius: 4px;
            font-size: 14px;
            margin-right: 10px;
        }}
        
        .ws-config-status {{
            margin-left: 10px;
            font-size: 14px;
            padding: 3px 8px;
            border-radius: 30px;
            background-color: var(--primary-color);
            color: white;
        }}
        
        .ws-config-status.connected {{
            background-color: var(--secondary-color);
        }}
        
        .ws-config-status.disconnected {{
            background-color: var(--danger-color);
        }}
        
        .room-info {{
            padding: 20px;
            background-color: var(--light-color);
            border-bottom: 1px solid #ddd;
        }}
        
        .room-info-grid {{
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(250px, 1fr));
            gap: 20px;
        }}
        
        .info-card {{
            background: white;
            padding: 15px;
            border-radius: var(--border-radius);
            box-shadow: 0 2px 4px rgba(0,0,0,0.05);
        }}
        
        .info-card h3 {{
            margin-bottom: 10px;
            color: var(--primary-color);
            font-size: 16px;
            border-bottom: 1px solid #eee;
            padding-bottom: 5px;
        }}
        
        .info-value {{
            font-size: 24px;
            font-weight: bold;
        }}
        
        .tabs {{
            display: flex;
            background-color: var(--dark-color);
        }}
        
        .tab {{
            padding: 15px 20px;
            color: white;
            cursor: pointer;
            transition: var(--transition);
            border-bottom: 3px solid transparent;
        }}
        
        .tab:hover {{
            background-color: rgba(255,255,255,0.1);
        }}
        
        .tab.active {{
            border-bottom-color: var(--secondary-color);
        }}
        
        .tab-content {{
            display: none;
            padding: 20px;
        }}
        
        .tab-content.active {{
            display: block;
        }}
        
        .players-grid {{
            display: grid;
            grid-template-columns: repeat(auto-fill, minmax(300px, 1fr));
            gap: 20px;
        }}
        
        .player-card {{
            display: flex;
            background: white;
            border-radius: var(--border-radius);
            overflow: hidden;
            box-shadow: 0 2px 4px rgba(0,0,0,0.05);
            transition: var(--transition);
            position: relative;
        }}
        
        .player-card:hover {{
            transform: translateY(-5px);
            box-shadow: 0 5px 15px rgba(0,0,0,0.1);
        }}
        
        .player-role {{
            position: absolute;
            top: 10px;
            right: 10px;
            background-color: var(--primary-color);
            color: white;
            padding: 3px 8px;
            border-radius: 30px;
            font-size: 12px;
        }}
        
        .player-role.owner {{
            background-color: var(--secondary-color);
        }}
        
        .player-avatar {{
            width: 80px;
            height: 80px;
            object-fit: cover;
        }}
        
        .player-info {{
            flex: 1;
            padding: 15px;
        }}
        
        .player-name {{
            font-weight: bold;
            margin-bottom: 5px;
        }}
        
        .player-id {{
            color: #777;
            font-size: 12px;
        }}

        .player-signature {{
            color: #888;
            font-size: 12px;
            font-style: italic;
            margin-top: 5px;
            white-space: nowrap;
            overflow: hidden;
            text-overflow: ellipsis;
            max-width: 100%;
        }}
        
        .empty-message {{
            text-align: center;
            padding: 50px 0;
            color: #999;
        }}
        
        .actions {{
            display: flex;
            margin-top: 10px;
        }}
        
        .btn {{
            padding: 8px 15px;
            border: none;
            border-radius: 4px;
            cursor: pointer;
            transition: var(--transition);
            margin-right: 10px;
            font-weight: bold;
        }}
        
        .btn-primary {{
            background-color: var(--primary-color);
            color: white;
        }}
        
        .btn-primary:hover {{
            background-color: #2980b9;
        }}
        
        .btn-success {{
            background-color: var(--secondary-color);
            color: white;
        }}
        
        .btn-success:hover {{
            background-color: #27ae60;
        }}
        
        .btn-danger {{
            background-color: var(--danger-color);
            color: white;
        }}
        
        .btn-danger:hover {{
            background-color: #c0392b;
        }}
        
        .btn-connect {{
            background-color: var(--secondary-color);
            color: white;
            padding: 8px 15px;
            border: none;
            border-radius: 4px;
            cursor: pointer;
            transition: var(--transition);
            font-weight: bold;
        }}
        
        .btn-connect:hover {{
            background-color: #27ae60;
            transform: translateY(-2px);
            box-shadow: 0 3px 5px rgba(0,0,0,0.2);
        }}
        
        .notification {{
            position: fixed;
            bottom: 20px;
            right: 20px;
            padding: 15px 20px;
            background-color: var(--secondary-color);
            color: white;
            border-radius: var(--border-radius);
            box-shadow: var(--box-shadow);
            transform: translateY(100px);
            opacity: 0;
            transition: all 0.5s ease;
            z-index: 1000;
            max-width: 300px;
        }}
        
        .notification.show {{
            transform: translateY(0);
            opacity: 1;
        }}
        
        .notification.error {{
            background-color: var(--danger-color);
        }}

        .resource-card {{
            background: white;
            border-radius: var(--border-radius);
            overflow: hidden;
            box-shadow: 0 2px 4px rgba(0,0,0,0.05);
            margin-bottom: 20px;
        }}

        .resource-image {{
            width: 100%;
            height: 150px;
            object-fit: cover;
        }}

        .resource-info {{
            padding: 15px;
        }}

        .resource-name {{
            font-weight: bold;
            margin-bottom: 10px;
            font-size: 18px;
        }}

        .resource-description {{
            color: #666;
            font-size: 14px;
            max-height: 100px;
            overflow-y: auto;
        }}

        .blacklist-controls {{
            margin-bottom: 20px;
            padding: 15px;
            background: white;
            border-radius: var(--border-radius);
        }}

        .blacklist-input {{
            display: flex;
            margin-bottom: 10px;
        }}

        .blacklist-input input {{
            flex: 1;
            padding: 8px;
            border: 1px solid #ddd;
            border-radius: 4px 0 0 4px;
        }}

        .blacklist-input button {{
            border-radius: 0 4px 4px 0;
            padding: 8px 15px;
        }}

        @media (max-width: 768px) {{
            .room-info-grid {{
                grid-template-columns: 1fr;
            }}
            
            .players-grid {{
                grid-template-columns: 1fr;
            }}
            
            .header h1 {{
                font-size: 22px;
            }}
        }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>房间管理系统</h1>
        </div>
        
        <div class=""ws-config"">
            <div class=""ws-config-label"">WebSocket服务器:</div>
            <input type=""text"" id=""wsUrl"" class=""ws-config-input"" placeholder=""ws://服务器地址:端口"" style=""flex: 3;"">
            <button class=""btn-connect"" id=""connectWsBtn"">连接</button>
            <div class=""ws-config-status"" id=""wsStatus"">未连接</div>
        </div>
        
        <div class=""room-info"">
            <h2>房间信息</h2>
            <div class=""room-info-grid"">
                <div class=""info-card"">
                    <h3>房间名称</h3>
                    <div class=""info-value"" id=""roomName"">加载中...</div>
                </div>
                <div class=""info-card"">
                    <h3>房间ID</h3>
                    <div class=""info-value"" id=""roomId"">加载中...</div>
                </div>
                <div class=""info-card"">
                    <h3>房间密码</h3>
                    <div class=""info-value"" id=""RoomPassword"">加载中...</div>
                </div>
                <div class=""info-card"">
                    <h3>当前人数</h3>
                    <div class=""info-value"" id=""playerCount"">0/0</div>
                </div>
                <div class=""info-card"">
                    <h3>需要密码</h3>
                    <div class=""info-value"" id=""needPassword"">否</div>
                </div>
                <div class=""info-card"">
                    <h3>游戏版本</h3>
                    <div class=""info-value"" id=""gameVersion"">加载中...</div>
                </div>
                <div class=""info-card"">
                    <h3>房间状态</h3>
                    <div class=""info-value"" id=""roomStatus"">加载中...</div>
                </div>
            </div>
        </div>

        <div class=""tabs"">
            <div class=""tab active"" data-tab=""players"">房间成员</div>
            <div class=""tab"" data-tab=""blacklist"">黑名单管理</div>
            <div class=""tab"" data-tab=""resource"">地图资源</div>
        </div>
        
        <div class=""tab-content active"" id=""players-tab"">
            <h2>房间成员</h2>
            <div class=""players-grid"" id=""players-container"">
                <!-- 玩家卡片会通过JavaScript动态生成 -->
            </div>
            <div class=""empty-message"" id=""no-players-message"" style=""display: none;"">
                <p>房间中还没有玩家</p>
            </div>
        </div>
        
        <div class=""tab-content"" id=""blacklist-tab"">
            <h2>黑名单管理</h2>
            
            <div class=""blacklist-controls"">
                <h3>添加用户到黑名单</h3>
                <div class=""blacklist-input"">
                    <input type=""text"" id=""blacklistUserId"" placeholder=""输入用户ID"">
                    <button class=""btn btn-primary"" id=""addBlacklistBtn"">添加</button>
                </div>
                
                <h3>添加正则表达式黑名单</h3>
                <div class=""blacklist-input"">
                    <input type=""text"" id=""regexPattern"" placeholder=""输入正则表达式"">
                    <button class=""btn btn-primary"" id=""addRegexBtn"">添加</button>
                </div>
            </div>
            
            <h3>用户黑名单</h3>
            <div class=""players-grid"" id=""blacklist-container"">
                <!-- 黑名单会通过JavaScript动态生成 -->
            </div>
            <div class=""empty-message"" id=""no-blacklist-message"" style=""display: none;"">
                <p>黑名单中还没有用户</p>
            </div>
            
            <h3>正则表达式黑名单</h3>
            <div class=""players-grid"" id=""regex-blacklist-container"">
                <!-- 正则黑名单会通过JavaScript动态生成 -->
            </div>
            <div class=""empty-message"" id=""no-regex-message"" style=""display: none;"">
                <p>正则表达式黑名单为空</p>
            </div>
        </div>
        
        <div class=""tab-content"" id=""resource-tab"">
            <h2>地图资源</h2>
            <div class=""resource-card"" id=""resource-container"">
                <img id=""resource-image"" class=""resource-image"" src="""" alt=""地图封面"">
                <div class=""resource-info"">
                    <div class=""resource-name"" id=""resource-name"">加载中...</div>
                    <div class=""resource-description"" id=""resource-description""></div>
                </div>
            </div>
        </div>
    </div>
    
    <div class=""notification"" id=""notification""></div>

    <script>
        // 全局变量
        let ws = null;
        let roomInfo = null;
        let players = [];
        let blacklist = [];
        let regexBlacklist = [];
        let currentUserId = '';
        let isOwner = false;
        let customWsServer = '';
        let isCustomWs = false;
        let wsConnected = false; // 用于跟踪WebSocket连接状态

        // 初始化页面
        function init() {{
            // 初始化WebSocket配置
            initWebSocketConfig();
            
            // 设置选项卡切换
            setupTabs();
            
            // 设置按钮事件
            setupButtons();
            
            // 获取初始数据
            fetchRoomInfo();
        }}
        
        // 初始化WebSocket配置
        function initWebSocketConfig() {{
            // 从localStorage获取自定义WebSocket配置
            customWsServer = localStorage.getItem('customWsServer') || '';
            isCustomWs = localStorage.getItem('isCustomWs') === 'true';
            
            // 获取默认的WebSocket URL
            const defaultWsUrl = `ws://localhost:${{getWebSocketPort()}}/websocket`;
            
            // 更新UI
            document.getElementById('wsUrl').value = isCustomWs && customWsServer ? customWsServer : defaultWsUrl;
            
            // 不再自动连接，等待用户点击按钮
            document.getElementById('wsStatus').textContent = '未连接';
            document.getElementById('wsStatus').classList.remove('connected');
            document.getElementById('wsStatus').classList.add('disconnected');
        }}

        // 连接WebSocket
        function connectWebSocket() {{
            // 断开现有连接
            if (ws && ws.readyState !== WebSocket.CLOSED) {{
                ws.close();
                // 给关闭一些时间
                setTimeout(function() {{
                    actuallyConnect();
                }}, 300);
            }} else {{
                actuallyConnect();
            }}
        }}
        
        // 实际执行连接操作
        function actuallyConnect() {{
            let wsUrl = '';
            const inputUrl = document.getElementById('wsUrl').value.trim();
            
            if (isCustomWs && customWsServer) {{
                // 使用自定义WebSocket服务器
                wsUrl = customWsServer;
            }} else {{
                // 使用默认或输入框中的WebSocket服务器
                if (inputUrl && (inputUrl.startsWith('ws://') || inputUrl.startsWith('wss://'))) {{
                    wsUrl = inputUrl;
                }} else {{
                    wsUrl = `ws://localhost:${{getWebSocketPort()}}/websocket`;
                    document.getElementById('wsUrl').value = wsUrl;
                }}
            }}
            
            document.getElementById('wsStatus').textContent = '正在连接...';
            document.getElementById('wsStatus').className = 'ws-config-status';
            
            try {{
                ws = new WebSocket(wsUrl);
                
                ws.onopen = function() {{
                    showNotification(""WebSocket连接已建立"", ""success"");
                    wsConnected = true;
                    console.log(""WebSocket连接已建立: "" + wsUrl);
                    document.getElementById('wsStatus').textContent = '已连接';
                    document.getElementById('wsStatus').classList.add('connected');
                    document.getElementById('wsStatus').classList.remove('disconnected');
                }};
                
                ws.onmessage = function(event) {{
                    try {{
                        const data = JSON.parse(event.data);
                        console.log(""收到WebSocket消息:"", data);
                        
                        // 根据消息类型更新界面
                        if (data.type === ""RoomManage"") {{
                            handleRoomManageMessage(data);
                        }} else if (data.type === ""Recv_Pocket"") {{
                            handleRecvPocketMessage(data);
                        }}
                    }} catch (e) {{
                        console.error(""解析WebSocket消息失败:"", e);
                    }}
                }};
                
                ws.onclose = function() {{
                    console.log(""WebSocket连接已关闭"");
                    document.getElementById('wsStatus').textContent = '已断开';
                    document.getElementById('wsStatus').classList.remove('connected');
                    document.getElementById('wsStatus').classList.add('disconnected');
                    if(wsConnected) {{
                        showNotification(""WebSocket连接已断开，请手动重新连接"", ""error"");
                        wsConnected = false;
                    }}
                    // 不再自动重连
                }};
                
                ws.onerror = function(error) {{
                    console.error(""WebSocket错误:"", error);
                    document.getElementById('wsStatus').textContent = '连接错误';
                    document.getElementById('wsStatus').classList.remove('connected');
                    document.getElementById('wsStatus').classList.add('disconnected');
                    showNotification(""WebSocket连接出错，请检查服务器地址和端口"", ""error"");
                    wsConnected = false;
                }};
            }} catch (error) {{
                console.error(""创建WebSocket连接失败:"", error);
                document.getElementById('wsStatus').textContent = '连接失败';
                document.getElementById('wsStatus').classList.remove('connected');
                document.getElementById('wsStatus').classList.add('disconnected');
                showNotification(""创建WebSocket连接失败: "" + error.message, ""error"");
            }}
        }}

        // 从URL获取WebSocket端口
        function getWebSocketPort() {{
            // 默认端口
            let port = {Path_Bool.HttpPort.ToString()};
            
            // 尝试从URL参数获取端口
            const urlParams = new URLSearchParams(window.location.search);
            const portParam = urlParams.get('wsport');
            
            if (portParam && !isNaN(parseInt(portParam))) {{
                port = parseInt(portParam);
            }}
            
            return port;
        }}

        // 处理RoomManage类型的消息
        function handleRoomManageMessage(data) {{
            switch(data.status) {{
                case ""CreateRoom"":
                case ""JoinRoom"":
                case ""GetRoomInfo"":
                    updateRoomInfo(data.data);
                    break;
                case ""Leave"":
                    // 如果离开的是当前房间，显示提示
                    if (data.data.roomId === document.getElementById(""roomId"").textContent) {{
                        showNotification(""你已离开房间"", ""error"");
                        // 不再自动跳转到房间管理页面
                        fetchRoomInfo(); // 刷新房间信息
                    }}
                    break;
                case ""UpdatePlayers"":
                    if (data.data.players) {{
                        players = data.data.players;
                        renderPlayersGrid();
                    }}
                    if (data.data.roomInfo) {{
                        updateRoomInfo(data.data.roomInfo);
                    }}
                    break;
            }}
        }}

        // 处理Recv_Pocket类型的消息
        function handleRecvPocketMessage(data) {{
            switch(data.status) {{
                case ""join"":
                    showNotification(`玩家 ${{data.playerName}} 加入了房间`, ""success"");
                    fetchRoomInfo(); // 刷新房间信息
                    break;
                case ""leave"":
                    showNotification(`玩家 ${{data.playerName}} 离开了房间`, ""success"");
                    fetchRoomInfo(); // 刷新房间信息
                    break;
                case ""kick"":
                    if (!data.playerName) {{
                        // 自己被踢了
                        showNotification(""你已被踢出房间"", ""error"");
                        // 不再自动跳转到房间管理页面
                        fetchRoomInfo(); // 刷新房间信息
                    }} else {{
                        showNotification(`玩家 ${{data.playerName}} 被踢出房间`, ""success"");
                        fetchRoomInfo(); // 刷新房间信息
                    }}
                    break;
                case ""room_update"":
                    fetchRoomInfo(); // 刷新房间信息
                    break;
                default:
                    // 处理没有status字段的消息
                    if (data.data && (data.data.entity_id || data.data.room_name)) {{
                        // 这可能是房间信息更新
                        fetchRoomInfo();
                    }}
                    break;
            }}
        }}

        // 设置选项卡切换
        function setupTabs() {{
            const tabs = document.querySelectorAll("".tab"");
            tabs.forEach(tab => {{
                tab.addEventListener(""click"", function() {{
                    // 移除所有标签和内容的active类
                    tabs.forEach(t => t.classList.remove(""active""));
                    document.querySelectorAll("".tab-content"").forEach(c => c.classList.remove(""active""));
                    
                    // 给当前标签和对应内容添加active类
                    this.classList.add(""active"");
                    const tabId = this.getAttribute(""data-tab"") + ""-tab"";
                    document.getElementById(tabId).classList.add(""active"");
                }});
            }});
        }}
        
        // 设置按钮事件
        function setupButtons() {{
            // WebSocket服务器连接按钮
            document.getElementById(""connectWsBtn"").addEventListener(""click"", function() {{
                const wsUrlInput = document.getElementById(""wsUrl"").value.trim();
                
                if (wsUrlInput && (wsUrlInput.startsWith('ws://') || wsUrlInput.startsWith('wss://'))) {{
                    // 保存自定义WebSocket配置
                    localStorage.setItem('customWsServer', wsUrlInput);
                    localStorage.setItem('isCustomWs', 'true');
                    
                    customWsServer = wsUrlInput;
                    isCustomWs = true;
                    
                    // 显示连接中状态
                    document.getElementById('wsStatus').textContent = '正在连接...';
                    document.getElementById('wsStatus').className = 'ws-config-status';
                    
                    // 连接到指定服务器
                    connectWebSocket();
                }} else if (!wsUrlInput) {{
                    // 恢复默认设置
                    localStorage.removeItem('customWsServer');
                    localStorage.setItem('isCustomWs', 'false');
                    
                    customWsServer = '';
                    isCustomWs = false;
                    
                    // 设置默认URL
                    document.getElementById(""wsUrl"").value = `ws://localhost:${{getWebSocketPort()}}/websocket`;
                    
                    // 显示连接中状态
                    document.getElementById('wsStatus').textContent = '正在连接...';
                    document.getElementById('wsStatus').className = 'ws-config-status';
                    
                    // 连接到默认服务器
                    connectWebSocket();
                }} else {{
                    showNotification(""请输入有效的WebSocket URL，格式为ws://地址:端口 或 wss://地址:端口"", ""error"");
                }}
            }});
            
            // 添加到黑名单按钮
            document.getElementById(""addBlacklistBtn"").addEventListener(""click"", function() {{
                const userId = document.getElementById(""blacklistUserId"").value.trim();
                if (userId) {{
                    addToBlacklist(userId);
                    document.getElementById(""blacklistUserId"").value = """";
                }} else {{
                    showNotification(""请输入有效的用户ID"", ""error"");
                }}
            }});
            
            // 添加正则表达式黑名单按钮
            document.getElementById(""addRegexBtn"").addEventListener(""click"", function() {{
                const pattern = document.getElementById(""regexPattern"").value.trim();
                if (pattern) {{
                    addToRegexBlacklist(pattern);
                    document.getElementById(""regexPattern"").value = """";
                }} else {{
                    showNotification(""请输入有效的正则表达式"", ""error"");
                }}
            }});
        }}
        
        // 获取房间信息
        function fetchRoomInfo() {{
            fetch(""/get_roominfo"")
                .then(response => response.json())
                .then(data => {{
                    console.log(""获取房间信息成功:"", data);
                    
                    // 设置房间信息
                    roomInfo = data;
                    
                    // 更新UI
                    updateRoomInfoUI(data);
                    
                    // 获取当前用户ID
                    currentUserId = data.currentUserId || """";
                    
                    // 判断是否是房主
                    isOwner = data.isOwner || (data.roomInfo && data.roomInfo.entity && 
                              data.roomInfo.entity.owner_id === currentUserId);
                    
                    // 处理玩家列表
                    if (data.playersList) {{
                        players = data.playersList;
                        renderPlayersGrid();
                    }}
                    
                    // 不再从这里获取黑名单，而是通过专门的API获取
                    
                    // 处理资源信息
                    if (data.resourceInfo && data.resourceInfo.entity) {{
                        updateResourceInfo(data.resourceInfo.entity, data.roomTitleImage);
                    }}
                    
                    // 不再自动连接WebSocket
                }})
                .catch(error => {{
                    console.error(""获取房间信息失败:"", error);
                    showNotification(""获取房间信息失败，请刷新页面重试"", ""error"");
                }});
                
            // 单独获取黑名单数据
            fetchUserBlacklist();
            fetchRegexBlacklist();
        }}
        
        // 获取用户黑名单
        function fetchUserBlacklist() {{
            fetch(""/api/blacklists/users"")
                .then(response => response.json())
                .then(data => {{
                    if (data.error === 0) {{
                        // 更新黑名单数据
                        blacklist = data.users.map(user => user.userId);
                        // 渲染黑名单
                        renderBlacklistGrid(data.users);
                    }} else {{
                        console.error(""获取用户黑名单失败:"", data.message);
                    }}
                }})
                .catch(error => {{
                    console.error(""获取用户黑名单请求失败:"", error);
                }});
        }}
        
        // 获取正则黑名单
        function fetchRegexBlacklist() {{
            fetch(""/api/blacklists/regex"")
                .then(response => response.json())
                .then(data => {{
                    if (data.error === 0) {{
                        // 更新正则黑名单数据
                        regexBlacklist = data.patterns.map(p => p.pattern);
                        // 渲染正则黑名单
                        renderRegexBlacklistGrid();
                    }} else {{
                        console.error(""获取正则黑名单失败:"", data.message);
                    }}
                }})
                .catch(error => {{
                    console.error(""获取正则黑名单请求失败:"", error);
                }});
        }}
        
        // 渲染黑名单
        function renderBlacklistGrid(userInfoArray) {{
            const container = document.getElementById(""blacklist-container"");
            const noBlacklistMessage = document.getElementById(""no-blacklist-message"");
            
            if (!userInfoArray || userInfoArray.length === 0) {{
                container.innerHTML = """";
                noBlacklistMessage.style.display = ""block"";
                return;
            }}
            
            noBlacklistMessage.style.display = ""none"";
            container.innerHTML = """";
            
            userInfoArray.forEach(userInfo => {{
                const blacklistCard = document.createElement(""div"");
                blacklistCard.className = ""player-card"";
                blacklistCard.dataset.userId = userInfo.userId;
                
                blacklistCard.innerHTML = `
                    <img class=""player-avatar"" src=""${{userInfo.avatarUrl || 'https://x19.fp.ps.netease.com/file/5a34e0777f9d2a8a4ea3d36eza31LhW3'}}"" alt=""${{userInfo.userName}}"">
                    <div class=""player-info"">
                        <div class=""player-name"">${{userInfo.userName || '未知玩家'}}</div>
                        <div class=""player-id"">ID: ${{userInfo.userId}}</div>
                        <div class=""player-signature"">${{userInfo.signature || ''}}</div>
                        <div class=""actions"">
                            <button class=""btn btn-danger remove-blacklist-btn"" data-user-id=""${{userInfo.userId}}"">移除黑名单</button>
                        </div>
                    </div>
                `;
                
                container.appendChild(blacklistCard);
            }});
            
            // 添加移除黑名单的事件监听
            document.querySelectorAll("".remove-blacklist-btn"").forEach(btn => {{
                btn.addEventListener(""click"", function() {{
                    const userId = this.getAttribute(""data-user-id"");
                    removeFromBlacklist(userId);
                }});
            }});
        }}
        
        // 渲染玩家列表
        function renderPlayersGrid() {{
            const container = document.getElementById(""players-container"");
            const noPlayersMessage = document.getElementById(""no-players-message"");
            
            if (!players || players.length === 0) {{
                container.innerHTML = """";
                noPlayersMessage.style.display = ""block"";
                return;
            }}
            
            noPlayersMessage.style.display = ""none"";
            container.innerHTML = """";
            
            players.forEach(player => {{
                const playerCard = document.createElement(""div"");
                playerCard.className = ""player-card"";
                playerCard.dataset.userId = player.userId;
                
                const isPlayerOwner = player.role === ""房主"" || player.ident === 1;
                
                playerCard.innerHTML = `
                    <img class=""player-avatar"" src=""${{player.avatarUrl || 'https://x19.fp.ps.netease.com/file/5a34e0777f9d2a8a4ea3d36eza31LhW3'}}"" alt=""${{player.playerName}}"">
                    <div class=""player-role ${{isPlayerOwner ? 'owner' : ''}}"">${{player.role || (isPlayerOwner ? '房主' : '成员')}}</div>
                    <div class=""player-info"">
                        <div class=""player-name"">${{player.playerName || '未知玩家'}}</div>
                        <div class=""player-id"">ID: ${{player.userId}}</div>
                        <div class=""player-signature"">${{player.signature || ''}}</div>
                        ${{!isPlayerOwner && isOwner ? `
                            <div class=""actions"">
                                <button class=""btn btn-danger kick-player-btn"" data-user-id=""${{player.userId}}"">踢出</button>
                                <button class=""btn btn-danger add-blacklist-btn"" data-user-id=""${{player.userId}}"">加入黑名单</button>
                            </div>
                        ` : ''}}
                    </div>
                `;
                
                container.appendChild(playerCard);
            }});
            
            // 添加踢出玩家的事件监听
            document.querySelectorAll("".kick-player-btn"").forEach(btn => {{
                btn.addEventListener(""click"", function() {{
                    const userId = this.getAttribute(""data-user-id"");
                    kickPlayer(userId);
                }});
            }});
            
            // 添加加入黑名单的事件监听
            document.querySelectorAll("".add-blacklist-btn"").forEach(btn => {{
                btn.addEventListener(""click"", function() {{
                    const userId = this.getAttribute(""data-user-id"");
                    addToBlacklist(userId);
                }});
            }});
        }}
        
        // 渲染正则黑名单
        function renderRegexBlacklistGrid() {{
            const container = document.getElementById(""regex-blacklist-container"");
            const noRegexMessage = document.getElementById(""no-regex-message"");
            
            if (!regexBlacklist || regexBlacklist.length === 0) {{
                container.innerHTML = """";
                noRegexMessage.style.display = ""block"";
                return;
            }}
            
            noRegexMessage.style.display = ""none"";
            container.innerHTML = """";
            
            regexBlacklist.forEach(pattern => {{
                const regexCard = document.createElement(""div"");
                regexCard.className = ""player-card"";
                regexCard.dataset.pattern = pattern;
                
                regexCard.innerHTML = `
                    <div class=""player-info"">
                        <div class=""player-name"">正则表达式</div>
                        <div class=""player-id"">${{pattern}}</div>
                        <div class=""actions"">
                            <button class=""btn btn-danger remove-regex-btn"" data-pattern=""${{pattern}}"">移除</button>
                        </div>
                    </div>
                `;
                
                container.appendChild(regexCard);
            }});
            
            // 添加移除正则黑名单的事件监听
            document.querySelectorAll("".remove-regex-btn"").forEach(btn => {{
                btn.addEventListener(""click"", function() {{
                    const pattern = this.getAttribute(""data-pattern"");
                    removeFromRegexBlacklist(pattern);
                }});
            }});
        }}
        
        // 踢出玩家
        function kickPlayer(userId) {{
            if (!isOwner) {{
                showNotification(""只有房主才能踢出玩家"", ""error"");
                return;
            }}
            
            showNotification(""正在踢出玩家..."", ""success"");
            
            fetch(`/Room/Kick/${{userId}}`)
                .then(response => response.json())
                .then(data => {{
                    if (data.error === 0) {{
                        showNotification(""玩家已被踢出"", ""success"");
                        // 刷新玩家列表
                        fetchRoomInfo();
                    }} else {{
                        showNotification(`踢出失败: ${{data.message}}`, ""error"");
                    }}
                }})
                .catch(error => {{
                    console.error(""踢出玩家请求失败:"", error);
                    showNotification(""踢出请求失败，请重试"", ""error"");
                }});
        }}
        
        // 添加到黑名单
        function addToBlacklist(userId) {{
            fetch(`/Room/AddBlacklist/${{userId}}`)
                .then(response => response.json())
                .then(data => {{
                    if (data.error === 0) {{
                        showNotification(""已添加到黑名单"", ""success"");
                        // 刷新黑名单
                        fetchUserBlacklist();
                    }} else {{
                        showNotification(`添加失败: ${{data.message}}`, ""error"");
                    }}
                }})
                .catch(error => {{
                    console.error(""添加黑名单请求失败:"", error);
                    showNotification(""添加请求失败，请重试"", ""error"");
                }});
        }}
        
        // 从黑名单移除
        function removeFromBlacklist(userId) {{
            fetch(`/Room/RemoveBlacklist/${{userId}}`)
                .then(response => response.json())
                .then(data => {{
                    if (data.error === 0) {{
                        showNotification(""已从黑名单移除"", ""success"");
                        // 刷新黑名单
                        fetchUserBlacklist();
                    }} else {{
                        showNotification(`移除失败: ${{data.message}}`, ""error"");
                    }}
                }})
                .catch(error => {{
                    console.error(""移除黑名单请求失败:"", error);
                    showNotification(""移除请求失败，请重试"", ""error"");
                }});
        }}
        
        // 添加到正则黑名单
        function addToRegexBlacklist(pattern) {{
            // 使用POST方法发送请求
            fetch(""/Room/AddRegexBlacklist"", {{
                method: ""POST"",
                headers: {{
                    ""Content-Type"": ""application/json""
                }},
                body: JSON.stringify({{ pattern: pattern }})
            }})
            .then(response => response.json())
            .then(data => {{
                if (data.error === 0) {{
                    showNotification(""已添加到正则黑名单"", ""success"");
                    // 刷新正则黑名单
                    fetchRegexBlacklist();
                }} else {{
                    showNotification(`添加失败: ${{data.message}}`, ""error"");
                }}
            }})
            .catch(error => {{
                console.error(""添加正则黑名单请求失败:"", error);
                showNotification(""添加请求失败，请重试"", ""error"");
            }});
        }}
        
        // 从正则黑名单移除
        function removeFromRegexBlacklist(pattern) {{
            // 使用POST方法发送请求
            fetch(""/Room/RemoveRegexBlacklist"", {{
                method: ""POST"",
                headers: {{
                    ""Content-Type"": ""application/json""
                }},
                body: JSON.stringify({{ pattern: pattern }})
            }})
            .then(response => response.json())
            .then(data => {{
                if (data.error === 0) {{
                    showNotification(""已从正则黑名单移除"", ""success"");
                    // 刷新正则黑名单
                    fetchRegexBlacklist();
                }} else {{
                    showNotification(`移除失败: ${{data.message}}`, ""error"");
                }}
            }})
            .catch(error => {{
                console.error(""移除正则黑名单请求失败:"", error);
                showNotification(""移除请求失败，请重试"", ""error"");
            }});
        }}
        
        // 更新房间信息
        function updateRoomInfo(roomData) {{
            if (!roomData) return;
            
            if (roomData.entity) {{
                const entity = roomData.entity;
                document.getElementById(""roomName"").textContent = entity.room_name || ""未命名"";
                document.getElementById(""roomId"").textContent = entity.entity_id || ""-"";
                document.getElementById(""playerCount"").textContent = `${{entity.cur_num || 0}}/${{entity.max_count || 10}}`;
                document.getElementById(""needPassword"").textContent = entity.password ? ""是"" : ""否"";
                document.getElementById(""RoomPassword"").textContent = entity.password ? ""是"" : ""否"";
                document.getElementById(""gameVersion"").textContent = entity.version || ""-"";
                document.getElementById(""roomStatus"").textContent = entity.game_status === 1 ? ""在线"" : ""离线"";
            }}
        }}
        
        // 更新房间信息UI
        function updateRoomInfoUI(data) {{
            // 基本房间信息
            if (data.roomInfo && data.roomInfo.entity) {{
                const entity = data.roomInfo.entity;
                document.getElementById(""roomName"").textContent = entity.room_name || ""未命名"";
                document.getElementById(""roomId"").textContent = entity.entity_id || ""-"";
                document.getElementById(""playerCount"").textContent = `${{entity.cur_num || 0}}/${{entity.max_count || 10}}`;
                document.getElementById(""needPassword"").textContent = entity.password ? ""是"" : ""否"";
                document.getElementById(""RoomPassword"").textContent = data.password || ""-"";
                document.getElementById(""gameVersion"").textContent = entity.version || ""-"";
                document.getElementById(""roomStatus"").textContent = entity.game_status === 1 ? ""在线"" : ""离线"";
            }} else if (data.roomName) {{
                // 备选方式获取房间信息
                document.getElementById(""roomName"").textContent = data.roomName || ""未命名"";
                document.getElementById(""roomId"").textContent = data.roomId || ""-"";
                document.getElementById(""playerCount"").textContent = `${{data.currentPlayers || 0}}/${{data.maxPlayers || 10}}`;
                document.getElementById(""needPassword"").textContent = data.hasPassword ? ""是"" : ""否"";
                document.getElementById(""RoomPassword"").textContent = data.password || ""-"";
                document.getElementById(""gameVersion"").textContent = data.version || ""-"";
                document.getElementById(""roomStatus"").textContent = data.gameStatus === 1 ? ""在线"" : ""离线"";
            }}
        }}
        
        // 更新资源信息
        function updateResourceInfo(resourceEntity, titleImage) {{
            if (!resourceEntity) return;
            
            document.getElementById(""resource-name"").textContent = resourceEntity.name || ""未知地图"";
            
            // 设置地图描述，如果有detail_description则使用，否则使用brief_summary
            const description = resourceEntity.detail_description || resourceEntity.brief_summary || """";
            document.getElementById(""resource-description"").innerHTML = description;
            
            // 设置地图图片
            if (titleImage && titleImage.entities && titleImage.entities.length > 0) {{
                document.getElementById(""resource-image"").src = titleImage.entities[0].title_image_url;
            }} else if (resourceEntity.brief_image_urls && resourceEntity.brief_image_urls.length > 0) {{
                document.getElementById(""resource-image"").src = resourceEntity.brief_image_urls[0];
            }}
        }}
        
        // 显示通知
        function showNotification(message, type = ""success"") {{
            const notification = document.getElementById(""notification"");
            notification.textContent = message;
            notification.className = ""notification "" + (type === ""error"" ? ""error"" : """");
            notification.classList.add(""show"");
            
            setTimeout(() => {{
                notification.classList.remove(""show"");
            }}, 3000);
        }}
        
        // 页面加载时初始化
        window.addEventListener(""load"", init);
    </script>
</body>
</html> ";
        }
        public static string GetHotUpdateHtml()
        {
            return """
                   <!DOCTYPE html>
                   <html lang="zh-CN">
                   <head>
                       <meta charset="UTF-8">
                       <meta name="viewport" content="width=device-width, initial-scale=1.0">
                       <title>热更新配置管理</title>
                       <style>
                           :root {
                               --primary-color: #3498db;
                               --secondary-color: #2ecc71;
                               --danger-color: #e74c3c;
                               --dark-color: #34495e;
                               --light-color: #ecf0f1;
                               --text-color: #2c3e50;
                               --border-radius: 8px;
                               --box-shadow: 0 4px 6px rgba(0,0,0,0.1);
                               --transition: all 0.3s ease;
                           }
                           
                           * {
                               margin: 0;
                               padding: 0;
                               box-sizing: border-box;
                           }
                           
                           body {
                               font-family: "Helvetica Neue", Arial, sans-serif;
                               line-height: 1.6;
                               color: var(--text-color);
                               background: linear-gradient(135deg, #f5f7fa 0%, #c3cfe2 100%);
                               min-height: 100vh;
                               padding: 20px;
                           }
                           
                           .container {
                               max-width: 1200px;
                               margin: 0 auto;
                               background-color: white;
                               border-radius: var(--border-radius);
                               box-shadow: var(--box-shadow);
                               overflow: hidden;
                           }
                           
                           .header {
                               background-color: var(--primary-color);
                               color: white;
                               padding: 20px;
                               text-align: center;
                           }
                           
                           .header h1 {
                               margin: 0;
                               font-size: 28px;
                           }
                           
                           .config-grid {
                               display: grid;
                               grid-template-columns: repeat(auto-fit, minmax(300px, 1fr));
                               gap: 20px;
                               padding: 20px;
                           }
                           
                           .config-card {
                               background: white;
                               padding: 20px;
                               border-radius: var(--border-radius);
                               box-shadow: 0 2px 4px rgba(0,0,0,0.05);
                               border: 1px solid #eee;
                           }
                           
                           .config-card h3 {
                               margin-bottom: 15px;
                               color: var(--primary-color);
                               font-size: 18px;
                               border-bottom: 1px solid #eee;
                               padding-bottom: 10px;
                           }
                           
                           .form-group {
                               margin-bottom: 15px;
                           }
                           
                           .form-group label {
                               display: block;
                               margin-bottom: 5px;
                               font-weight: bold;
                           }
                           
                           .form-group input, .form-group select {
                               width: 100%;
                               padding: 10px;
                               border: 1px solid #ddd;
                               border-radius: 4px;
                               font-size: 14px;
                           }
                           
                           .form-group input[type="checkbox"] {
                               width: auto;
                               margin-right: 10px;
                           }
                           
                           .btn {
                               display: inline-block;
                               padding: 12px 24px;
                               background-color: var(--primary-color);
                               color: white;
                               border: none;
                               border-radius: var(--border-radius);
                               cursor: pointer;
                               font-size: 16px;
                               transition: var(--transition);
                               text-align: center;
                           }
                           
                           .btn:hover {
                               background-color: #2980b9;
                               transform: translateY(-2px);
                           }
                           
                           .btn-success {
                               background-color: var(--secondary-color);
                           }
                           
                           .btn-success:hover {
                               background-color: #27ae60;
                           }
                           
                           .btn-danger {
                               background-color: var(--danger-color);
                           }
                           
                           .btn-danger:hover {
                               background-color: #c0392b;
                           }
                           
                           .actions {
                               padding: 20px;
                               text-align: center;
                               border-top: 1px solid #eee;
                           }
                           
                           .notification {
                               position: fixed;
                               top: 20px;
                               right: 20px;
                               padding: 15px 20px;
                               border-radius: 4px;
                               color: white;
                               font-weight: bold;
                               box-shadow: 0 2px 10px rgba(0,0,0,0.2);
                               z-index: 1000;
                               display: none;
                           }
                           
                           .notification.success {
                               background-color: var(--secondary-color);
                           }
                           
                           .notification.error {
                               background-color: var(--danger-color);
                           }
                           
                           .api-doc {
                               padding: 20px;
                               background-color: var(--light-color);
                               border-top: 1px solid #ddd;
                           }
                           
                           .api-doc h2 {
                               color: var(--dark-color);
                               margin-bottom: 15px;
                           }
                           
                           .endpoint {
                               background: white;
                               border-left: 4px solid var(--primary-color);
                               padding: 15px;
                               margin: 15px 0;
                               border-radius: 0 4px 4px 0;
                           }
                           
                           .method {
                               display: inline-block;
                               padding: 4px 8px;
                               border-radius: 4px;
                               color: white;
                               font-weight: bold;
                               margin-right: 10px;
                           }
                           
                           .get { background-color: #27ae60; }
                           .post { background-color: #e67e22; }
                           .put { background-color: #3498db; }
                           .delete { background-color: #e74c3c; }
                           
                           .url {
                               color: #34495e;
                               font-family: monospace;
                               font-size: 1.1em;
                           }
                           
                           .description {
                               margin-top: 10px;
                               color: #555;
                           }
                       </style>
                   </head>
                   <body>
                       <div class="container">
                           <div class="header">
                               <h1>热更新配置管理</h1>
                           </div>
                           
                           <div class="config-grid">
                               <div class="config-card">
                                   <h3>基础配置</h3>
                                   <div class="form-group">
                                       <label>
                                           <input type="checkbox" id="IsBypassGameUpdate_Bedrock"> 
                                           跳过基岩版游戏更新 (IsBypassGameUpdate_Bedrock)
                                       </label>
                                   </div>
                                   
                                   <div class="form-group">
                                       <label>
                                           <input type="checkbox" id="IsEnableX64mc"> 
                                           启用64位MC (IsEnableX64mc)
                                       </label>
                                   </div>
                                   
                                   <div class="form-group">
                                       <label>
                                           <input type="checkbox" id="IsStartWebSocket"> 
                                           启动WebSocket (IsStartWebSocket)
                                       </label>
                                   </div>
                                   
                                   <div class="form-group">
                                       <label>
                                           <input type="checkbox" id="IsDebug"> 
                                           调试模式 (IsDebug)
                                       </label>
                                   </div>
                               </div>
                               
                               <div class="config-card">
                                   <h3>模组配置</h3>
                                   <div class="form-group">
                                       <label>
                                           <input type="checkbox" id="EnableModsInject"> 
                                           启用模组注入 (EnableModsInject)
                                       </label>
                                   </div>
                                   
                                   <div class="form-group">
                                       <label>
                                           <input type="checkbox" id="IsDecryptMod"> 
                                           解密模组 (IsDecryptMod)
                                       </label>
                                   </div>
                               </div>
                               
                               <div class="config-card">
                                   <h3>房间配置</h3>
                                   <div class="form-group">
                                       <label>
                                           <input type="checkbox" id="EnableRoomBlacklist"> 
                                           启用房间黑名单 (EnableRoomBlacklist)
                                       </label>
                                   </div>
                                   
                                   <div class="form-group">
                                       <label>
                                           <input type="checkbox" id="EnableRegexBlacklist"> 
                                           启用正则表达式黑名单 (EnableRegexBlacklist)
                                       </label>
                                   </div>
                                   
                                   <div class="form-group">
                                       <label for="MaxRoomCount">最大房间数 (MaxRoomCount)</label>
                                       <input type="number" id="MaxRoomCount" min="0">
                                   </div>
                               </div>
                               
                               <div class="config-card">
                                   <h3>网络配置</h3>
                                   <div class="form-group">
                                       <label for="WebSocketPort">WebSocket端口 (WebSocketPort)</label>
                                       <input type="number" id="WebSocketPort" min="1" max="65535">
                                   </div>
                                   
                                   <div class="form-group">
                                       <label for="HttpPort">HTTP端口 (HttpPort)</label>
                                       <input type="number" id="HttpPort" min="1" max="65535">
                                   </div>
                                   
                                   <div class="form-group">
                                       <label for="NeteaseUpdateDomainhttp">网易更新域 (NeteaseUpdateDomainhttp)</label>
                                       <input type="text" id="NeteaseUpdateDomainhttp">
                                   </div>
                               </div>
                               
                               <div class="config-card">
                                   <h3>世界配置</h3>
                                   <div class="form-group">
                                       <label>
                                           <input type="checkbox" id="AlwaysSaveWorld"> 
                                           总是保存世界 (AlwaysSaveWorld)
                                       </label>
                                   </div>
                               </div>
                           </div>
                           
                           <div class="actions">
                               <button class="btn" onclick="loadConfig()">加载当前配置</button>
                               <button class="btn btn-success" onclick="saveConfig()">保存配置</button>
                               <button class="btn btn-danger" onclick="resetConfig()">重置为默认配置</button>
                           </div>
                           
                           <div class="api-doc">
                               <h2>API接口文档</h2>
                               
                               <div class="endpoint">
                                   <div class="description">获取当前配置</div>
                                   <div>
                                       <span class="method get">GET</span>
                                       <span class="url">/config/get</span>
                                   </div>
                               </div>
                               
                               <div class="endpoint">
                                   <div class="description">更新配置</div>
                                   <div>
                                       <span class="method post">POST</span>
                                       <span class="url">/config/apply</span>
                                   </div>
                                   <div class="description">
                                       请求体格式: JSON对象，包含需要更新的配置项
                                   </div>
                               </div>
                               
                               <div class="endpoint">
                                   <div class="description">获取设置列表</div>
                                   <div>
                                       <span class="method get">GET</span>
                                       <span class="url">/config/settingslist</span>
                                   </div>
                               </div>
                               
                               <div class="endpoint">
                                   <div class="description">设置主界面</div>
                                   <div>
                                       <span class="method get">GET</span>
                                       <span class="url">/settings</span>
                                   </div>
                               </div>
                           </div>
                       </div>
                       
                       <div id="notification" class="notification"></div>
                       
                       <script>
                           // 显示通知
                           function showNotification(message, type) {
                               const notification = document.getElementById('notification');
                               notification.textContent = message;
                               notification.className = 'notification ' + type;
                               notification.style.display = 'block';
                               
                               setTimeout(() => {
                                   notification.style.display = 'none';
                               }, 3000);
                           }
                           
                           // 加载配置
                           function loadConfig() {
                               fetch('/config/get')
                                   .then(response => response.json())
                                   .then(data => {
                                       if (data.error === 0) {
                                           const config = data.data;
                                           document.getElementById('IsBypassGameUpdate_Bedrock').checked = config.IsBypassGameUpdate_Bedrock || false;
                                           document.getElementById('IsEnableX64mc').checked = config.IsEnableX64mc || false;
                                           document.getElementById('IsStartWebSocket').checked = config.IsStartWebSocket || false;
                                           document.getElementById('IsDebug').checked = config.IsDebug || false;
                                           document.getElementById('EnableModsInject').checked = config.EnableModsInject || false;
                                           document.getElementById('EnableRoomBlacklist').checked = config.EnableRoomBlacklist || false;
                                           document.getElementById('EnableRegexBlacklist').checked = config.EnableRegexBlacklist || false;
                                           document.getElementById('MaxRoomCount').value = config.MaxRoomCount || 0;
                                           document.getElementById('WebSocketPort').value = config.WebSocketPort || 4600;
                                           document.getElementById('HttpPort').value = config.HttpPort || 4601;
                                           document.getElementById('NeteaseUpdateDomainhttp').value = config.NeteaseUpdateDomainhttp || '';
                                           document.getElementById('IsDecryptMod').checked = config.IsDecryptMod !== false; // 默认为true
                                           document.getElementById('AlwaysSaveWorld').checked = config.AlwaysSaveWorld !== false; // 默认为true
                                           
                                           showNotification('配置加载成功', 'success');
                                       } else {
                                           showNotification('配置加载失败: ' + data.message, 'error');
                                       }
                                   })
                                   .catch(error => {
                                       console.error('Error:', error);
                                       showNotification('配置加载失败: ' + error.message, 'error');
                                   });
                           }
                           
                           // 保存配置
                           function saveConfig() {
                               const config = {
                                   IsBypassGameUpdate_Bedrock: document.getElementById('IsBypassGameUpdate_Bedrock').checked,
                                   IsEnableX64mc: document.getElementById('IsEnableX64mc').checked,
                                   IsStartWebSocket: document.getElementById('IsStartWebSocket').checked,
                                   IsDebug: document.getElementById('IsDebug').checked,
                                   EnableModsInject: document.getElementById('EnableModsInject').checked,
                                   EnableRoomBlacklist: document.getElementById('EnableRoomBlacklist').checked,
                                   EnableRegexBlacklist: document.getElementById('EnableRegexBlacklist').checked,
                                   MaxRoomCount: parseInt(document.getElementById('MaxRoomCount').value) || 0,
                                   WebSocketPort: parseInt(document.getElementById('WebSocketPort').value) || 4600,
                                   HttpPort: parseInt(document.getElementById('HttpPort').value) || 4601,
                                   NeteaseUpdateDomainhttp: document.getElementById('NeteaseUpdateDomainhttp').value,
                                   IsDecryptMod: document.getElementById('IsDecryptMod').checked,
                                   AlwaysSaveWorld: document.getElementById('AlwaysSaveWorld').checked
                               };
                               
                               fetch('/config/apply', {
                                   method: 'POST',
                                   headers: {
                                       'Content-Type': 'application/json',
                                   },
                                   body: JSON.stringify(config)
                               })
                               .then(response => response.json())
                               .then(data => {
                                   if (data.error === 0) {
                                       showNotification('配置保存成功', 'success');
                                   } else {
                                       showNotification('配置保存失败: ' + data.message, 'error');
                                   }
                               })
                               .catch(error => {
                                   console.error('Error:', error);
                                   showNotification('配置保存失败: ' + error.message, 'error');
                               });
                           }
                           
                           // 重置配置(通过发送默认配置实现)
                           function resetConfig() {
                               if (confirm('确定要重置为默认配置吗？此操作不可恢复。')) {
                                   const defaultConfig = {
                                       IsBypassGameUpdate_Bedrock: false,
                                       IsEnableX64mc: false,
                                       IsStartWebSocket: false,
                                       IsDebug: false,
                                       EnableModsInject: false,
                                       EnableRoomBlacklist: false,
                                       EnableRegexBlacklist: false,
                                       MaxRoomCount: 0,
                                       WebSocketPort: 4600,
                                       HttpPort: 4601,
                                       NeteaseUpdateDomainhttp: "https://x19.update.netease.com",
                                       IsDecryptMod: true,
                                       AlwaysSaveWorld: true
                                   };
                                   
                                   fetch('/config/apply', {
                                       method: 'POST',
                                       headers: {
                                           'Content-Type': 'application/json',
                                       },
                                       body: JSON.stringify(defaultConfig)
                                   })
                                   .then(response => response.json())
                                   .then(data => {
                                       if (data.error === 0) {
                                           loadConfig(); // 重新加载配置
                                           showNotification('配置已重置为默认值', 'success');
                                       } else {
                                           showNotification('配置重置失败: ' + data.message, 'error');
                                       }
                                   })
                                   .catch(error => {
                                       console.error('Error:', error);
                                       showNotification('配置重置失败: ' + error.message, 'error');
                                   });
                               }
                           }
                           
                           // 页面加载完成后自动加载配置
                           document.addEventListener('DOMContentLoaded', function() {
                               loadConfig();
                           });
                       </script>
                   </body>
                   </html>
                   """;
        }
    }
}