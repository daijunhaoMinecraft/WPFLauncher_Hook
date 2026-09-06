# 项目维护约定

## 开始前

- 阅读 `REFACTOR_PLAN.md`、目标目录内更具体的 `AGENTS.md`。
- 先检查 Git 状态，保留用户已有修改；禁止使用 reset/clean 或批量还原消除工作区差异。
- 本项目是 **.NET Framework 4.8、x86、WPF 类库**，不是普通独立 WPF 应用。
- `Dotnetdetour/Initialize.cs` 含模块初始化器，加载生产 DLL 即可能安装 Hook、创建控制台和启动后台服务。不要为单元测试反射加载它。
- 游戏进程 Hook 只负责启动参数、输出和进程生命周期；不要重新加入外部调试器附加、进程隐藏或驱动控制逻辑。

## 目录与边界

- 当前重构范围是 `Mcl.Core/Dotnetdetour`、`Mcl.Core/Tools`；其他目录仅进行必要的调用同步和反编译注释清理。
- `Mcl.Core/Mcl.Core.csproj` 对这两个目录使用 `Compile`/`Page` 通配符；其他目录仍显式列项。
- 外部启动器类型、混淆后的方法名、Hook 特性内字符串、协议字段和旧配置键都是兼容契约。不能进行普通文本式“全局改名”。
- 目录移动默认保留原公共命名空间；详情见子目录说明。命名空间整理应作为另一次明确的 API 迁移。

## 构建与验证

在仓库根目录的 PowerShell 中运行：

```powershell
.\scripts\Validate-Refactor.ps1
```

其他机器传入本机依赖：

```powershell
.\scripts\Validate-Refactor.ps1 -LauncherDirectory 'D:\Games\MCLauncher' -LauncherAssemblyPath 'D:\Dependencies\WPFLauncher.dump-cleaned.exe'
```

- 外部 `WPFLauncher`、`WPFControls`、CefSharp、NLog、Newtonsoft.Json 等依赖不由这个仓库完整提供。不要为了让构建变绿而替换主项目的宿主程序集为测试桩。
- 验证脚本输出到 `.artifacts/validation`，不会主动覆盖已有 `Mcl.Core/bin/obj`。
- 无宿主程序集时可用 `-SkipMainBuild` 跑源码链接测试，但仍需 NLog/Newtonsoft.Json；这不等于主项目构建成功。
- 测试仅替换宿主/更新器副作用，直接编译生产配置、工具、日志和共享 UI 源码。
- 不运行真实登录、游戏、驱动或联网房间操作来验证外观；需要这些验证时先向用户说明。

## 改码原则

- 先提取重复实现，再改调用；不要同时改变业务语义和文件位置。
- 用 Rider 的符号/调用层级工具确认影响；工具不支持某个符号时再回退到源码搜索。
- 保留解释协议约束和线程模型的注释；删除 Token/RID/RVA/File Offset、无用的教程提示和整段旧代码注释。
- 每次新建窗口都接入共享主题，添加配置都接入注册表，并更新验证计数与文档。
- 完成后记录：改动范围、构建结果、测试结果、未验证的宿主行为，不将“XAML 能编译”描述成完整功能验证。
