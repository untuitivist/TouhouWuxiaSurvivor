# WebGPU 本地渲染实验

后续真实游戏验证已完成本机可执行检查：隔离整合了 Mono/WebGPU 引擎，但因大量实体/地面漏画、持久化、生命周期及故障回退失败，**不通过完整验收，不可发布**。详见 [真实游戏完整验证](webgpu_full_validation.md)。以下保留原先 JS 渲染实验的历史范围与结果，不将其回退或 FPS 冒充真实游戏通过。

核验时间：2026-09-10 02:11（北京时间）／2026-09-09 18:11 UTC。游戏基线：alpha-0.1.7，提交 `ae61d89559c593e131cab4da7bba1ff9539e3563`。

## 结论与范围

- **浏览器层面的真实 WebGPU 绘制与 WebGL2 回退已通过；实际 Godot/C# 游戏未切换后端。** 本轮只做下一版的本地技术验证，不升版本、不导出发行包、不推送、不部署。
- 两个后端绘制同一原有妖精图集、相同 1,000 个精灵、相同数据和分辨率；不是只检测 `navigator.gpu`，也不是两套游戏逻辑。
- 当前 C# Web 引擎与本次核查的 WebGPU 引擎分支并不兼容，不能直接替换导出模板。没有为了实验改语言、另建玩法项目、降级兼容性或改变现有素材。
- 测量未包括 C# 战斗、ECS 更新、碰撞、UI、音频、资源下载或真实手机。**没有证据证明完整游戏因此更快，也没有重新认证 1,000 战斗实体／60 FPS。**

## 引擎与导出支持核查

| 对象 | 核查依据 | 对本工程的意义 |
| --- | --- | --- |
| Godot 官方稳定版 Web 导出说明 | 文档限定 WebGL 2.0／Compatibility，明确不支持 WebGPU [1] | 不能把项目设置改成 WebGPU 就完成迁移 |
| 已发布 C# Web 工具链 | `tools/threadless/toolchain.json` 固定 ComplexRobot/godot `b94985982075d0c7d73bbced427516ce5f3e140f`；导出脚本使用 4.6.1，.NET SDK 9.0.317、emsdk 4.0.11 | 保留现有同项目 C#、无共享内存 Web 构建和 Windows 导出 |
| 候选 dwalter/godotwebgpu | `webgpu-4.6.2` 核查提交 `f329e39ce8db7acaa5c9d6628a530fb769969228`；Web 平台声明的 `supported` 仅有 `webgpu`，Mono 配置在缺少 `mono` 支持时中止构建 [2][3] | 该原样分支不是 C# Web 模板；需要另行审计、整合 Mono Web 与 WebGPU 引擎改动 |
| 本地 Edge 硬件实验 | 实际取得 NVIDIA Lovelace 设备、创建管线、绘制并回读像素 | 证明本机浏览器 API 可用，不证明 Godot 已支持该后端 |

以上是指定文档、指定分支的核查，不代表穷举所有第三方引擎。没有构建或合并候选引擎；仅添加支持标记也不能视为完成 Mono 移植。浏览器支持、引擎支持、C# 导出支持需要分别验证。

## 实验结构

位置：`tools/webgpu/`，由 `.gdignore` 隔离于游戏资源导入之外。没有更改游戏导出预设或运行时入口。

| 文件 | 职责 |
| --- | --- |
| `scene.mjs` | 固定工作负载、可复用实例数组、统计、像素对照、后端选择规则 |
| `renderers.mjs` | WebGPU／WebGL2 资源生命周期、着色器、批绘制、像素回读与错误报告 |
| `lab.mjs`、`index.html` | 实验状态、手动后端切换、对照测量、设备丢失演练和明确的范围提示 |
| `server.cjs` | 仅绑定 127.0.0.1；固定文件白名单；不暴露整个仓库，不设置跨源隔离头 |
| `scene.test.mjs`、`server.test.cjs` | 数据、回退、错误与服务器访问边界测试 |
| `verify.cjs`、`verify.cmd` | 有限时真实浏览器验证、截图、输入 SHA-256、报告与日志归档 |

原图直接读取 `assets/internal_original/base/actors/wild_fairy.png`（192×48，4 帧，每帧 48×48），没有重新绘制、改图或制作新美术。

实例布局对齐 `game/presentation/SpriteBatch.cs` 的 16 浮点格式；这是共享数据约定，不是直接调用 Godot 的实例渲染器。

| 浮点偏移 | 数据 |
| --- | --- |
| 0–3、4–7 | 两行二维变换，平移分别在 3、7 |
| 8–11 | RGBA 染色与透明度 |
| 12、13 | 动画帧索引、帧数倒数 |
| 14–15 | 保留 |

固定 1280×720 内部分辨率、1,000 实例、每帧 64,000 字节实例上传、单次实例绘制。两后端使用同一可复用 Float32Array；缓冲、纹理、管线或程序、VAO 等在后端存活期间复用，而非每帧重建。WebGPU 每帧仍按 API 创建命令编码器，未宣称完全零分配。

两后端显式统一透明混合、色彩上传和最近邻采样。初测发现半尺寸 24px 精灵在纹素边界有分歧；改用 `textureLoad`／`texelFetch` 整数寻址，并在取整前加入共同的 `TEXEL_BIAS = 1/1024` 纹素偏置，固定边界取样规则。没有修改原图或放宽像素通过阈值。旋转光栅边缘仍存在少量差异，不能宣称任意场景逐像素一致。

WebGPU 不可用时记录原因后退回 WebGL2。设备丢失通过真实 `device.destroy()` 触发；恢复时新建 canvas，避免在已绑定 WebGPU 上下文的 canvas 上切换 WebGL2。两种后端都不可用时显示失败，不伪报恢复。

## 运行与证据

在仓库根目录的 CMD 中执行：

~~~cmd
tools\webgpu\verify.cmd
~~~

此入口使用本机已配置的 Codex Node／Playwright 和 Edge，不下载模型、引擎或依赖；Node 路径在入口脚本内显式指定。验证结束后关闭本轮启动的浏览器与服务器，日志保留为 `artifacts/webgpu-lab/verify-*.log`。

其他环境需要先准备 Node.js（含 node:test 与 fetch）、Playwright 和可用的 Chromium 系浏览器，再用自己的 Node 执行两项测试与 `verify.cjs`。浏览器可通过 `WEBGPU_BROWSER_EXE` 环境变量指定。没有可用 WebGPU 时报告会明确跳过对应测量，不把仅 WebGL2 成功当作 WebGPU 成功。

仅手动打开实验页面时：

~~~cmd
node tools\webgpu\server.cjs
~~~

如 Node 不在 PATH，使用 `verify.cmd` 中的 Node 完整路径。浏览器访问 `http://127.0.0.1:8769/`，CMD 按 Ctrl+C 停止；这是本地实验地址，不是线上游戏入口。

最新一轮：`artifacts/webgpu-lab/2026-09-09T18-11-05-609Z/report.json`；日志：`artifacts/webgpu-lab/verify-4799-23665.log`。报告记录测试时的基线提交与源文件／素材 SHA-256；`latest.json` 指向最近报告。截图及详细报告位于本机被 Git 忽略的 artifacts 目录，源码、文档和结果摘要提交进仓库；失败记录没有清除。

### 正确性与兼容性

- 12 项单元／服务器测试全部通过，覆盖实例数据复用、非法输入、计时统计、像素检测、初始化失败回退和服务器白名单。
- 6 组真实浏览器验证全部通过：真实双后端对照及设备丢失恢复、API 缺失、adapter 缺失、设备申请拒绝、双后端不可用、触屏 DPR3 模拟。四类初始化故障使用测试注入；设备丢失使用真实 GPUDevice 销毁。
- Edge `152.0.4191.62`；WebGPU 返回 `nvidia / lovelace / isFallbackAdapter=false`，WebGL2 的 ANGLE 信息为 NVIDIA GeForce RTX 4070 Ti / Direct3D11。使用无界面浏览器，没有开启 unsafe 或强制 WebGPU 参数；不视为用户浏览器实玩结果。
- 所有情境均验证 `crossOriginIsolated=false`、`SharedArrayBuffer=undefined`；DPR3 模拟没有扩大内部绘制分辨率，不代表真实手机 GPU。
- 对齐半尺寸图在 921,600 像素中完全一致（最大误差、平均误差均为 0）。旋转图有 8 个超阈值差异像素，平均通道误差 0.0003125，最大单通道误差 101；不是整幅全等。
- “差异像素”定义为任一 RGBA 通道误差大于 3／255。固定验收要求：可见像素不少于 10,000、差异像素比例不超过 1%、平均通道误差不超过 0.5；没有为此次通过调松阈值。
- 报告没有运行错误；浏览器对 Windows 忽略 `powerPreference` 的警告原样保留。通过 Git 差异检查确认游戏运行时代码、素材和版本文件未变化；没有因此重跑完整游戏满载测试。

截图位于同一轮报告目录：`comparison.png`、`device-loss-fallback.png`、`api-missing.png`、`adapter-missing.png`、`device-rejected.png`、`both-missing.png`、`touch-dpr3.png`。设备丢失和 API 缺失截图实际显示 WebGL2 图像、“已回退”状态与各自原因。

### 性能计时边界

每种 API 测三轮，每轮预热 30 帧、记录 120 帧；交替运行顺序，保持相同工作负载。下表为三轮均值，完整 median／p95／max 保留在报告。

| 后端 | CPU 准备均值（ms） | CPU 提交均值（ms） | 仅渲染 RAF 回调速率（Hz） |
| --- | ---: | ---: | ---: |
| WebGL2 | 0.09417 | 0.01194 | 200.01 |
| WebGPU | 0.08583 | 0.05806 | 200.00 |

CPU 准备指生成实例数组，CPU 提交指调用上传、绘制或队列提交的同步耗时，不是 GPU 完成耗时。虽设备提供 timestamp-query，本实验没有测量 GPU 时间。高频短样本、浏览器计时量化、无界面环境和未饱和负载均限制性能推断。

**约 200Hz 是本实验的 requestAnimationFrame 回调速率，不是游戏 200 FPS。当前数据未证明 WebGPU 有性能收益，也不足以认定其在实际游戏中更慢。**

## 下一阶段门槛

当前决定：保留生产 WebGL2 和现有 Windows 渲染；不因这次 API 验证直接发版。

如继续真正接入游戏，应先在隔离的引擎构建区审计并整合 Mono Web 与 WebGPU 支持，固定可重建的引擎与工具链，再用同一个 C# 项目验证启动、真实战斗、设置、触控、存档、贴图稳定性，以及无 WebGPU／设备丢失时的生产兼容导出回退。不能把本页的 JS 回退当成 Godot 已具备对应能力。

通过这些功能门槛后，才对同种子完整战斗进行可比较的 CPU／GPU／内存与真实手机测试；只有有证据的收益才考虑发布。同时保留 Windows 自包含 EXE 和 Web 双端交付，不能为 WebGPU 另写一个游戏或移除旧浏览器路径。本轮没有开始大型引擎下载、编译或发布。

## 一手来源

于上述核验日期直接读取成功；Web 搜索工具未返回可用引用文本，因此提供实际检查的官方页面与固定提交路径，便于重新核查。

1. Godot 官方 Web 导出文档：`https://docs.godotengine.org/en/stable/tutorials/export/exporting_for_web.html`
2. 候选固定提交的 Web 配置：`https://github.com/dwalter/godotwebgpu/blob/f329e39ce8db7acaa5c9d6628a530fb769969228/platform/web/detect.py`
3. 候选固定提交的 Mono 配置：`https://github.com/dwalter/godotwebgpu/blob/f329e39ce8db7acaa5c9d6628a530fb769969228/modules/mono/config.py`
4. 当前 C# Web 实验工具链上游：`https://github.com/ComplexRobot/godot-dotnet-web-export`；以本仓库 `tools/threadless/toolchain.json`、`tools/platform/build_web.ps1` 的固定版本为实际构建依据。
