# 测试边界

- 不再维护 `Mcl.Core.SmokeTests` 或源码链接的宿主桩测试项目。
- `Mcl.Core.DetourProbe` 是单独的 x86 原生回跳诊断程序，只修改它自己声明的测试方法，不加载生产 DLL 或初始化器。更改 Detour 后需分别运行及在 Rider 下调试：两者都应输出 `Result=14`；调试成功后会主动中断，继续即可退出。
- `RoomManage.RenderingTests.cjs` 仅使用 jsdom 检查 HTML 渲染与转义，禁用网络和 WebSocket；依赖放在 `.artifacts`，不要提交 `node_modules`。
- 测试和诊断程序不得加入 `Dotnetdetour/Initialize.cs`，也不得加载或启动生产 Hook。
