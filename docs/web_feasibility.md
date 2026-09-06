# 网页部署可行性评估

核对日期：2026-09-06。对象：当前 alpha-0.0.6，以及同样使用 Godot C# 的重写前项目。

## 结论

**游戏可以另做浏览器版本并部署服务器，但现有 Windows EXE 不能作为网页游戏直接运行，当前 Godot 4 C# 工程也不能走官方支持的直接 Web 导出路径。**

当前项目为 `Godot.NET.Sdk/4.7.1`、`net8.0`，导出预设只有 Windows。Godot 官方 stable 与 latest 的 Web 导出文档均仍明确说明：

> Projects written in C# using Godot 4 currently cannot be exported to the web.

这不是“C# 这种语言永远无法在浏览器使用”的判断，而是当前 Godot C# 集成的受支持导出路径限制。Windows 自带 .NET 的单文件发布并不解决它。

## 三种目标不能混淆

| 目标 | 当前状态 |
| --- | --- |
| 网页上提供 EXE 下载 | 可以，仍是下载后在 Windows 运行，不是浏览器游戏 |
| 玩家打开网址即可玩 | 需要 Web 构建与移植，当前 EXE/工程不能直接交付 |
| 排行榜、账户、云存档或联机 | 需要另外设计服务端功能；不是静态网页部署自然附送的能力 |

## 可行路线与建议

### 路线 A：保留 Godot，迁移到受支持的脚本/运行路径

如果桌面版与网页版都重要，优先评估以 Godot + GDScript 维护一套玩法与表现，再分别导出 Windows 和 Web。
现有素材和设计可复用；新纯 .NET 核心可作规则与回归参考，但 C# 模拟、输入、绘制和存档接口不能原封不动由官方 Web 模板执行。
这是一项实际的移植任务，不是添加 Web 预设之后再点一次导出。

### 路线 B：以浏览器为主，重建浏览器原生客户端

可以把规则迁移到 TypeScript/JavaScript 的 2D 客户端，复用有权使用的素材与设计。代价是重建渲染、音频、输入、资源加载和存档集成；若同时保留旧 C# 游戏，会有双端一致性成本。
在需求尚未冻结时，不建议直接开始又一轮无约束重写。

官方文档提到 Godot 3 的 C# Web 路径，但当前项目使用 Godot 4 API，不建议将降级引擎当作低成本导出开关。

## 完成移植后，服务器需要什么

- 普通单机网页版通常由服务器/CDN 提供 HTML、JavaScript、WebAssembly 和游戏资源，游戏逻辑在玩家浏览器执行，不需要服务器逐个运行 Windows EXE。
- 建议使用 HTTPS，并正确设置 `.wasm` 的 `application/wasm` MIME；压缩、缓存与资源加载策略需要按实际 Web 包测试。
- 官方当前默认且推荐单线程 Web 导出。开启线程或相应扩展支持时才额外处理跨源隔离与相关响应头，不能把所有 Godot 网页项目都说成必须开启这些头。
- 目标浏览器需要支持 WebAssembly 和 WebGL 2；现有 Compatibility 渲染路线方向上可用，但不解除 C# 导出限制。

## 当前代码还要处理的端侧问题

1. 键盘硬绑定与 Space/Shift 设计：桌面浏览器可保留，但手机需要触控移动、闪身和升级操作。
2. 存档：现在 `ProfileStore` 使用 .NET 文件读写。Web 版需要接入浏览器持久化；Godot 文档提示 user:// 持久化依赖 IndexedDB，隐私模式及嵌入环境可能限制它。
3. 字体：当前优先加载微软雅黑、楷体等系统字体；Web 版需要可分发的随包中文字体或经过验证的替代方案，不能假设浏览器用户安装相同字体。
4. 音频：当前进入标题即尝试播放音乐，浏览器自动播放限制需要通过用户首次点击/按键解锁。
5. 性能：大量弹幕、对象分配、Canvas 绘制和资源加载必须在浏览器/手机实测；C# 桌面测试数据不能替代 Web 帧率与内存测试。
6. 包体：183.5 MiB 是 Windows 引擎、.NET 与资源的成品大小，不能直接当作未来 Web 下载大小，也不能保证 Web 会很小。
7. 素材边界：仓库明确将部分原作素材标为仅供内部验证；公网部署前必须另外确认素材使用与传播权限。

## 本轮没有执行的操作

未建立服务器、未上传 EXE 或素材、未移植语言、未生成 Web 构建、未更改现有游戏功能。
建议先定下需要保留的旧版能力，再决定采用统一 Godot 脚本版本还是浏览器优先客户端。

## 官方核对来源

以下官方页面在本次核对中直接读取；stable 和 latest 均重复 C# Web 限制。它们属于可变化的支持矩阵，不应永久固化为“永不支持”。

- Godot stable，Exporting for the Web：`https://docs.godotengine.org/en/stable/tutorials/export/exporting_for_web.html`
- Godot latest，Exporting for the Web：`https://docs.godotengine.org/en/latest/tutorials/export/exporting_for_web.html`

相关本地证据：`TouhouWuxiaSurvivor.csproj`、`export_presets.cfg`、`game/presentation/GameRoot.cs`、`game/presentation/ProfileStore.cs`、`game/presentation/GameAudio.cs`。
