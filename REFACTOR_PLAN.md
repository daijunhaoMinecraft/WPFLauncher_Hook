# Mcl.Core 可维护性重构计划

更新日期：2026-09-06

## 边界与约束

- 业务目录移动仅限 `Mcl.Core/Tools` 和 `Mcl.Core/Dotnetdetour`，不迁移其他业务目录。
- 保留 .NET Framework 4.8、x86、WPF 和现有启动器 Hook 协议；WinUI 3 Style 指视觉风格，不是引入 Windows App SDK。
- 保留工作区已有改动，尤其是 GameProcessHooks、WpfConfig 和项目引用；外部调试器附加、进程隐藏和驱动控制代码不属于插件功能，必须移除。
- 老 JSON/API 配置键不变；新字段名称通过注册表映射。旧字段名提供已弃用的源码兼容属性，但不承诺已编译第三方二进制的字段 ABI 兼容。
- 全项目源码允许清理 dnSpy 元数据注释；构建产物、外部程序集和生成代码不清理。

## 执行清单

- [x] 审视目录、构建设置、配置、日志与 UI 调用；记录已有未提交改动。
- [x] 独立目录基线构建：0 错误、31 警告；保留当前源码快照与密码协议基线向量。
- [x] 清理已跟踪源码中的 dnSpy Token/RID/RVA/File Offset 注释。
- [x] 配置元数据与字段名分离；统一校验、原子保存、旧键兼容。
- [x] 创建共享 Fluent 主题，以及分类、搜索、卡片与固定操作栏的共享设置组件。
- [x] 整理目录（见下表），同步项目项、资源路径和维护文档。
- [x] 合并 Tools 中重复算法、编码、哈希逻辑；保留协议字节行为并加回归检查。
- [x] 统一启动器日志分级，消除绕过详细开关的诊断输出；保留警告/错误和独立的游戏日志开关。
- [x] 移除外部调试器、进程隐藏驱动集成、配置字段、调用点和项目源码。
- [x] 修复进程日志积压、无限历史缓存、窗口关闭后继续写入等生命周期问题。
- [x] 检查全部 MCL 窗口、Web 管理页的样式与布局，保留所有控件名称/绑定/事件。
- [x] 添加根目录和子目录 AGENTS.md、构建/验证脚本、重构说明与测试。
- [x] 完整构建、回归测试、WPF 资源/界面渲染与残留扫描；记录无法在独立环境验证的宿主行为。

## 目录调整清单

| 原位置 | 目标位置 | 原因 |
| --- | --- | --- |
| Tools 下密码算法文件 | Tools/Cryptography | 按职责组织，拆开一文件多个工具类 |
| Tools 下编码/哈希/字符串辅助类 | Tools/Encoding、Tools/Cryptography | 避免同一实现存在两份 |
| Tools/UidHelper.cs | Tools/Identifiers/UidHelper.cs | 独立标识符职责 |
| Dotnetdetour/Features/Optizime | Dotnetdetour/Features/Optimization | 修正目录拼写 |
| Dotnetdetour/Models/Entity | Dotnetdetour/Models/Entities | 合并重复实体目录 |
| Dotnetdetour/Models/Updater | Dotnetdetour/Services/Updates | 更新服务不是数据模型 |
| Dotnetdetour/UI/Forms | Dotnetdetour/UI/Accounts | 明确账号相关视图 |
| Dotnetdetour/UI/Injector | Dotnetdetour/UI/Integration | 明确宿主集成边界 |
| Features/GameTweaks/BedrockPathSelectWindow.* | UI/Dialogs | UI 不再混在游戏 Hook 内 |
| Features/GeneralHooks/RoomInfoWindow.cs | UI/Windows | 将窗口实现归还 UI 层 |

目录移动默认保留兼容命名空间，避免把结构整理扩大为外部 API 破坏；新增类遵循目标目录命名空间。

## 验证策略

1. 使用独立 `.artifacts` 输出，避免覆盖用户已有 `bin/obj` 产物。
2. 不加载生产 DLL 做单元测试：模块初始化器会安装 Hook、申请控制台和启动后台任务。独立验证程序排除该初始化器。
3. 覆盖旧配置加载、新旧键更新、非法类型/边界、批次不部分应用、持久化失败、未知键保留。
4. 覆盖日志关闭/开启/再次关闭、控制台与文件一致性、警告/错误保留、缓冲区边界与关闭生命周期。
5. 用重构前生成的固定向量验证加密、签名、动态令牌和 UID；不得以“优化”为由改变协议填充或整数宽度。
6. 验证所有 XAML 页面构建与资源引用，并渲染代表性设置/日志/对话框供人工复核。
7. 不启动真实账号登录、联网房间、游戏、驱动或注入流程；这些在交付中明确列为宿主手工回归项。

## 本轮完成记录

- 配置：36 个设置项使用统一注册表；46 个全局字段采用新名称，保留旧名称源码属性与旧 JSON/API 键。
- 目录：以上移动均已完成；兼容命名空间保持不变。
- UI：21 个 XAML 窗口接入同一 Fluent 主题，代码创建的设置/房间窗口也使用该主题；设置与日志等布局重新组织。
- 重复代码：两套设置编辑器合为一套；AES/编码/哈希合并；Skip32 双向变换合并；大型 HTML 字符串改为单一嵌入资源。
- 日志：关闭详细模式不输出 Debug/Trace/直接 Console 诊断；保留关键状态、警告和错误；游戏输出独立控制，关闭窗口释放接收缓冲。
- 源码扫描：184 个 C# 源文件检查通过，dnSpy 元数据注释残留为 0；直接控制台输出仅剩明确开关保护的游戏 stdout/stderr。
- 构建：完整主项目构建 0 错误、25 条已有警告（基线 31 条），未屏蔽警告；测试项目构建 0 错误、0 警告。
- 回归：125 项检查通过，包含旧协议固定向量、配置迁移/批次失败/文件占用、日志级别热切换、缓冲上限、UI 保存与内联错误、21 个 XAML 窗口的解析/渲染。
- Web：两个页面的 JavaScript 语法检查通过；设置页不再依赖远程 CSS CDN。未对真实服务端做浏览器端到端操作。
- Rider：已格式化关键新增/重构文件，关键文件错误级检查未报告问题。
- 可重复入口：`scripts/Validate-Refactor.ps1`；产物：`.artifacts/validation`。

### 必须在真实宿主手工回归的项目

独立测试没有启动网易启动器、真实账号登录、游戏、房间网络、驱动或注入流程。请在实际宿主验证：

1. 首次配置窗口、启动器内嵌配置页及小窗口/DPI 下的布局与键盘操作。
2. 详细日志关闭 → 开启 → 再关闭；文件输出切换；两种游戏输出开关组合。
3. Java/基岩进程启动、窗口关闭后游戏继续运行、结束进程与最后一批输出。
4. 账号/验证码、存档/备份、更新、端口转发与 Web 配置保存。
5. 旧版 `config.json` 原地加载，外部扩展重新编译后访问新字段或旧兼容属性。

本轮未重新设计 Hook 引擎或更改反作弊/驱动功能；其余原有复杂业务模块仍应分功能增加宿主回归后逐步拆分，而不是一次性重写。

## 后续日志补充

- 插件自身日志继续以英文为主，使用 `[MCL][Module]` 前缀、统一级别和重复事件限频。
- 游戏 stdout/stderr、远程公告、用户昵称、房间名和聊天内容允许保留中文；这些内容不强制转义为 Unicode。
- 外部调试器、进程隐藏和驱动控制实现已从插件中删除，不再作为可配置能力或调试入口。

## 2026-10-09：基岩缓存与配置 API 清理

本节是对前述历史完成记录的后续变更，替代“继续保留旧 C# 属性别名”的约定。

- 保留用户已有的 `SkipStartJavaFileFullCheck = false` 修改以及 IDE/obj 差异；未改动外部工具箱源文件。
- 删除 `WpfConfig.Legacy.cs` 的 46 个源码兼容属性；旧 JSON/Web 键不变。外部扩展需要迁移到新字段并重新编译。
- 新增 6 个注册配置项（2026-10-10 追加缓存自动化持续时间项，现总计 45 项）。基岩 EN 缓存清理、MCP 文件复制均默认关闭，缓存默认目录为 `%AppData%\MinecraftPC_Netease\packcache`。
- 文件自动操作仅绑定经过游戏进程 Hook 的精确 `Minecraft.Windows.exe`，等待启动、跟踪退出、合作取消；拒绝重叠路径、路径逃逸及重解析点。无效缓存配置加载时关闭两个开关，避免在回退路径上执行删除。
- 共享设置页增加目录/文件对话框与多行列表；基岩目录验证 `<选择的文件夹>\<客户端名称文件夹>\Minecraft.Windows.exe`。启动时客户端选择窗口优先使用已保存路径，写入统一走配置事务。
- 版本由 `WpfConfig.GetVersion()` 读取 DLL 信息版本；编译时基版本拼接固定 `-Fix` 后缀。输出 DLL 的 ProductVersion 已检查为 `7.0.0-DLL-Public-Fix`，FileVersion 为 `7.0.0.0`，未反射加载生产 DLL。
- 配置维护方式、外部 ConfigManager 的不足、行为边界详见 `Mcl.Core/Dotnetdetour/Models/Config/README.md`。

### 验证结果

- 已尝试 `scripts/Validate-Refactor.ps1`。源码扫描被 **HEAD 已有**的 `Mcl.Core/Network/NetClient.cs:240` 未过滤 `Console.WriteLine` 阻断。没有放宽扫描规则，也没有修改该无关业务文件；不能将一键验证描述为通过。
- 随后使用脚本同样的隔离输出参数直接构建两个项目：主项目 0 错误、24 条警告；测试项目 0 错误、3 条警告（来自源码链接的既有 RegistryHelper 未使用变量）。没有用测试桩替换主项目宿主依赖。
- 源码链接烟雾测试 **184 项通过**，包括缓存默认关闭、路径与批次验证、无效加载关闭功能、复制失败重试、递归反馈保护、实际目录链接拒绝、可控进程状态的等待/退出/取消、目录选择草稿/取消/错误、旧 JSON 键和 DLL 版本元数据。
- 21 个 XAML 窗口解析/渲染，另渲染并检查基岩设置、缓存开关和 660×520 下的 MCP 来源列表/操作栏。Windows 系统文件对话框未做自动化交互；使用同一路径回调验证选择后的应用行为。
- 两个 Web 页面 JavaScript 语法检查通过；没有进行浏览器与真实 Web 服务的端到端测试。
- 日志和渲染位于 `.artifacts/validation`。未启动真实启动器登录、游戏、联网房间、驱动或内存注入；真实宿主进程时序及实际缓存操作仍需在备份数据后手工验证。

## 2026-10-09：模组目录弹窗修复与 Rider 调试启动流

- 模组目录：从 `ApplyRuntimeSettings` 移除自动打开 Explorer，共享设置面板增加手动按钮，统一目录准备和 `.jar` 扫描；不改变复制命名规则。此前源码链接验证 210 项通过，主项目 0 错误/24 警告，测试项目 0 错误/3 警告，已检查 860×680 与 660×520 下的新按钮。
- 本地调试：新增 `.run/Prepare MCLauncherDebug.run.xml`、`.run/WPFLauncher Debug.run.xml` 和 `scripts/Prepare-LauncherDebug.ps1`。保留旧 Shell 启动配置，不再将它当作托管调试入口。
- 修正准备配置的解释器为系统 PowerShell 绝对路径；核对当前 Rider 实际注册类型为 `RunExe`。新配置启动前构建 Debug/x86（关闭优化、portable PDB），只部署 DLL/PDB 到仓库内的 `MCLauncherDebug` 并校验哈希。
- 主项目 `LauncherAssemblyPath` 参数现在实际控制宿主引用；普通构建默认仍为此前实际使用的 dump.exe，调试构建明确引用用户提供的本地无壳 WPFLauncher.exe 和依赖。
- 调试准备已由 Rider 运行成功，0 错误、24 警告，DLL/PDB 哈希匹配，宿主 EXE 未改动。Rider 已识别配置并创建 32 位托管调试会话；取栈前会话已结束，没有取得完整的自动化断点/单步验证证据。
- 没有修改生产 Hook 去附加调试器，也没有自动运行真实登录、联网房间或游戏。`MCLauncherDebug` 已整体忽略，账号、第三方二进制不加入提交。详细用法见 `DEBUGGING.md`。
- 一键重构验证仍有此前记录的 `NetClient.cs:240` Console 源码扫描问题；本次没有放宽检查。

## 2026-10-09：Rider 重定向控制台句柄修复

- 原因：模块初始化无条件设置 `Console.CursorVisible`，在 Rider 非屏幕缓冲区输出句柄下抛出 IOException，并使 `<Module>` 与宿主 `tb` 类型初始化失败。
- 修复：提取无配置/日志依赖的 `ConsoleEnvironment`，识别已有标准输出/错误句柄并保留宿主 writer/编码；仅缺少输出目标时尝试分配控制台。移除初始化中的光标设置、`Console.SetOut` 和重复输出流创建。可选控制台失败不阻断配置加载和 Hook 安装，不吞掉整个初始化流程的异常。
- 共享输出：重定向时不读写颜色；颜色能力、读取、设置、恢复失败时尽量保留纯文本输出。NLog 根据原生控制台能力选择彩色或普通 ConsoleTarget。
- 验证：完整 Debug/x86 构建 0 错误/24 警告，测试构建 0 错误/3 警告，源码链接测试 232 项通过（新增 22 项）。测试不加载生产模块初始化器，不实际创建控制台。Rider 错误级检查未报告问题。
- 已在旧调试会话结束后运行 Prepare MCLauncherDebug，部署 DLL/PDB 并校验哈希；此增量准备构建 0 错误。没有自动重新启动真实宿主登录/游戏，真实宿主复测由用户重启 Debug 完成。
- 一键验证入口仍被已有 NetClient.cs:240 未过滤 Console 输出阻断，未放宽规则。CLR 类型初始化失败在当前进程中会缓存，必须停止旧进程后重新调试，不能仅继续执行或热重载。

## 2026-10-10：调试器入口跳板复现与修复

- 确认部署 DLL 已含前次去重/UTF-8 修改，不能把持续 AV 归因于未更新 DLL。当前证据来自独立 net48/x86 探针，未执行真实登录或游戏。
- 用生产 NativeDetourFor32Bit 源码链接到独立探针：旧实现直接运行返回 14；Rider 下入口是 E9 rel32，原方法 trampoline 搬移后错误地从应到达的 0x02970C98 跳至 0x00DA1008，无法正常返回。
- 修复 E9 调试入口的原方法回跳，直接保留原 JIT 函数体地址。相同探针在直接运行与 Rider Debug 下均返回 14，Rider 局部变量已确认；实际原方法地址等于跳板原始目标。证据保存在 .artifacts/detour-probe/before-*.txt、after-*.txt。
- 前次中断的 HookPatchRegistry 现已完成真实安装协调流程回归：记录型 IDetour 覆盖分批/全量安装、重入、并发和失败后禁止盲目重试，没有让烟雾测试写原生代码。
- 调试配置显式设置 MCL_CONSOLE_UTF8=1，ConsoleEnvironment 与 NLog target 使用 UTF-8；准备脚本同步编码。Web 控制台入口在调试模式下不再改回 GBK，也不关闭调试器输出。普通宿主显式显示/隐藏控制台流程保留。
- 中文验证不仅是编码函数测试：真实子进程输出字节符合 UTF-8，模拟宿主重设 GBK 后的 NLog 输出仍可正确解码；Rider Debug 探针中实际显示「服务器已启动：中文输出验证」。
- 源码链接烟雾测试 254 项通过；DetourProbe 构建 0 错误/0 警告，Run 退出 0，Debug 在成功断点验证后继续正常退出。主插件已通过准备步骤构建并部署 DLL/PDB，哈希一致。
- 一键 Validate-Refactor.ps1 仍被既有 NetClient.cs:240 的 Console 输出扫描阻断，未放宽规则。此结论不扩大为 x64、任意指令重定位、Hot Reload 或整个宿主业务流程都已验证；真实启动器须按用户原场景重新 Debug。

## 2026-10-10：基岩缓存扫描空闲退避与持续时间

- 问题：`BedrockCacheService.RunSessionAsync` 以固定 800ms 轮询 `RunPass`，游戏早期加载结束后缓存不再重建，之后每轮都是纯递归遍历开销，MCP 目标查找同样每轮全树 `Walk`。
- 空闲退避：`BedrockCacheWorker.RunPass` 改为返回本轮是否发生删除/复制；会话循环在有改动时保持 800ms，连续空转则以 2 倍步进延长扫描间隔，上限 30s，一旦有活动立即回落。不改变“每个 MCP 目标成功复制一次、失败重试”的契约。
- 持续时间：新增注册配置 `BedrockCacheDurationSeconds`（字段 `WpfConfig.BedrockCacheDurationSeconds`，0–86400，默认 0=持续到游戏退出），从检测到游戏进程运行起计时，超过后停止本会话的清理与复制扫描。字段计入 `BedrockCacheOptions.IsCacheField`，随批量校验/无效加载关闭逻辑一并处理。
- 测试更新：注册表计数 44→45（`Verify-Source.ps1` 同步）；`RunPass` 活动/空闲返回值断言；持续时间上限（进程存活时到点停止）回归。
- 验证结果：`Verify-Source.ps1` PASS（195 个源文件、21 个窗口共享主题）；测试项目构建 0 错误/3 警告（既有 RegistryHelper 未使用变量）；源码链接烟雾测试 256 项通过（较前 254 项新增 2 项）。
- **已知既存问题（非本次引入）**：当前工作区 `WpfConfig.BedrockPackcacheDirectory` 默认值为 `MinecraftPE_Netease`，而 `BedrockTests.cs:32` 与上文 `%AppData%\MinecraftPC_Netease\packcache` 断言为 `MinecraftPC_Netease`。该不一致会使烟雾测试在到达基岩用例前失败；本次仅在临时对齐 PC 后取得上述完整通过结果，临时改动已还原為 PE。需要确认发布默认值到底应为 PE 还是 PC 并统一 `WpfConfig`、`ConfigManager` helpText、测试与文档。
- 未验证：真实游戏进程时序与实际缓存读写仍需在备份数据后手工确认。

## 2026-10-10：手动清空 packcache（修复损坏资源包）

- 需求：设置页提供一次性“清空 packcache 目录”动作，删除缓存内容以强制游戏重建资源，修复损坏的资源包。
- 交互：仅 WPF 共享设置面板（`SettingsPanel`）。在 `BedrockPackcacheDirectory` 卡片操作栏新增“清空目录（修复资源包）”按钮，点击后弹出确认框（可注入以测试），确认后立即清空并在状态栏报告结果；不写入 `config.json`，Web 设置页不涉及。
- 实现：`BedrockCacheWorker.ClearContents(root, report)` 清空根目录内容并保留 packcache 文件夹本身，复用 `EnsureSafePath` 的路径逃逸与重解析点校验，逐项失败继续并回报（区别于自动清理的遇错中止）。`BedrockCacheService.ClearPackcache(directory, failures)` 复用 `BedrockCacheOptions.ValidateEntry` 强制目录名必须为 packcache。目录不存在时 UI 明确提示，不删除任何内容。
- 验证：`Verify-Source.ps1` PASS（195 源文件、21 窗口）；主项目 0 错误/24 警告；测试项目 0 错误/3 警告；源码链接烟雾测试 263 项通过（较前 256 项新增 7 项：服务级清空/保留根/拒绝非 packcache/缺失目录，UI 按钮清空/成功提示/缺失提示/取消不删除）。
- 与前节相同的既存问题：`WpfConfig.BedrockPackcacheDirectory` 默认值为 `PE` 而测试/文档断言为 `PC`，完整通过需临时对齐；真实宿主内嵌设置页的动作按钮行为与真实缓存读写仍未验证。
