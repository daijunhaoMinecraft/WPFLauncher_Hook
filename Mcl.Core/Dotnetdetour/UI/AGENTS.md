# UI 维护说明

## 风格与资源

- 保留 WPF；WinUI 3 Style 由 `Themes/Fluent.xaml` 实现，不能直接使用 WinUI 的 XAML 类型。
- 每个窗口自己的 `Window.Resources` 合并 `/Mcl.Core;component/Dotnetdetour/UI/Themes/Fluent.xaml`。
- 代码创建的窗口/控件调用 `FluentTheme.Apply`。**不要把隐式样式合并到宿主 Application.Resources**。
- 统一字体、语义色、4px 控件圆角、8px 卡片圆角和 24px 页面边距；优先用资源键，不新建一套硬编码颜色。
- 按钮用默认、`PrimaryButton`、`DangerButton`；专属模板仅用于真正不同的交互。
- TextBox/PasswordBox 的 `PART_ContentHost` 不额外绑定 Padding 到 Margin，WPF 文本视图已经处理 Padding；否则固定高度的输入框会裁字。
- 保留控件名称、事件和绑定；移动 XAML 时同步代码文件、资源路径和源码验证程序。

## 页面组织

- `Controls/SettingsPanel.cs` 是启动设置窗口和宿主内嵌设置页的唯一配置编辑实现。
- 设置按分类导航，支持跨分类搜索，布尔值用 ToggleSwitch，说明与错误使用内联文本。
- 操作栏应固定在滚动区域之外；文本输入需关联 AutomationProperties.Name。
- `Accounts` 是历史 Forms 目录，原 `UI.Forms` 命名空间保留。
- `Integration` 是历史 Injector 目录，原 `UI.Injector` 命名空间保留。
- `Dialogs/BedrockPathSelectWindow` 与 `Windows/RoomInfoWindow` 保留历史 Features.GeneralHooks 命名空间以减少外部破坏。

## 日志窗口

- `BoundedLogBuffer` 分别限制待显示队列和历史缓存。
- UI 最多显示 50,000 字符；导出保留最近 2 Mi 字符，不声称是无限完整历史。
- 高速输出时提示被跳过的显示行，关闭窗口后不再接受数据。
- 不用逐行 Dispatcher.Invoke 或无限增长的 StringBuilder 做日志接收。

## Web 页面

- `WebAssets/RoomManage.html` 和 `Settings.html` 是唯一源文件，通过 EmbeddedResource 发布。
- `HtmlResource` 只负责读取缓存和端口/主题替换，不能再写大段 C# HTML 字符串。
- `Fluent.css` 与 WPF 主题使用一致语义色；本地设置页不依赖在线 CSS CDN。
- 配置值写入 DOM 使用 `.textContent` / `.value`，不能拼接到 innerHTML。

## 验证

- 运行根目录验证脚本，检查 `.artifacts/validation/renders` 的实际 WPF 渲染。
- 新窗口需要更新 `Verify-Source.ps1` 与烟雾测试的窗口计数。
- 独立 XAML 渲染不执行 code-behind 的登录/网络逻辑，不代表宿主集成已经验证。
