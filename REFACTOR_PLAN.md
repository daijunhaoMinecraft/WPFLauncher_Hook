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

## 2026-10-12：联机大厅房间密码修改与房间管理页面

- 需求：`/online-lobby-room/update` 的 request json 只要带上 `password` 字段即可修改房间密码，但宿主请求类型 `ResetRoomSettingRequest` 没有该字段，宿主界面也没有入口，只能靠 detour 补字段。
- Detour：新增 `Features/NetworkAndRoom/RoomSettingsUpdater.cs`，hook `WPFLauncher.Network.Protocol.LobbyGame.age::b`（`/online-lobby-room/update` 的唯一发送点，宿主 `set_AllowSave`/`set_VisibleScope`/转移房主都会走到这里）。插件自行序列化 `{visibility, allow_save, owner_id, room_id, password}`，仍通过宿主的 `aet.g<EntityResponse<ResetRoomSettingRequest>>` 发出，因此 user-id/user-token 头、动态令牌、加密选项和回调派发线程与宿主完全一致；`age.a`（创建房间）本来就带 password，未改动。
- 密码生命周期：界面显式修改时用一次性 ticket（`[ThreadStatic]`）让紧随其后的那次宿主请求消费，只有服务端返回 `code == 0` 才写入 `WpfConfig.Password` 与 `RoomInfo.entity.password`；没有待处理修改时，若当前房间有密码（创建/加入房间时记录），宿主自身的可见性/允许存档更新和转移房主请求也会带上该密码，避免整包更新丢掉密码；房间本来没有密码时保持宿主原始请求，不产生多余的 `password` 字段。
- 回退：detour 未安装（宿主版本变化等）或调用失败时，改用 `X19Http.Post("/online-lobby-room/update")` 直连发送同一份请求体，并在日志中标注回退；两条路径都报告同一个 `RoomUpdateResult`。
- UI：新增 `UI/Windows/RoomManagementWindow.xaml`（第 22 个共享 Fluent/WinUI3 风格窗口），房间信息窗口标题栏新增「房间管理」按钮。页面包含房间状态、密码保护（保存/取消密码保护、明文切换、1-32 字符校验）、可见性与允许存档、玩家列表（踢出/设为房主/房间黑名单，仅房主可操作）以及房间动态；非房主为只读视图，未加入房间时提示且禁用编辑。
- 兼容：未新增配置项（注册表仍 45 项），未改动房间更新推送（`LobbyGameRoomManager` commandId=4）、Web/HTTP 房间端点与 WebSocket 契约；`RoomManage.html` 本次不提供密码入口。
- 验证：`Verify-Source.ps1` PASS（198 个源文件、22 个窗口共享主题）；主项目 x86 Debug 构建 0 错误/24 警告；测试项目 0 错误/3 警告；源码链接烟雾测试 264 项通过（较前 263 项新增 1 项窗口渲染）；已渲染 `.artifacts/validation/renders/RoomManagementWindow.png` 人工复核布局。未改变 Hook 引擎、原生 detour 或宿主启动流程，未新增配置项。
- 与前节相同的既存问题：`WpfConfig.BedrockPackcacheDirectory` 默认值仍为 `PE` 而测试断言为 `PC`，完整跑通烟雾测试同样是在临时对齐 PC 后取得，临时改动已还原。
- 未验证：真实宿主的登录、建房/进房，服务端是否接受 `update` 请求中的 `password` 字段，非房主的服务端拒绝行为，以及直连回退路径；这些需要在真实账号与房间中手工回归。

## 2026-10-12：用 MCL 房间管理窗口替换启动器房间窗口

- 需求：把启动器自带（WPFLauncher.exe 内）的联机大厅房间窗口替换成 MCL 房间管理窗口，功能不能缺项，并且可以在设置中关闭替换；同时补充踢出、加好友、拉黑等玩家操作。
- 替换：新增 `Features/NetworkAndRoom/RoomWindowReplacement.cs`，hook `WPFLauncher.Common.BaseWindow::d`（宿主所有窗口的 Show，`hook(window)` 形参承接 this），只拦截 `LobbyGameRoomManagerView`：启用替换时不再显示宿主房间窗口，改为显示 `RoomManagementWindow`，其余窗口原样转发。宿主房间窗口仍然会被创建（`jp` ViewModel 是房间状态与全部房间命令的载体，`azf<apn>.Instance.k<LobbyGameRoomManagerView>()` 仍能取到）；`jp`/`jq` 是 internal 类型，按 `ExitPop`、`HttpHelper` 既有做法通过反射访问。
- 功能对齐：新增 `Features/NetworkAndRoom/RoomOperations.cs`，通过宿主房间 ViewModel 的公开命令 `GameOperateCommand`/`InviteCommand`/`BackupSaveCommand`/`ResetSaveCommand`/`ReportCommand`/`CloseCommand`/`KickOutPlayerCommand`/`AddFriendCommand` 实现开始/进入游戏、邀请好友、备份存档、重置存档、举报房间、退出房间、踢出玩家、加好友（玩家命令按 UID 在 `Players` 中定位后执行），因此确认框、失败提示以及插件已有 Hook（`ExitPop` 退出确认、`LobbyGameSavesManager` 备份界面、`GetRoomIp`、密码修复）全部沿用。转移房主、可见性、允许存档与密码仍走 `age.b` detour，房间黑名单沿用插件实现；页面新增「房间操作」卡片与玩家行的「加好友」按钮，并按宿主 `LobbyGameState` 切换「开始游戏/进入游戏」。
- 生命周期：`WpfConfig.UseMclRoomManagementWindow=false` 时完全不拦截，恢复启动器房间窗口；替换过程抛异常会记录日志并立即回退到宿主 Show；宿主房间窗口关闭（退出房间/被踢）或 `age.Left` 时关闭 MCL 房间管理窗口；替换启用时 `RoomManager` 不再重复弹出房间信息窗口。独立 WPF 探针确认：从未显示的窗口 `Activate()` 返回 false、`Close()` 正常，因此隐藏宿主房间窗口不会破坏宿主窗口状态机。
- 配置：新增注册项 `UseMclRoomManagementWindow`（字段 `WpfConfig.UseMclRoomManagementWindow`，默认 true，分类「联机与房间」，含说明），注册表 45→46；`Verify-Source.ps1` 与烟雾测试同步。
- 验证：`scripts/Validate-Refactor.ps1` 全流程通过（`Verify-Source.ps1` PASS：200 源文件、22 窗口、46 注册项；主项目 x86 Debug 0 错误/24 警告；测试项目 0 错误/3 警告；源码链接烟雾测试 266 项通过，新增替换开关的关闭/恢复断言与「房间操作」卡片渲染）。渲染 `.artifacts/validation/renders/RoomManagementWindow.png` 已人工复核。
- 与前节相同的既存问题：`WpfConfig.BedrockPackcacheDirectory` 默认值仍为 `PE` 而测试断言为 `PC`，完整跑通烟雾测试同样是先临时对齐再还原。
- 未验证：真实启动器中替换后的完整流程（建房/进房、开始游戏、进入游戏、邀请、备份/重置存档、举报、退出、踢出、加好友）、宿主窗口被隐藏时的实际时序，以及服务端是否接受 `update` 请求中的 `password` 字段。

### 修正：房间在线后「开始游戏」按钮仍为灰色

- 原因：房间状态推送（`moduleId=40, commandId=1`）由插件 `ChatConnectionPacket.HandleGameStatus` 与宿主 `ahl.a` 各自处理，前者只更新 `WpfConfig.RoomInfo`，后者把 `akv.State` 改成 `SERVER_ON`；MCL 房间管理窗口的按钮状态原先只在打开窗口/手动刷新/自身操作后重算，没有任何事件驱动，因此一直停留在旧的 `SERVER_OFF/SERVER_OPENING`。
- 修正：`RoomManagementWindow` 订阅宿主房间对象（`NotifyObject` → `INotifyPropertyChanged`）的 `PropertyChanged`，状态变化立即重算按钮；插件自己的 `HandleGameStatus`/`HandleRoomInfoChange`/`HandlePlayerOperation` 与 `RoomManager` 成员列表 Hook 分别调用 `NotifyRoomStateChanged()`/`NotifyRoomPlayersChanged()`；窗口在打开期间还有一个 2 秒的 `DispatcherTimer` 周期性校正，防止宿主未通知或替换过房间对象。`GetHostGameState()` 在宿主状态不可用时回退到房间推送里的 `game_status`。
- 验证：主项目 0 错误/24 警告；`Validate-Refactor.ps1` 全流程通过（`Verify-Source.ps1` 200 源文件/22 窗口/46 注册项，主项目与测试项目均 0 错误，烟雾测试 266 项通过，渲染包含更新后的「房间操作」卡片）。真实宿主中需要在房间服务器就绪后确认按钮自动切换为「进入游戏」。

### 修正（第二轮反馈）：房间管理窗口细节

先用插件自身 HTTP API（`http://127.0.0.1:4600/get_roominfo_fast` 与 `POST /api/online-lobby-room/get`）确认了真实房间状态（密码 `123456`、`game_status=1`，服务器在该接口返回的 `fids` 为空），据此完成：

- 跨线程异常：`HandleRoomInfoChange`/`HandleGameStatus` 在后台线程调用 `RoomManagementWindow.Notify*` 时枚举了 `Application.Windows`，触发“调用线程无法访问此对象”。现在统一走 `RunOnWindows`：在 `Application.Dispatcher` 上用 `BeginInvoke` 完成枚举与刷新，异常只记 Debug。该异常此前会中断 `HandleRoomInfoChange`，也是“应用房间设置后控件回退成旧值/密码状态不同步”的直接原因。
- 应用设置后的回退：`RoomSettingsUpdater` 在服务端返回成功后先把 `visibility`/`allow_save` 写回本地 `RoomInfo.entity` 再回调界面；`刷新房间信息` 改为调用服务器端点 `/online-lobby-room/get`（body `{"room_id":...}`，与宿主 `age.c` 相同）读取真实状态，服务器返回空 `fids` 时保留本地成员列表。
- 滚轮：玩家列表、房间动态、可见性下拉框、密码框会吞掉滚轮。现在对这些控件挂 `PreviewMouseWheel`：内部还能滚动时保持原行为，否则把滚轮转发给页面 `ScrollViewer`。
- 窗口行为：移除房间管理窗口的「置顶窗口」复选框（不再强制 Topmost）；移除底部「关闭」按钮，右上角关闭等同于「退出房间」（`OnClosing` 取消关闭并执行宿主 `CloseCommand`，程序化关闭用 `closingWithoutLeave` 抑制）。
- 重复退出：`RoomManager.Left` 原本对同一条离开消息打印两次（日志 + 彩色控制台各一条），且宿主窗口 Closed 与插件窗口关闭存在两条路径；现在只保留一条英文 Debug 日志，窗口关闭统一走宿主命令，离开时 `CloseAll` 标记程序化关闭。
- 密码可见性：按需求在 Debug/Info 日志输出本次设置或修改后的密码（`Room update request sent with a new room password: <pwd>`、`Room password updated: <pwd>`），与既有建房/进房日志的密码输出一致；取消密码保护仍只记录状态。
- 淘汰旧窗口：删除 `UI/Windows/RoomInfoWindow.cs` 与注册配置 `ShowRoomManagerWindow`（`WpfConfig.ShowRoomDetailsWindow`），`RoomManager`/`ChatConnectionPacket` 中所有房间信息窗口的创建、更新、关闭调用一并移除，房间信息统一由 `RoomManagementWindow` 展示；注册配置回到 45 项。
- 玩家头像：玩家列表读取 `/user/query/search-by-ids` 的 `avatar_image_url` 并在行内显示 28×28 头像，缺失时使用默认头像。
- 验证：`Verify-Source.ps1` PASS（199 源文件、22 个 XAML 窗口、45 项注册配置）；主项目 0 错误/24 警告；测试项目 0 错误/3 警告；烟雾测试 266 项通过；渲染已复核（无置顶复选框、无底部关闭按钮）。仍需真实宿主回归：滚轮、关闭窗口即退出、应用设置后控件不回退、头像加载。

### 修正（第三轮反馈）：房间变更对比与关闭窗口异常

- 房间变更对比：`HandleRoomInfoChange` 原先在 `UpdateRoomInfo` 前用 Debug 级日志逐项比较，且上一轮为消除“控件回退”加入的乐观实体写入会把待比较的旧值提前覆盖，导致用户只看到“房间信息已更改:”与一条被重复抑制的分隔线（`suppressed=1`）。现在改为 `DescribeRoomInfoChanges` 先与本地实体逐项对比（房间ID/房主（含名称）/房间名/存档大小/密码保护/允许保存/可见性），输出 Warn 级 `房间信息已更改: <变化字段>` 加每行 `旧值 -> 新值`（标题带字段名，避免同文本被 `PluginLog` 重复抑制），再写入实体；同一批变化同时写入房间管理窗口的「房间动态」。旧的 `LogRoomInfoChanges`/`LogOwnerChange`/`LogVisibilityChange` 已删除。
- 设置控件回退：移除 `RoomSettingsUpdater` 的乐观实体写入，改为窗口提交成功后 `RefreshFromConfig(refreshSettings: false)` 保留用户刚提交的可见性/允许存档选择，等房间推送到达由 `NotifyRoomInfoChanged` 把控件对齐服务器状态。
- `CloseCommand` 抛 `InvalidOperationException`（“在 Window 关闭时…”）：用户点击右上角关闭时，插件仍在 `OnClosing`（窗口处于 closing 状态）里同步执行宿主 `CloseCommand`，宿主离开流程最终触发 `RoomManager.Left` → `CloseAll` → 对同一窗口再次 `Close()`，WPF 直接抛异常。现在 `OnClosing` 只取消关闭并用 `Dispatcher.BeginInvoke(DispatcherPriority.Background)` 把退出命令延后到 Closing 结束；`CloseAll`/`CloseForRoom` 增加 `closingWithoutLeave` 去重与 `InvalidOperationException` 兜底。
- 验证：`Verify-Source.ps1` PASS（199 源文件、22 个 XAML 窗口、45 项注册配置）；主项目 0 错误/24 警告；测试项目 0 错误/3 警告；烟雾测试 266 项通过。仍需真实宿主回归：房间设置变更时日志与「房间动态」能列出旧值→新值、点右上角关闭只弹一次退出确认且不再报错。

### 修正（第四轮反馈）：“应用房间设置”提示当前未加入房间

- 现象：点「重置存档」后再点「应用房间设置」提示“当前未加入联机大厅房间”。
- 证据：日志 18:50:53 `POST /online-lobby-room/get`（`age.c`）对**旧房间** `4691130555207178372` 返回“无法找到该房间”，随后 `[MCL][Network] [RoomInfo] 获取房间信息失败` 与 `RoomInfo或entity为空，无法更新游戏状态`；期间用户已退出旧房间并在 18:50:35 新建了房间。
- 根因：宿主把 `age.c` 复用于两条流程——加入房间（`aum.f`）与重置存档后的 30 秒房间状态轮询（`jp.j` → `aga.d` → `jp.y` 排定 `jp.z` → `age.c(该 VM 绑定的房间)`）。插件的 JoinRoom Hook 把每次调用都当作“加入房间”，失败时无条件 `WpfConfig.RoomInfo = null`（成功时也无条件覆盖），于是旧房间的迟滞轮询会清掉/覆盖当前房间状态。
- 修复：新增 `RoomManager.IsJoinFlow(roomId)` 区分三类调用：无本地房间 → 加入；房间号与当前一致 → 刷新（`ApplyRoomInfoRefresh` 只同步服务器属性、保留本地成员列表、不重复打印“加入房间”与重置加入时间）；房间号与当前不一致且宿主房间窗口仍存在 → 旧房间迟滞轮询，成功/失败都不改动本地房间状态（失败降为 Debug）。`ApplyRoomInfoRefresh` 只在房间号与当前一致时写回 `RoomInfo`。加入流程写入 `fids` 时增加去重。
- 验证：`Verify-Source.ps1` PASS（199 源文件、22 个 XAML 窗口、45 项注册配置）；主项目 0 错误/24 警告；测试项目 0 错误/3 警告；烟雾测试 266 项通过。仍需真实宿主回归：进房 → 重置存档 → 退出并新建房间 → 等 30 秒轮询后「应用房间设置」仍应正常生效，且不再出现旧房间的 ERROR 日志。

## 2026-10-12：云存档大小、房间分享与剪贴板邀请

- 云存档大小：`SaveManagerWindow`（载入存档）的每个槽位新增「存档大小」，`SaveItemViewModel.FormattedSize` 改为 KB/MB/GB 自适应（接口 `size` 为 KB，与备份窗口保持一致）。
- 房间分享：房间管理窗口新增「房间分享」卡片，三个按钮分别用于在默认浏览器打开 `https://neteasehelper.theconsole.top/rooms/<room_id>?edition=pc`、复制该链接、复制含密码的房间信息。复制内容是「中文房间介绍 + 末尾一行 `MCL-ROOM-INVITE:{json}`」：中文行包含 房间名/房间ID/房主/当前人数/密码/是否允许保存/可见性/游戏状态/浏览器查看链接，机器行包含 房间号/房名/密码/房主/人数/允许保存/可见性/游戏状态，供本插件识别；房主名称通过 `/user/query/search-by-uid` 补齐。
- 剪贴板邀请：新增 `Models/Entities/RoomInvite.cs`（纯文本模型：生成、解析、校验，已纳入烟雾测试）与 `Features/NetworkAndRoom/RoomInviteSharing.cs`（每 2 秒轮询剪贴板，识别到邀请且不是当前房间时弹窗询问，确认后转到后台线程加入）。加入流程 `RoomOperations.TryJoinRoom`：① 静默关闭宿主房间窗口（触发宿主 `jp.u`：停计时器、POST leave-room、清理大厅页面 VM）并等待 1 秒；② `age.f(roomId, password, true)` 发送带密码的加入请求；③ `age.c(roomId)` 拉取房间信息（同时由 JoinRoom Hook 更新本地房间状态）；④ 按宿主 `aum.f` 的步骤注册房间：`new akv(entity, null)` → `kg.c(akv)`/`PlayingLobbyGame` → `apn.d<LobbyGameRoomManagerView>(azc.o)` → 设置 VM 的 `SelectedGame` 并调用 `jp.a(akv)` → `view.d()`（由替换 Hook 打开 MCL 房间管理窗口）。`aqg`/`kg`/`jp` 均为 internal，按项目既有做法用反射访问。
- 配置：新增注册项 `EnableRoomInviteClipboard`（字段 `WpfConfig.EnableRoomInviteClipboard`，默认 true，分类「联机与房间」），注册表 45→46；`InitHook.ApplyRuntimeSettings` 启动监听（幂等，可在设置中关闭）。
- 验证：`Verify-Source.ps1` PASS（201 源文件、22 个 XAML 窗口、46 项注册配置）；主项目 0 错误/24 警告；测试项目 0 错误/3 警告；烟雾测试 273 项通过（新增邀请文本生成/解析/非法输入 5 项与剪贴板开关 2 项）。
- 未验证：真实宿主的“退房 → 加入 → 打开房间窗口”完整时序、剪贴板监听在真实 Windows 会话中的行为（占用/权限），以及 neteasehelper 页面在浏览器中的显示；邀请文本中的密码按需求为明文。

### 修正（第五轮反馈）：移除剪贴板邀请，只保留中文房间信息

- 按反馈删除剪贴板读取与一键加入：`RoomInviteSharing` 去掉监听计时器、提示框与加入调用，只保留 `BuildRoomText()`（中文房间信息）与 `BuildRoomPageLink()`；`RoomInvite` 删除 `Marker`/`Parse`/`ToShareText`，只保留 `ToHumanReadableText()`；`RoomOperations` 删除 `TryJoinRoom`、静默退房、宿主房间窗口注册与相关反射辅助；`InitHook` 不再启动剪贴板监听；配置项 `EnableRoomInviteClipboard` 一并移除（注册表回到 45 项）。
- 复制内容现在只有中文说明（房间名/房间ID/房主/当前人数/密码/是否允许保存/可见性/游戏状态/浏览器查看链接），不再包含 `MCL-ROOM-INVITE` 机器行；`复制房间链接` 与 `在浏览器查看房间` 保持不变。
- 验证：`Verify-Source.ps1` PASS（201 源文件、22 个 XAML 窗口、45 项注册配置）；主项目 0 错误/24 警告；测试项目 0 错误/3 警告；烟雾测试 269 项通过（房间信息文本断言改为校验中文字段与链接，并新增“不含机器标记”“房间ID为空不生成链接”两项）。

### 补充：房间状态可选中复制

- 「房间状态」卡片的 8 个值（房间号/房主/当前人数/最大人数/可见性/允许存档/密码保护/游戏状态/版本号/资源ID）由 `TextBlock` 改为扁平只读 `TextBox`（无边框、透明背景、`IsTabStop=false`），支持鼠标选中与 `Ctrl+C`；标签仍是普通文本，因此可以直接选中需要的字段值。
- 该卡片新增右键菜单：「复制房间状态」（房间名/房间ID/房主/人数/可见性/允许存档/密码保护/游戏状态/版本号/资源ID，**不含密码明文**）与「复制房间信息(含密码)」（与「房间分享」卡片同一份中文文本）。
- 命名修正：`room_name` 统一显示为「房间号」、`entity_id` 统一显示为「房间ID」。状态卡改为 6 行 × 2 列并新增「房间ID」行（此前「房间号」误显示 entity_id）；标题副标题、`复制房间状态`、中文分享文本与房间变更日志中的「房间名」字样一并改为「房间号」。
- 验证：`Verify-Source.ps1` PASS（201 源文件、22 个 XAML 窗口、45 项注册配置）；主项目 0 错误/24 警告；测试项目 0 错误/3 警告；烟雾测试 269 项通过，渲染已复核（状态卡片外观与之前一致，仅可选中）。

## 2026-10-12：房间黑名单管理页与「加入即踢出」

- 需求：拉黑玩家后立即踢出；房间管理窗口内可查看 UserID 黑名单与正则表达式（匹配玩家名称）名单，并支持手动添加/删除。
- 新增 `Features/NetworkAndRoom/RoomBlacklist.cs`：统一名单增删与落盘（`WpfConfig.WriteRoomBlacklist`/`WriteRegexBlacklist`，文件仍是启动器目录下的 `RoomConfig/BlackList.json`、`RoomConfig/RegexBlackList.json`）、正则合法性校验、踢人（优先走宿主 `KickOutPlayerCommand`，玩家已不在宿主列表时回退 `POST /online-lobby-member-kick`），以及「加入正则后踢出当前房间匹配玩家」：按房间 `fids` 拉取玩家信息、`Regex.IsMatch(name)` 命中则加入 UserID 名单并踢出，与 Web 端 `/Room/AddRegexBlacklist` 行为一致。
- UI：房间管理窗口新增「房间黑名单」卡片，UserID 名单与正则名单各有一组「输入框 + 加入并踢出 / 移除选中」和带逐条「删除」按钮的列表；未开启 `EnableRoomBlacklist` 时显示提示并禁用编辑；玩家行的「黑名单」按钮改为“加入后立即踢出”（已在名单中则移出），所有结果写入房间动态与底部状态栏。
- 验证：`Verify-Source.ps1` PASS（202 源文件、22 个 XAML 窗口、45 项注册配置）；主项目 0 错误/24 警告；测试项目 0 错误/3 警告；烟雾测试 269 项通过；渲染已复核（黑名单卡片的两份名单、按钮与列表）。
- 未验证：真实房间内的拉黑踢人（需要第二个账号）、正则命中玩家的实际踢出，以及名单文件在启动器目录的读写与重启后加载。

## 2026-10-12：被踢自动重连改为后台线程并去掉重复请求

- 现象：被房主踢出后宿主日志出现 `CheckMCLBlock!` 与 30 帧调用栈（栈顶为 `RoomManager.ReJoinRoom → Thread.Sleep`），启动器界面卡顿；同一秒打印两条“成功重新加入房间”，并反复收到 12022「快速匹配过于频繁」。
- 根因：宿主在 **UI 线程（Dispatcher）** 调用被踢处理器 `ahl.c`，插件的 `RoomManager.ReJoinRoom` hook 直接在该线程上执行阻塞 HTTP + `Thread.Sleep(800)` 重试循环；同时 `ChatConnectionPacket`（moduleId 40, commandId 3）在后台 Task 中又实现了一份相同的重连循环，两条循环并发请求触发服务端限流。
- 修复：`RoomManager.ReJoinRoom` 只负责置位与派发，重连循环移到 `Task.Run` 后台；重试固定 1 秒、最多 90 次，12022 只按第 1 次与每 10 次记录日志；仅在重连最终失败时回到 UI 线程调用宿主原处理器 `SendErrToWpf`（提示被踢 + 关闭房间窗口）；用 `Interlocked` 去重，重复踢出包不会启动第二条循环。`ChatConnectionPacket` 删除重复的重连循环，只保留日志与 WebSocket 通知。
- 验证：`Verify-Source.ps1` PASS（202 源文件、22 个 XAML 窗口、45 项注册配置）；主项目 0 错误/24 警告；测试项目 0 错误/3 警告；烟雾测试 269 项通过。
- 未验证：真实被踢场景的重连成功率、宿主看门狗不再触发、以及重连失败时宿主提示与房间窗口关闭的表现（需实机复测）。

## 2026-10-16：移植 MCLauncherUpdater 的启动器更新逻辑

- 需求：以 dnSpy 反编译的 `Updater/Manager/UpdateManager`（MCLauncherUpdater.exe）为参照，在插件内实现官方启动器更新；渠道改用 `Channel.ChannelGet()` 自动识别；解压时不得覆盖钩子自己的 `Mcl.Core.dll`；必须处理宿主运行中的文件占用。
- 新增 `Services/Updates/LauncherUpdateModels.cs`：`LauncherPatchInfo`、`LauncherChannelProfile`（netease→`/pl/x19_java_patchlist`、4399pc→`PC4399_`、a50_sdk_cn→`A50SdkCn_`，产品名沿用官方 GetChannelPrefix 规则）、`LauncherPatchList.Parse`（官方响应缺外花括号且带尾随逗号）、`LauncherUpdatePlan`（对齐 GetLastMajorMinorPatch：最新 Major.Minor 系列首版为整包步骤 + 列表末项为补丁步骤，只执行高于本机版本的步骤）。
- 新增 `Services/Updates/LauncherPatchDownloader.cs`：`FileDownload`/`DownloadManager` 的移植（清空目标、16KB 分块、Accept-Ranges 断点续传重试两次、磁盘空间检查、MD5 校验、失败清理与官方错误码文案）。
- 新增 `Services/Updates/SevenZipExtractor.cs`：等价于 `SevenZipHelper.UnZipSync`，调用安装目录 `ext\7z\7z.exe`（`x "包" -y -aoa -o"目标"`），额外支持 `-xr!` 排除项（跳过 `Mcl.Core.dll`、`Mcl.Core.pdb`，已验证 7z 排除不区分大小写且作用于任意层级）、30 分钟超时与取消时终止进程。
- 新增 `Services/Updates/DeferredPatchApplier.cs`：钩子运行在启动器进程内，无法照搬官方“先杀进程再覆盖”，因此生成 `apply-update.cmd`：等待宿主 PID 退出 → 备份钩子 DLL → 运行大版本安装器（`/silent /verisilent /norestart /nocancel`，官方参数）或 robocopy 小版本补丁（`/XF Mcl.Core.dll Mcl.Core.pdb`，不复刻官方“全量删除”语义）→ 恢复钩子 DLL → 重启原宿主可执行文件；日志写入会话目录 `apply-update.log`。脚本用系统 ANSI 编码写出，兼容中文用户目录；用 `ping` 代替 `timeout` 避免无控制台时失败。
- 重写 `Services/Updates/UpdateManager.cs`：保留管理员检查/提权、`MclUpdateConfig.json`（`DisableUpdate` 生效；`IsBuildChannel` 仅保留旧 JSON 键）、`DisableUpdater` 文件与 `Mcl.Core.dll.old` 清理；检查流程为 官方 patchlist → 本地 `WPFLauncher.exe` 文件版本（回退宿主 MainModule 版本）→ 计划 → GB2312 更新说明（2 秒超时）→ 更新窗口 → 下载/解压到 `%LocalAppData%\Netease\MCLauncherUpdater\patch\<时间戳-版本>` → 启动应用脚本并退出宿主；启动时清理 3 天前的旧会话。原 GitHub DLL 自更新（UpdatePromptWindow/DownloadProgressWindow/MarkdownWpfParser）被本次官方更新流程取代并删除。
- UI：新增 `UI/Windows/LauncherUpdateWindow.xaml(.cs)`（WinUI 3 风格自绘标题栏 + 版本对比卡片 + 更新说明 + 下载进度/阶段指示 + 完成/失败状态；支持取消），`UpdateConfigWindow` 去掉手工渠道下拉，改为显示自动识别的渠道；`SettingsPanel` 更新分区去掉“开发版更新”。XAML 窗口数 22 → 21，已同步 `Verify-Source.ps1` 与烟雾测试计数。
- 验证：`Verify-Source.ps1` PASS（21 个 XAML 窗口、45 项注册配置）；主项目 0 错误/24 警告；测试项目 0 错误/3 警告；烟雾测试离线 307 项通过，设置 `MCL_LIVE_UPDATE_CHECK=1` 后 314 项通过（真实 patchlist 解析、真实下载 + MD5、真实 7z 解压并确认不含 Mcl.Core.dll）；已渲染 update-prompt/working/ready/error 四种状态供复核。
- 顺带修复 HEAD 中已存在的测试不一致：`BedrockTests` 期望的 packcache 默认目录（`MinecraftPC_Netease` → 与源码/帮助文本一致的 `MinecraftPE_Netease`）与版本号（`7.0.0-DLL-Public-Fix` → 当前 `7.0.1-DLL-Public-Beta1`）；两者都只改测试期望，不改变运行行为。
- 未验证：真实宿主内的完整更新（下载→退出→覆盖→重启）未在真实账号/启动器会话执行；“稍后”与取消的文件清理、4399/a50 渠道的真实 patchlist、非管理员环境下提权交互、杀毒软件拦截覆盖时的日志表现，需要实机回归。

### 2026-10-16（第二轮）：接管宿主更新入口

- `Features/AntiCheatBypass/BypassUpdate.cs`：钩子由“弹窗询问是否更新”改为**无条件 `return false`**，彻底阻止宿主（网易启动器）自带更新，避免官方更新用原版 `Mcl.Core.dll` 覆盖钩子；删除 `uz.q` 询问框、更新日志抓取与 `xw` 调用（这些职责已由插件更新窗口承担），保留 `[OriginalMethod]` 占位方法与 `WPFLauncher.Update.xw/h` Hook 契约不变；首次阻止时输出一条 Info 日志。
- `Initialize.cs`：新增“启动器更新”段落 —— 先 `MethodHook.InstallTypes(new[] { typeof(BypassUpdate) })`（早于 `InitMpay` 的完整 `Install()`，保证宿主启动早期的更新检查也被阻止），再调用 `UpdateManager.InitializeAsync()` 由插件接管更新；注释明确三种禁用方式（设置页“禁用自动更新检查”、`MclUpdateConfig.json` 的 `DisableUpdate=true`、`DisableUpdater` 文件），禁用后宿主自带更新同样保持阻止。
- `SettingsPanel` 更新分区说明改为“禁用后宿主自带更新也会被阻止”，与上述语义一致。
- 验证：`Verify-Source.ps1` PASS（204 源文件、21 窗口、45 配置）；主项目 0 错误/24 警告；测试项目 0 错误/3 警告；烟雾测试 311 项通过（新增 4 项：宿主更新默认阻止、重复调用稳定返回 false、Hook 目标契约 `WPFLauncher.Update.xw/h`、原方法占位不再启用更新）。
- 未验证：真实宿主中 `xw.h` 的调用时机/频率与阻止后的界面表现（启动器自带的“检查更新”菜单、定时更新提示），以及“禁用更新”与官方 `Updater/MCLauncherUpdater.exe` 手动执行并存的场景，需要实机回归。

### 2026-10-16（第三轮）：Mcl.Core.dll 自更新与启动器更新分离

- 需求：恢复插件自身（Mcl.Core.dll，GitHub）更新，并与启动器（官方 patchlist）更新完全解耦，可分别开关。
- 新增 `Services/Updates/HookUpdateManager.cs`：GitHub 自更新轨道 —— Build 渠道（contents API sha 对比本地 git blob SHA1，提交信息作更新说明）与 Release 渠道（最新发行版 `Mcl.Core.dll` 资产 + `sha256:` digest 校验）；节点选择沿用旧逻辑（自动=gh-proxy→ghproxy.net→直连、直连、gh-proxy、ghproxy.net）；下载到 `Mcl.Core.dll.update-download` → 哈希校验 → 重命名替换（旧文件留 `.old` 待下次启动清理，失败自动回滚）→ 宿主退出后重启。镜像/哈希/响应解析/校验/替换等纯逻辑均离线可测。
- 新增 `Services/Updates/UpdateWindowRunner.cs`：STA 窗口调度器，从 `UpdateManager` 提取，两条更新轨道共用。
- 恢复 `Services/Updates/MarkdownWpfParser.cs`（上一轮随旧窗口删除），继续渲染 Release markdown 更新说明（含 GitHub 图片代理）。
- 新增 `UI/Windows/HookUpdateWindow.xaml(.cs)`：WinUI 3 风格自更新窗口（版本/说明/下载节点 → 下载进度 → 校验替换 → 就绪/失败），与 `LauncherUpdateWindow` 视觉一致。
- `UpdateManager`：配置拆分为 `DisableUpdate`（Mcl.Core.dll 自更新，旧键保留原语义）、`IsBuildChannel`（自更新渠道，恢复 Build/Release 原语义）、新键 `DisableLauncherUpdate`（启动器更新，默认开启）；`InitializeAsync` 在同一互斥锁下先执行自更新轨道、再执行启动器轨道，任一条完成即退出/重启；`DisableUpdater` 文件改为同时禁用两条轨道。
- UI：`UpdateConfigWindow` 与设置面板“更新”分区改为两条轨道各自的开关 + 插件更新渠道下拉；启动器渠道仍由 `ChannelGet()` 自动识别。
- 计数：XAML 窗口 21 → 22；`Verify-Source.ps1` 与烟雾测试同步；测试工程补 `System.Net.Http` 引用与 `HookUpdateWindow` 源链接。
- 验证：主项目 0 错误/24 警告；测试项目 0 错误/3 警告；烟雾测试离线 332 项、`MCL_LIVE_UPDATE_CHECK=1` 时 342 项通过（新增：git blob SHA1 黄金向量 `177e962b…`/`ce013625…`、SHA256 向量、Build/Release 响应解析与“已是最新”判定、镜像顺序、旧 JSON 键兼容、真实读取最新 Release 并下载 `Mcl.Core.dll` 校验 digest）；已渲染 hook-update 四态与新版配置窗口供复核。
- 未验证：真实宿主中的自更新替换与重启（会实际替换 Mcl.Core.dll）、GitHub API 限流/无 digest 的降级路径，以及自更新与启动器更新在同一个启动周期内的先后触发，需要实机回归。

## 2026-10-17：文件日志敏感信息过滤

- NLog 文件目标改为 `RedactingFileTarget`：控制台目标保持原样，写入 `logs\*.log` 的内容统一经过 `LogSanitizer.RedactSensitiveAuthValues`。目标按行追加后立即关闭文件句柄，保留原先可被其他进程读取的 `KeepFileOpen=false` 行为。
- 文件日志只替换 `sauth_json`、`unisdk_login_json`、`sead`、`sdkuid` 的值，覆盖顶层 `SauthJson`、`HttpEncrypt` 的 `/authentication-otp` 请求体及普通 JSON 键值；`sa_data`、设备环境、`otp_token`、账号、UserId、EncryptKey 等保留用于日志分析。该策略按反馈收窄，避免把有用的环境信息整段删除。
- 游戏进程日志窗口导出同样只过滤上述四个字段；房间创建、加入、设置更新日志不再输出密码明文；敏感账号诊断设置说明同步注明这四个字段会被过滤。
- 顺带将烟雾测试中陈旧的 `Beta1` 版本断言对齐当前 `AssemblyInfo` 的 `7.0.1-DLL-Public-Beta2`。
- 验证：`scripts\Validate-Refactor.ps1` PASS（209 个源文件、22 个窗口、45 项注册配置，主项目/测试项目 0 错误，337 项烟雾测试通过）；同步源码链接项后 `Mcl.Core.DetourProbe` 构建 0 错误/0 警告，自测输出 `Result=14`。首次运行曾在既有的延迟更新脚本 `apply-update.log` 文件占用检查处偶发失败，原样重跑通过。未启动真实宿主验证实际日志内容。
