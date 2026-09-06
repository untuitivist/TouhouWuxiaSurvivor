# C# Web 隔离验证报告

验证日期：2026-09-06。游戏基线：alpha-0.0.9，提交 `c56e1ef8c88d63523732b41611913dc5b81e8a67`。

## 结论

**保留 C# 的技术路线已经有真实游戏运行证据，但当前版本没有通过公开部署门槛。**

不是把 EXE 放到服务器，也不是空场景演示：本次导出了现有 `game/**/*.cs`、角色、战斗和 UI，并从本机 `/TouhouSurvivor/` 路径加载 HTML / WASM / PCK。

- 4.7.1 实验模板在当前 Edge 上出现严重 WebGL 错误；旧 Chromium 能显示相同构建的战斗。
- 额外隔离的 4.6.1 实验工具链在同一个当前 Edge 上能显示标题、灵梦战斗、魔理沙魔炮、三选一界面，没有复现上述 WebGL 错误。它只是对照样本，不是正式工程降级决定。
- 中文字体缺失、裁剪后的 JSON 序列化异常、缺少手机战斗输入仍然阻止交付。没有验证 Android/iPhone 真机、跨刷新存档可靠性或手机弹幕高峰性能。
- 建议继续保留 C#，先确定可靠的 Web 工具链，再做明确的 Web 适配；目前不要直接上线，也不需要据此马上改成 GDScript 或 Unity。

## 验证矩阵

| 项目 | 结果 | 证据与边界 |
| --- | --- | --- |
| 隔离 .NET 9 编辑器编译 | 通过 | 两组实验工具链均编译现有 C#，0 警告、0 错误 |
| 真正包含 C# 的 Web 导出 | 通过 | 导出日志包含 `GodotSharp.dll` 与 `TouhouWuxiaSurvivor.dll` 的打包记录 |
| 子路径托管 | 通过 | `/TouhouSurvivor` 308 到带斜杠路径，WASM MIME、COOP/COEP 校验通过；项目源码 URL 返回 404 |
| 4.7.1 + Edge 152.0.4191.62 | 未通过 | 标题背景/面板大片缺失，48 条 WebGL 上传/缓冲区警告 |
| 4.7.1 + Chromium 143.0.7499.4 | 有限通过 | 旧版自动化浏览器能显示像素场景、灵梦战斗与魔炮；不是对当前 Chrome 的支持保证 |
| 4.6.1 + Edge 152.0.4191.62 | 有限通过 | 标题/战斗正常绘制；中文仍为缺字方框，不能当成可交付 UI |
| 灵梦移动、闪身、F3 | 有限通过 | 真正点击开局并发送 D、Space、F3；截图中 XY 变化、闪身冷却与 Reimu 战况可见 |
| 魔理沙魔炮、升级界面 | 有限通过 | 既有确定性预览在浏览器运行并产生截图成功标记；退出时仍有两项资源未释放的报错，不算零错误回归 |
| 完整 UI / 设置 / 存档回归 | 未通过 | `JsonSerializerIsReflectionDisabled`；后续还有重绑调试键断言失败，需要修复前置异常后重测 |
| Boss 自动预览 | 未验证完成 | 同步推进到后期的预览超过 30 秒页面就绪期限；不能据此认定正常四分钟游戏一定崩溃或一定正常 |
| 844×390 触控模拟 | 有限通过 | 点击标题进入选人；没有虚拟摇杆/闪身/慢移按钮，不能完成手机战斗 |
| Android Chrome / iOS Safari 真机 | 未验证 | 桌面触控模拟不替代真实浏览器、设备内存、热降频或安全区验证 |
| 正式 Windows 源码回归 | 通过 | 原 .NET 8 构建、26/26 核心测试、UI/设置/存档验证全部通过 |

Firefox 自动化也曾尝试，但本机自动化库与已有 Firefox 的协议不匹配，随后导航失败；没有形成有效的 Firefox 游戏结论。未把工具启动失败写成浏览器不兼容。

## 已定位的问题

### 1. 当前实验模板存在浏览器版本相关渲染问题

4.7.1 日志出现 `texImage2D: ArrayBufferView not big enough for request`、`bufferData: srcOffset + length too large`。同项目 4.6.1 对照在相同 Edge 中未复现，说明不能仅归咎于我们的像素 UI。

上游 issue 16 与其评论报告了相同现象，但本次没有从源码证明最终根因，也没有修改或重新编译引擎。没有采用浏览器降级、关闭安全机制等面向玩家的规避方案。

本实验的修改版 `Browser.targets` 强制线程、SIMD 与异常处理；它不是 Godot 官方的普通单线程 Web 路线。测试服务提供 COOP `same-origin` 和 COEP `require-corp`，页面确认 `crossOriginIsolated=true`、`SharedArrayBuffer` 可用。

### 2. 存档代码需要兼容裁剪

现有 `ProfileStore` 使用默认反射式 `System.Text.Json` 序列化。Web 发布裁剪后在保存设置时抛出异常，原 Windows 回归不受影响。

下一步优先评估显式 JSON 源生成上下文，而不是关闭整个项目的裁剪。然后分别验证初次保存、刷新恢复、旧记录迁移、损坏文件保护、IndexedDB 持久化。此次没有把写入浏览器虚拟文件系统当成刷新后仍然存在的证据。

### 3. 字体、手机输入与平台设置尚未适配

- 系统中文字体不能在本次 Web 包中提供需要的字形；截图可见缺字方框。需要随包的可分发中文字体及许可记录。
- 触控可点击按钮，但当前游戏仍只有键盘移动/闪身/慢移。需要多点触控和适当大小的移动端菜单，不能只缩小桌面 UI。
- 音频首次加载记录到用户手势限制；需要通过明确的开始操作解锁并验收真实音频，不能只看有没有播放节点。
- 桌面显示模式、窗口尺寸、VSync 等选项要做 Web 能力分流，不能将桌面设置回归原封不动当成浏览器验收。
- 原作素材仍是内部验证状态。测试服务只监听 `127.0.0.1`，没有上传公网，也没有确认公开分发授权。

## 包体与性能口径

| 构建 | 输出文件总字节 | MiB |
| --- | ---: | ---: |
| 4.7.1 实验 Web | 100128938 | 95.49 |
| 4.6.1 对照 Web | 97610195 | 93.09 |

这是当前未精简、未做 HTTP 压缩的全部静态文件之和，不是手机首屏流量、不是真实网络下载时间，也不是 Windows EXE 的大小。实验导出还包含部分原生静态库/运行时数据，后续应审计资源包含规则，不盲删文件。

截图中 F3 的瞬时 FPS 不作为性能结论。后台标签页、初始化、软件 WebGL 和诊断预览都会影响数值。本轮没有给出手机帧率承诺。

## 可复现工具与证据

`tools/web_probe/` 的脚本只准备隔离副本，不调用上游 `install.bat`。私有 SDK/工作负载、NuGet 缓存与来源、Godot 模板/编辑器数据均在 `artifacts/web-probe-20260906/` 中。脚本默认固定源提交；直接调用 PowerShell 脚本时可以显式传入 `SourceRevision`。

本机先用 CMD 执行：

```bat
tools\web_probe\bootstrap.cmd
tools\web_probe\run.cmd prepare
tools\web_probe\run.cmd workload
tools\web_probe\run.cmd export
tools\web_probe\bootstrap.cmd 4.6.1
tools\web_probe\run.cmd prepare 4.6.1
tools\web_probe\run.cmd export 4.6.1
tools\web_probe\browser_check.cmd
```

`prepare` 拒绝覆盖已有实验项目；本机已经准备完成时跳过它。CMD 包装使用本机已存在的 PowerShell 7 / Node / Playwright 路径；其他电脑应配置等价依赖后直接调用对应脚本，不能回退 PowerShell 5.1。浏览器用现有 Edge，不使用个人浏览器配置。

浏览器检查退出码 `2` 表示本次样本不满足部署条件，退出码 `1` 表示检查程序本身失败；导出检查成功不等于浏览器检查成功。该检查工具是此版本的验证记录，不是完整发布门禁。

主要证据均位于 `artifacts/web-probe-20260906/`：

- `browser-check/report.json`：八组浏览器检查、异常、截图文件名和托管断言。
- `browser-check/edge-4.7.1-title.png` 与 `browser-check/edge-4.6.1-title.png`：相同 Edge 下的引擎版本对照。
- `browser-check/edge-4.6.1-reimu-before.png`、`edge-4.6.1-reimu-input.png`、`edge-4.6.1-marisa-beam.png`、`edge-4.6.1-choices.png`、`edge-4.6.1-touch-emulation.png`：真实渲染证据。
- `engine-export.log`、`compat-4.6.1/engine-export.log`：包含托管程序集的最终导出记录。
- `browser-ui-smoke.json`：最初 4.7.1 / Chromium 143 的 JSON 异常；4.6.1 / Edge 的异常在最终报告中。
- `upstream-issue-16.json`：上游问题和评论的本地快照。
- `desktop-regression.log`：原 .NET 8 的桌面回归结果。

已缓存的工具、下载与实验副本约占 4.04 GiB，保留不删除。初次 .NET 9 工作负载安装输出曾提示生成 ASP.NET 开发证书；未执行信任命令，也未删除证书。后续脚本显式禁用自动生成开发证书。正式 SDK 选择、Godot 模板与发布 EXE 没有替换。

alpha-0.0.9 EXE 的 SHA-256 仍为 `E21BFEDEC06B3DD1F2E730A97776C5B14BF609F7361FC73D4854509C42043AE8`。本轮不改版本号、不导出新 Windows 发行版、不推送、不部署域名。

## 固定的上游来源

本轮 web.run 未提供可用正文，以下实验包/源码/API 通过 curl 或 PowerShell 7 直接读取；不声称本轮重新核实了官方 stable 支持矩阵。

- 实验项目：`https://github.com/ComplexRobot/godot-dotnet-web-export`
- 4.7.1 发布：`https://github.com/ComplexRobot/godot-dotnet-web-export/releases/tag/4.7.1-stable`，归档 SHA-256 `ad76e72610187b13e83229e863928c32689b1ba5dda34f5210940d563b89e473`。
- 4.6.1 发布：`https://github.com/ComplexRobot/godot-dotnet-web-export/releases/tag/4.6.1-stable`，归档 SHA-256 `fa0e3d834864eb135c599b28c2edda942dfc2b033320a388262febe20e49097e`。
- 渲染问题：`https://github.com/ComplexRobot/godot-dotnet-web-export/issues/16`。
- 原型实现：`https://github.com/godotengine/godot/pull/106125`，此次通过实验项目 README 确认其来源，未逐行审计整个 PR。
- .NET SDK 元数据：`https://builds.dotnet.microsoft.com/dotnet/release-metadata/9.0/releases.json`；使用私有 SDK 9.0.317 / wasm-tools 9.0.19，下载的 SHA-512 固定在 bootstrap 脚本中。
