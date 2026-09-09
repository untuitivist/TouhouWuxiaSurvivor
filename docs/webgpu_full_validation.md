# WebGPU 真实游戏完整验证

验证时间：2026-09-09 UTC（本地 Asia/Shanghai 为 2026-09-10）。对象为同一套 alpha-0.1.7 C# 游戏，不是先前的 JavaScript 精灵实验。

## 结论：NO-GO，不可替换正式版

已经完成本机可执行的主验证及实际画面对照，但**完整验收不通过**。实验引擎可以启动 C# 游戏、使用真实硬件 WebGPU，却漏画大量批量实体和地面，存在读回、刷新持久化、生命周期及降级问题。没有手机真机证据。不能以它的 FPS 数字宣称千实体达标或提速。

- 主验证 47 组：29 组脚本通过、18 组失败。它们是分组，不是把每个单元测试算作一组。
- 另补做 6 对真实静态场景：12 次硬件截图成功，6 对视觉一致性全部失败。
- 后续仅增加测试的超时/清理边界，没有降低断言、削减游戏负载或修改玩法来换取通过。修订后的 WebGL2 无隔离完整 UI 回归再次通过。
- 游戏仍为 alpha-0.1.7；未推送、未部署、未升版本、未覆盖旧 EXE，未删除历史资料。实验仅输出到独立 artifacts 目录。

## 构建和同源性

| 项目 | 固定身份或结果 |
| --- | --- |
| 已发布共同游戏源码 | 82563e6535f42e35cfe67318d16a116b7b04dee7 |
| 实验导出时 HEAD | 78bda02f1998242c7e0f69d27563493a5f345a93；工具改动尚未提交，manifest 如实标记 sourceDirty=true |
| 原 Mono Web 引擎 | b94985982075d0c7d73bbced427516ce5f3e140f，4.6.1 |
| WebGPU 候选 | dwalter/godotwebgpu，f329e39ce8db7acaa5c9d6628a530fb769969228 |
| 候选的上游基线 | 001aa128b1cd80dc4e47e823c360bccf45ed6bad，4.6.2 |
| 合并范围 | 1,296 个引擎路径，48 个已有文件修改，0 个删除；候选/基线共 1,344 个 blob、20,995,745 字节，逐个校验 Git blob SHA-1 |
| 构建 | 独立 Mono 4.6.1 副本；Mono + WebGPU + WebGL2，threads=no；.NET 9.0.317、emsdk 4.0.11 |
| 实验模板 SHA-256 | 3190EFD1B3211430AFAAA328083F01CEA37BA9469911E3024647FB6AF23501FA |
| 共享输入复核 | 工作区、WebGL2 stage、WebGPU stage 的 179 项输入逐一匹配，其中 81 个 C# 文件；0 个差异 |
| 原 EXE SHA-256 | B2B2F21A4A191E47A2A882CAE9ECBF6E68377853E3EB6007341042586BD8D9C1，195,109,656 字节 |

这是引擎整合实验，不是第二套游戏。主项目的 Compatibility 设置不变；仅独立导出 HTML 的启动参数选择 Mobile/WebGPU，两个构建仍使用相同游戏代码和素材。实验模板与 web-webgpu-latest.json 不替换 web-compatible-latest.json。

构建过程曾遇到 support-list 合并冲突、Dawn 下载/缓存竞争及缺少原生 C++ 编译器；失败日志全部保留。最终明确合并 mono/webgpu 支持列表，改用私有 Emscripten/NuGet 缓存，并安装 Ubuntu g++（26 个新包、libc6 升级、0 个移除）。首次 Dawn 尝试在隔离缓存修正前触及了原 Emscripten 依赖缓存，因此不声称所有旧缓存未变。原引擎源码和正式导出物未变，脚本结束时终止其自己启动的 WSL 会话。

## 已通过的范围

| 范围 | 新执行结果及限制 |
| --- | --- |
| 编译/逻辑 | Debug 构建 0 警告、0 错误；Node 37/37；核心 40/40；6 条固定种子双角色通关；本地化 860/860 |
| Windows | 28 项独立 EXE 检查通过；只有 EXE 的临时目录启动，隐藏外部 .NET，检查语言、设置、图鉴、选人、战斗画面和两角色批量颜色；音频使用 Dummy，不代表实际听感验收 |
| WebGL2 功能 | raw/gzip 部署 × 隔离/无隔离，4 组各 5 个场景全部通过：桌面、触控、多指/旋转/暂停、升级和两角色通关；另含设置刷新、语言和选人立绘检查 |
| 资源加载 | 两构建的既有加载/断线重试/缓存测试通过；各 20 次 raw PCK 字节与 SHA-256 校验通过。后者替换了 Engine，只证明加载管道，不算 20 次完整游戏启动 |
| WebGPU 启动 | 真实主 canvas 的 webgpu 上下文和 NVIDIA 硬件 device 已验证；不是探测画布，不是 SwiftShader，没有开启不安全浏览器参数来凑硬件结果 |
| WebGPU 部分功能 | raw 两种隔离模式的触控、升级、两角色通关完成；选人 4 个布局完成；DPR1/3 的状态/尺寸检查完成。它们不能覆盖后述漏画问题 |

WebGPU 的 DPR 脚本只检查数量、尺寸、draw calls 和错误输出，曾在漏画时仍通过。这正是必须另做像素/画面验证，而不能只看计数器的原因。

## 阻断一：真实批量绘制漏画

实际 performance 场景，两后端同为 Tick=0、320 敌人、1,600 弹体、400 拾取物、BatchInstances=2,320、1280×720。WebGL2 可以看到完整密集实体和地面；WebGPU 只保留 UI、角色、部分景物及敌人外圈等，大片批量内容消失。六个场景的角色、时间、坐标及实体计数均逐项相等。

| 场景 | 超过单通道容差 8 的像素比例 | RGB 平均绝对误差 / 255 |
| --- | ---: | ---: |
| performance | 71.54% | 66.19 |
| reimu-field | 66.59% | 14.16 |
| reimu-spell | 66.74% | 14.81 |
| marisa-stars | 67.47% | 16.00 |
| marisa-warmup | 67.47% | 15.88 |
| marisa-beam | 67.17% | 15.47 |

预先设定的通过门槛为差异像素不超过 2%、平均绝对误差不超过 2。两图均通过非黑屏检查，因此不是“两张空白图相等”，也不是小范围抗锯齿差异。浏览器截图绕开引擎 GetImage，避免把引擎读回失败误判为没有可见画面。

对照图（左为 WebGL2，右为 WebGPU）：

![Actual game render comparison](../artifacts/webgpu-validation/2026-09-09T21-24-20-589Z/visual-comparison/comparison-performance.png)

目前证据将调查范围指向真实 Godot MultiMesh/批量路径；尚未把问题精确归因为某一条 shader/驱动语句，不能宣称已修好。代码中的 SpriteBatch 是 MultiMeshInstance2D，使用 Transform2D、颜色、自定义数据及整体 Buffer；单独 JS 16-float 实例实验通过不等于这条路径正确。

## 阻断二：读回和旧颜色压力测试不兼容

WebGPU 的两次 batch 脚本均在首个角色失败：CompareRenderedImages 收到 null Image，引出 NullReferenceException；打印异常时还有 Godot 栈信息 vector resize 错误。候选驱动 texture_get_data 的首次异步读回明确返回空向量，后续帧才提供结果，与当前同步 GetImage 验证约定不兼容。

原注入式顶点颜色压力工具针对 WebGL。不能因改用 WebGPU 没触发其注入，就把缺少覆盖算成通过；也不能只更改该工具、忽略已经实证的画面漏画。WebGPU 需要对应的真实批量读回/重建验证。

## 阻断三：刷新与生命周期

- WebGPU 桌面保存音量为 0.36 后，raw 两种隔离模式及 gzip 无隔离模式刷新读回 1；独立语言测试在切换英文并刷新后读回 zh 而非 en。这里保留为持久化失败，尚未证明是游戏代码还是新引擎文件系统同步导致。
- 两种 gzip 部署回归在 touch 后卡住，分别保留部分报告并在超过 11/7 分钟无进展后终止该测试自己拥有的进程树。未操作用户浏览器窗口或其他进程。
- WebGPU 灵梦英文基准在 load=40 通过后也卡住；保留原日志并结束该测试进程树，没有把未执行的 180/320 负载算成通过。
- 已给诊断 evaluate/screenshot/close 加超时并保留 finally 报告，完整 runner 有进程树兜底超时。这只是测试工具防挂死，不是修复引擎生命周期。

## 阻断四：四类故障均无游戏级回退

| 故障注入 | 实际结果 |
| --- | --- |
| navigator.gpu 不可用 | 清楚显示加载失败/重试，但没有启动 WebGL2 游戏 |
| requestAdapter 返回 null | 显示 adapter 未找到，没有回退 |
| requestDevice 拒绝 | 显示创建失败，没有回退 |
| 对真实游戏 device 调用 destroy | 捕获 destroyed；仍为原 WebGPU 主画布，没有 WebGL2 接管。C# Tick 从 195 到 803，不能把逻辑仍推进当作画面恢复 |

这与跨源隔离是不同维度：支持 WebGPU 的本机在 crossOriginIsolated=false、SharedArrayBuffer=undefined 下可以启动，不代表不支持 WebGPU 的小米浏览器也能玩。现有正式兼容构建保留不动。先前 JS 实验的回退成功不能冒充这四项真实游戏验证。

## 满载性能：只接受等量正确绘制后的结论

环境为本机 Windows Edge 152.0.4191.62、RTX 4070 Ti 硬件。844×390 触控布局仿真，游戏实际渲染 1280×720、DPR1、CPU throttle=1、无隔离/无共享内存。没有手机真机。基准顺序执行，不并行争抢 GPU；3 秒暖机，15 秒标准窗口，另每角色 60 秒持续窗口。记录来自 Godot FPS、C# 固定步进和系统计时，不使用 RAF 冒充游戏帧率，也没有 GPU timestamp 测量。

60 秒、320 敌人、约 1,600 弹体的诊断数据：

| 后端/角色 | 采样 FPS 均值 / 最低 | 模拟 ticks/s | 模拟步 p95 ms | 批量准备均值 ms | 解释 |
| --- | ---: | ---: | ---: | ---: | --- |
| WebGL2 灵梦 | 59.45 / 51 | 60.05 | 8.1 | 3.18 | 现有基准门槛通过，不是每帧严格 60 |
| WebGL2 魔理沙 | 32.79 / 5 | 53.56 | 20.9 | 2.53 | 性能失败，正式基线也存在这一重负载问题 |
| WebGPU 灵梦 | 59.97 / 59 | 60.01 | 7.7 | 3.08 | 漏画，不能作等量性能达标证据 |
| WebGPU 魔理沙 | 30.07 / 6 | 52.91 | 20.9 | 2.56 | 漏画且性能失败，不能计算可信的渲染收益 |

FPS 是 100 ms 定时采样的算术均值，严重卡顿时采样间隔会变长；原始 samples、wall、tick 和统计全部保留，不把它包装成严格逐帧/时间加权测量。原有数值门槛是平均 >=55、采样最低 >=45、模拟 >=55 ticks/s、模拟 p95 <=16.7 ms、批量准备均值 <=5 ms，不是严格“全程 60 FPS”。千实体是敌人/弹体/拾取物等的总量，不是 1,000 敌人。

另一个场景有效性问题：魔理沙 40/180 敌人基准约第 15 个模拟秒进入 Choosing 后停住。现有压力场景的 ResolveChoices 在 Refill 内，而 Refill 被 Playing 分支保护，进入升级后不再调用；场景还只尝试赋予灵梦分支。低/中负载这些数据属于无效的持续战斗试验，不能把暂停后的约 60 FPS 算性能好，也不能把停止 ticks 当作渲染器慢。未修改游戏场景或自动减少压力来使它通过。

320 负载保持 Playing，魔理沙的模拟/武器计时已明显超预算。这支持继续做 CPU 系统热点调查，不支持“换 WebGPU 即解决 OOP/ECS 性能”的结论。当前没有重新改架构、数值或升级树。

## 未覆盖与下一步

未连接真实手机/平板；触控仿真不等于 Android/iOS 性能。也未完成不同厂商显卡、Safari/Firefox/小米浏览器真机矩阵、实际音频听感和长时真实玩家全流程验收。

后续优先顺序：

1. 修复引擎/批量路径的漏画及读回，六组静态图和批量颜色全部过关后才比较 FPS。
2. 修复刷新持久化和 WebGPU 生命周期；设计真实启动失败回退及设备丢失处理，不能静默丢弃当前局。
3. 修复诊断场景的 Choosing 死锁和角色负载定义，分析魔理沙 CPU 热点，然后重跑等量双后端与手机真机。以上通过前不发布 WebGPU。

## 复现与证据

仓库根目录在 CMD 中执行；日志和输出保存在独立时间戳目录。脚本使用本机已有的 Node/PowerShell 7/WSL 工具链，不调用 Windows PowerShell 5.1。

~~~cmd
tools\webgpu\full_engine.cmd -Stage prepare
tools\webgpu\full_engine.cmd -Stage build
tools\webgpu\export_game.cmd
tools\webgpu\full_validation.cmd
~~~

已有模板和构建时只需最后一条。-InstallHostCompiler 是明确的可选安装开关；不会默认再次安装。--only=actual-visual-comparison 可单独做画面对照，--only 运行的报告明确标记为子集，不能冒充完整通过。

- 主验证：artifacts/webgpu-validation/2026-09-09T20-26-51-285Z/report.json；47 组独立日志和报告保留。
- 像素对照：artifacts/webgpu-validation/2026-09-09T21-24-20-589Z/visual-comparison/report.json。
- 超时工具修订后 WebGL2 回归：artifacts/webgpu-validation/2026-09-09T21-28-35-445Z/report.json。
- 最终工具复核：2026-09-09T21-38-40-250Z 的 37 项 Node 测试、2026-09-09T21-38-58-920Z 的 WebGL2 灵梦三负载基准均通过（同在 artifacts/webgpu-validation 下）。34 个改动文本的 UTF-8/无 BOM/无 NUL、16 个 JS 语法、4 个 PowerShell 7 AST 及 Python AST 通过；既有输出目录拒绝覆盖检查通过。
- 共享输入复核：主验证目录 shared-input-audit.json，179 项/81 C#，0 差异。
- 引擎合并：artifacts/webgpu-engine-f329e39c/source-manifest.json、preparation.json；成功构建日志 engine-23573-15805.log。
- 实验导出：artifacts/web-builds/20260910-040123-042/build-manifest.json；成功导出日志 artifacts/webgpu-engine-f329e39c/export-26398-7065.log。
- 正式兼容基线：artifacts/web-builds/20260910-003000-092/build-manifest.json；原 Windows EXE 及旧回执未变。

主验证的 protectedBefore/protectedAfter 全相等；同源校验另核对当前工作区和两份 stage。测试用临时目录、构建、失败日志全部保留。历史版本更新日志没有修改。本文件记录的是验证结论，不是发行公告。
