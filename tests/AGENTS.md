# 测试边界

- `Mcl.Core.SmokeTests` 是无需额外测试框架包的 net48/x86 控制台验证器，直接链接生产的配置、算法、日志和 UI 源码。
- `HostStubs.cs` 只代替外部宿主类型与更新器副作用，不复制被测配置、算法或日志逻辑。
- 测试程序集也叫 `Mcl.Core`，仅为解析真实 pack URI；不要把它复制到启动器目录，也不要引用生产 DLL。
- 禁止把 `Dotnetdetour/Initialize.cs` 加入测试项目。
- Golden vectors 来自改动前工作区，不能为了让测试通过而随意改写期望值。
- UI 测试解析所有窗口 XAML，但移除 code-behind 事件，不启动账号、网络、更新、游戏或驱动流程。
- 增加新窗口后更新窗口计数；增加新开关后覆盖旧键迁移及批量校验。
