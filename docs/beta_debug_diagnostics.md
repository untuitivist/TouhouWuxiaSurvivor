# alpha-0.0.3 Windows 诊断版使用说明

> 兼容说明：该旧文件名仅为既有本地流程保留。当前统一文档为
> `docs/diagnostics.md`，统一构建入口为
> `tools/diagnostics/build_diagnostics.cmd`。

该目录是 `alpha-0.0.3` 的诊断构建变体，不是新的游戏版本。它使用与正式版相同的 Release 优化模板，并通过 `diagnostics` 构建特性启用采样日志；同时保留内嵌 PCK、C# 调试符号和控制台封装程序。因此它适合复现正式版性能，但额外采样仍会产生少量开销。

## 开始前

1. 将整个 `diagnostics` 目录完整解压，不能只取其中一个 EXE；诊断构建与正式版共享 `alpha-0.0.3` 版本号。
2. 暂时关闭录屏、直播叠加层和第三方帧率限制工具。
3. 使用同一存档、相同角色、相同内容选择和相近游玩时间完成两次复现。
4. 每次进入游戏后按 `F3`，在明显卡顿时截一张完整游戏画面。

诊断版第一次启动会修复旧版本遗留的非法视频设置。例如持久化的 `MaxFps=3` 会迁移到最近的合法档位 `30` 并写回；首个 `performance-*.jsonl` 仍会同时记录 `originalMaxFps=3`、应用后的上限和 `videoSettingsRepaired=true`，因此原始证据不会丢失。第二次 OpenGL 对照使用修复后的同一设置，避免继续把非法锁帧误判成渲染后端差异。

## 第一次：D3D12 基准

双击 `Run_Diagnostics_D3D12.cmd`。这是用于复现已知异常的 D3D12 与 Mobile 对照组合；正式版默认已改为 OpenGL Compatibility。正常游玩到低帧问题出现，保持约 30 秒，然后从游戏菜单退出。

游戏退出后会自动打开本次会话目录。保留其中的：

- `godot.log`：引擎、GPU、错误和每秒 FPS 输出。
- `performance-*.jsonl`：游戏环境头与逐秒性能数据，包括实际帧率上限、设置迁移、CPU/GPU 帧时、内存、GC、敌人、双方弹幕和区块负载。
- `session.txt`：构建、渲染后端、处理器架构和退出码。
- `windows_version.txt`：Windows 版本。

## 第二次：OpenGL 对照

双击 `Run_Diagnostics_OpenGL.cmd`，重复完全相同的流程。该入口只把渲染后端切换为 OpenGL Compatibility，不修改角色、地图、敌人或构筑数据。

## 回传内容

请将以下内容一起压缩回传：

1. `logs` 下最新的一个 `session_d3d12_*` 目录。
2. `logs` 下最新的一个 `session_opengl_*` 目录。
3. 两次复现各一张打开 F3 后的截图。
4. 卡顿首次出现的大致时间，例如“开局即出现”或“游玩 12 分钟后出现”。
5. 是否只在窗口最大化、全屏或特定分辨率下出现。

不要只回传空白的当前 `godot.log`。确认两个会话目录都含有 `performance-*.jsonl`；诊断启动器为每次运行创建独立目录，不会覆盖之前的日志。若直接双击主 EXE，结构化日志会回退到 `%APPDATA%\Godot\app_userdata\Touhou-Wuxia-Survivor\diagnostics`。

## 如何理解结果

- D3D12 接近 3 FPS、OpenGL 明显正常：优先检查显卡驱动、错误 GPU 适配器或 D3D12 后端。
- 两种后端都随敌人和弹幕增加而同步下降：优先检查 ECS 碰撞、弹幕遍历和批量绘制。
- 两种后端都在开局立即很低：优先检查硬件识别、资源初始化、帧率限制和系统环境。
- `originalMaxFps=3` 且 `videoSettingsRepaired=true`：已确认旧设置把游戏真实锁在 3 FPS；无需先归因于 ECS 或显卡。
- 只有诊断版明显变慢：先检查日志采样或控制台环境造成的额外开销，再结合正式版复现。

日志只用于内部定位。日志可能包含操作系统版本、处理器架构、Godot 版本、渲染后端和显卡名称，不应在公开渠道直接发布。
