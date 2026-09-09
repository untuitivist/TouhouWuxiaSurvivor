# 全量美术与双端验证记录

日期：2026-09-09。活动素材：shrine-v04。版本保持 alpha-0.1.6，未发布、未推送、未部署。

## 已完成范围

- 当前游戏所用的 54 项旧资源均有对应替换；补足完整立绘、四向动作、建筑、设定板和图集后，合计 64 项 Aseprite 源文件与 PNG。
- 灵梦和魔理沙各 32 帧，四方向各含待机、四个不同步态、三帧施法；每位角色十二个动画标签。
- 人物／妖怪／树木／建筑／阴阳玉使用统一图集、脚底排序、前景遮挡淡化和直立补偿；地面使用 0.72 深度投影。
- 角色选择与图鉴使用完整立绘；标题保持月夜鸟居构图并调整为当前色板。UI 九宫格、图标、战斗纹理一起替换。
- 保留旧源稿与本轮 v01～v03 候选；只有 v04 被运行时和打包脚本引用。HUD 数字、血条、小地图点位与范围预警仍由界面／几何逻辑绘制，不冒充 Aseprite 栅格素材。

## 实测结果

| 检查 | 结果 |
| --- | --- |
| Aseprite 源文件复开及可见层逐像素导出一致 | 64 / 64；隐藏参考层锁定且未进入 PNG |
| 图集布局、四向帧与动作标签 | 14 区域，无越界／重叠，四步态不同，标签不串方向 |
| C# 核心与投影／动画测试 | 43 / 43 |
| 原生 UI、语言、角色、图鉴、技能和批次场景 | 15 个检查模式全部通过 |
| 原生及 Web 方向图集渲染 | 每位角色执行 32 帧的 UV、遮挡、直立对照 |
| Web 无跨源隔离和共享内存 | crossOriginIsolated=false；SharedArrayBuffer=undefined |
| Web 功能 | 桌面、触控、触控升级、灵梦完整通关、魔理沙完整通关全部通过 |
| Web 技能画面 | 灵梦阵／梦想封印、魔理沙星弹／蓄力／魔炮，5 个固定场景通过 |
| Web 贴图变黑回归 | 两位角色均通过；注入错误前置颜色后，全部实例绘制仍显式提供颜色 |
| Web 小地图 | 桌面、手机、手机 DPR3、小触屏，4 种布局通过 |
| Web 图鉴 | 4 种布局，各检查 22 个已实现条目，返回卡片和暂停状态保留 |
| Web 语言 | 语言切换、保存／重载及共享语言 smoke，3 个检查通过 |

当前仓库、最终 Windows 验证目录和最终 Web 验证目录的 **88 个 C# 文件逐字节一致**；64 张当前素材 PNG 也逐字节一致。共享 C# 源快照摘要：

2648d2c84a278efe0a02dea2645b69a3ca57ec964d3f567c8025732ef4b8b653

这是两个构建目标，不是两套维护中的玩法工程。Web 构建使用已有兼容工具链和独立临时目录；检查没有修改服务器或线上资源。

## 证据位置

- 可追溯摘要：art/shrine-v04/validation.json。
- 源文件报告：artifacts/aseprite-complete-verification/shrine-v04/report.json。
- 最终原生图像与日志：artifacts/redraw-native/20260909-135454-596/。
- 最终 Web 构建及所有验证报告：artifacts/web-builds/20260909-215457-903/。
- 核心测试日志：artifacts/complete-art-core-tests.log。
- 编译日志：artifacts/complete-art-compile-final.log；0 警告、0 错误。

artifacts 下是本机验证产物，不纳入仓库；摘要记录纳入仓库。各报告中的 sourceCommit 是验证开始时的父提交，sourceDirty=true 表示测试的是本轮工作区，不是旧提交；上面的内容摘要绑定实际被测源码。

## 复查入口

- tools/aseprite/verify_redraw.cmd：Aseprite 源文件、动画、图集和打包边界。
- tools/aseprite/verify_redraw_native.ps1：使用 PowerShell 7 执行，复制到独立目录验证原生 UI 与画面。
- tools/platform/build_web.ps1 -Threadless：使用 PowerShell 7 构建本地无共享内存站点。
- tools/platform/verify_web.cmd --compatible --unisolated：本地 Web 功能与双角色通关。
- tools/platform/verify_hero_art.cmd：Web 技能截图。
- tools/platform/verify_batches.cmd --color-state-stress：Web 纹理状态故障注入与四向图集对照。
- tools/platform/verify_journal.cmd：Web 图鉴。

## 不夸大的边界

- 绘制是 Aseprite Lua 脚本落笔，不是人工鼠标逐笔画。未使用参考图像素提取流程来冒充重绘。
- 画面已进行实际截图审查，但用户尚未认可这套新稿；不声称与设定板一模一样，也不把技术测试通过等同于美术验收。
- 手机检查为桌面浏览器的触屏／高 DPI 模拟，不是小米等物理设备实测；本轮没有重新宣称“手机 1000 实体 60fps”。
- 没有升版、发行 EXE、创建发布标签、Git 推送或服务器部署；因此线上网页仍是先前发布版本。
