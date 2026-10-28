# 项目维护约定

## 开始前

- 阅读目标目录内更具体的 `AGENTS.md`；本地 `REFACTOR_PLAN.md` 仅供个人参考，不纳入仓库。
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
[README.md](Mcl.Core/Dotnetdetour/Models/Config/README.md)
```powershell
.\scripts\Validate-Refactor.ps1 -LauncherDirectory 'D:\Games\MCLauncher' -LauncherAssemblyPath 'D:\Dependencies\WPFLauncher.dump-cleaned.exe'
```

- `WPFLauncher`、`WPFControls`、CefSharp、NLog、Newtonsoft.Json 等编译依赖统一由私有仓库 `WPFLauncher_Hook_Build` 提供；宿主 EXE/DLL 和编译引用程序集不得复制进本仓库、Release 或 Actions artifact。经明确批准的独立运行时 DLL 只能放入 `build-artifacts` 分支的 `build/`。
- 验证脚本输出到 `.artifacts/validation`，不会主动覆盖已有 `Mcl.Core/bin/obj`。
- 无宿主程序集时可用 `-SkipMainBuild` 只跑源码审计；这不等于主项目构建成功。
- 不运行真实登录、游戏、驱动或联网房间操作来验证外观；需要这些验证时先向用户说明。

## 私有依赖与 C# 引用

- 新增托管 DLL/EXE 引用时，先把文件放入 `WPFLauncher_Hook_Build/launcher`，再在私有仓库运行 `scripts/Update-Dependencies.ps1`。该脚本负责校验文件类型、重建 `SHA256SUMS.txt`、提交并推送；不要手工维护哈希或只上传二进制。
- 为编译引用添加文件时，主项目只提交引用配置，不提交二进制；经批准的公开运行时文件除外，且只能放入 `build-artifacts` 分支的 `build/`。使用 `$(LauncherDirectory)`，不要写本机绝对路径：

```xml
<Reference Include="Example">
  <HintPath>$(LauncherDirectory)\Example.dll</HintPath>
  <Private>false</Private>
</Reference>
```

- 主宿主使用现有 `$(LauncherAssemblyPath)`；其他托管 EXE 可像 DLL 一样通过 `$(LauncherDirectory)` 引用。`Reference Include` 应使用程序集简单名称。
- 宿主已经提供的程序集可设 `<Private>false</Private>`。需要随插件分发时再设 `true`，并显式增加公开产物复制步骤、确认授权；仅设置 `Private=true` 不会让 Actions 自动发布它。
- 原生 DLL/EXE 不是 C# 程序集引用；应通过 P/Invoke 使用，并按运行时资源单独处理。
- `Mcl.Core.DetourProbe` 等独立诊断项目仍应通过 `LauncherDirectory` 解析其依赖；不要把它们加入生产项目或启动器分发目录。
- 只更新私有仓库二进制不会触发公开仓库的 `Build Mcl.Core`。添加引用或修改主项目后推送 `main`，或手动运行：

```powershell
gh workflow run build-mcl-core.yml --repo daijunhaoMinecraft/WPFLauncher_Hook --ref main
```

- Actions 的 `WPFLAUNCHER_HOOK_BUILD_DEPLOY_KEY` 是只读 Deploy Key。不得打印密钥，也不得让工作流上传 `.private-build` 内容。

## 构建产物与更新

- `main` 不跟踪生成的 `Mcl.Core.dll` 或 `Mcl.Core/bin/x86/Debug/net48/Mcl.Core.dll`；本地构建副本由 `.gitignore` 忽略。
- Actions 将唯一公开更新产物 `build/Mcl.Core.dll` 发布到 orphan 分支 `build-artifacts`，不得提交回 `main`。
- `HookUpdateManager.BuildRef` 必须为 `build-artifacts`，`BuildFilePath` 必须为 `build/Mcl.Core.dll`；下载地址应固定到具体产物提交，避免 Raw/CDN 缓存旧文件。
- Actions 产物提交继承触发构建的源码提交消息，并附带 `MCL-SOURCE-COMMIT: <sha>`；更新窗口应显示源码提交内容并链接到源码提交。
- 连续提交时，`concurrency` 应取消旧运行；发布前必须再次确认 `GITHUB_SHA` 仍是当前 `origin/main`，旧运行不得覆盖新产物。
- `main` 不再保留 `build/` 目录；生成 DLL 和经批准的可选附件只放在 `build-artifacts` 分支的 `build/`。
- `build-artifacts/build/wintun.dll` 用于组网模式；`ocr.dll`、`OcrProxy.dll`、`WmCode.dll` 用于验证码自动识别，后三个必须同时提供。

## 改码原则

- 先提取重复实现，再改调用；不要同时改变业务语义和文件位置。
- 用 Rider 的符号/调用层级工具确认影响；工具不支持某个符号时再回退到源码搜索。
- 保留解释协议约束和线程模型的注释；删除 Token/RID/RVA/File Offset、无用的教程提示和整段旧代码注释。
- 每次新建窗口都接入共享主题，添加配置都接入注册表，并更新验证计数与文档。
- 完成后记录：改动范围、构建结果、测试结果、未验证的宿主行为，不将“XAML 能编译”描述成完整功能验证。
