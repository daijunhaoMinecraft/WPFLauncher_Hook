# Dotnetdetour 维护说明

## 职责分层

- `CoreEngine`：Hook 发现、方法替换和原生 detour。保留目标签名、特性和调用约定。
- `Features`：按功能组织宿主适配；不要在这里添加新的窗口实现。
- `Models/Config`：配置注册、转换、校验、迁移和持久化。
- `Models/Entities`：协议与界面数据；历史 `Models.Entity` 命名空间暂时保留。
- `Services/Updates`：更新服务与 Markdown 渲染；历史 `Mcl.Core.Updater` 命名空间保留。
- `UI`：所有 MCL 自有窗口、宿主设置页集成和嵌入 Web 页面。
- `Utilities/Diagnostics`：启动器日志策略、线程安全的游戏日志缓冲。
- `Utilities/Crypto` 中的旧 AES API 是 Tools 的兼容转发层，不得复制算法回来。

## 配置契约

1. 在 `WpfConfig` 中定义意义清晰的 PascalCase 字段。
2. 在 `ConfigManager.Registry` 中创建 `ConfigEntry`，用 `nameof(WpfConfig.Xxx)` 指定字段。
3. `ConfigEntry.Key` 是旧 JSON/Web API 契约；重命名字段时不要重命名这个键。
4. UI 和 Web 必须通过 `ConfigManager.Update`/`UpdateFromJson` 写入，不能复制 `GetField + Convert.ChangeType`。
5. 所有输入先验证，再原子替换文件，最后更新运行时字段；保存失败不能发布半个批次。
6. 加载旧文件时，非法单项保留当前值并告警；未知键保留，错误 JSON 不自动覆盖。
7. `WpfConfig.Legacy.cs` 保留旧名称的源码兼容属性；这不兼容第三方已编译的旧字段访问 IL，外部扩展需要重新编译。
8. 不要在配置字段初始化中创建 `Window` 等 DispatcherObject；配置可能首先从后台线程读取。

## 日志约定

- 启动器诊断使用 `LauncherLogging.Debug` 或 NLog `Debug`；请求体、数据包和内部状态不能标为 Info。
- Info 仅用于关键状态，Warn/Error 表示用户可行动的问题；关闭详细日志不应该吞掉真正错误。
- 昂贵的消息格式化优先用 `LauncherLogging.Debug(() => ...)`。
- `EnableVerboseLogging` 控制启动器详细级别；`WriteLauncherLogsToFile` 控制文件目标；二者通过配置入口保存后立即应用。
- 游戏 stdout/stderr 是独立通道，由 `ShowGameLogsInConsole` / `ShowGameLogsWindow` 控制。启动后关闭开关会停止显示，启用捕获/创建窗口可能需要下次启动。
- 不要全局替换或过滤 `Console.Out` 以“修复日志”，否则会误伤宿主与游戏输出。
- 启动器唯一允许的直接 Console 输出是游戏 Hook 中显式开关保护的两处输出。
- 详细日志不意味着可以输出密码、Cookie、Sauth 等凭据；敏感输出必须受独立的明确选择控制。

## 生命周期

- 设置页创建和窗口操作在 UI Dispatcher 上进行；宿主集成轮询不得同步阻塞后台线程或重复注册。
- 进程日志窗口关闭时停止计时器、清空缓冲、断开对窗口的持有；退出事件后仍可能有最后一批 stdout 回调。
- 不在没有专门回归的情况下改动反作弊、身份适配、网络封包或原生内存逻辑；插件不包含外部调试器、进程隐藏或驱动控制集成。
