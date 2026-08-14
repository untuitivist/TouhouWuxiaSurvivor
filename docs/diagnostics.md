# alpha-0.0.4 Windows 诊断版使用说明

该目录是 `alpha-0.0.4` 的诊断构建变体，不是新的游戏版本。它使用与正式版相同的 Release 优化代码，通过 `diagnostics` 构建特性启用低频采样，并保留内嵌 PCK、C# 调试符号和控制台封装程序。

## 开始前

1. 将整个 `diagnostics` 目录完整解压，不能只取其中一个 EXE。
2. 暂时关闭录屏、直播叠加层和第三方帧率限制工具。
3. 使用同一存档、角色、内容选择和相近游玩时间完成两次复现。
4. 每次进入游戏后按 `F3`，在明显卡顿时截一张完整画面。

首次启动会修复旧版本遗留的非法视频设置。例如 `MaxFps=3` 会迁移到合法档位 `30`；首个 `performance-*.jsonl` 会同时记录原值、应用值和修复状态，因此原始证据不会丢失。

## D3D12 基准

双击 `Run_Diagnostics_D3D12.cmd`，复现问题后保持约 30 秒，再从游戏菜单退出。该入口用于复现后端异常；正式版默认使用 OpenGL Compatibility。

## OpenGL 对照

双击 `Run_Diagnostics_OpenGL.cmd`，重复相同流程。该入口只切换渲染后端，不修改角色、地图、敌人或构筑数据。

## 回传内容

回传最新的 `session_d3d12_*` 与 `session_opengl_*` 目录、两次 F3 截图、卡顿首次出现的大致时间，以及窗口模式和分辨率。两个会话目录都应含 `godot.log`、`performance-*.jsonl`、`session.txt` 与 `windows_version.txt`。

## 结果判读

- D3D12 接近 3 FPS、OpenGL 正常：优先检查驱动、GPU 适配器或 D3D12 后端。
- 两种后端都随实体增加而下降：优先检查 ECS 碰撞、弹幕遍历和批量绘制。
- 两种后端从开局就很低：优先检查硬件识别、资源初始化、帧率限制和系统环境。
- `originalMaxFps=3` 且 `videoSettingsRepaired=true`：旧设置曾真实锁定 3 FPS。

日志可能包含操作系统、处理器架构、Godot 版本、渲染后端和显卡名称，仅用于内部定位，不应直接公开。
