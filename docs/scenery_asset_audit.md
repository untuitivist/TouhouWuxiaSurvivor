# 原作场景素材接入

> 历史接入记录：当前视觉规则已改为原作图片仅作参考、运行时使用统一 Aseprite 重绘。以下原图与裁切说明仅保留作研究历史，不再描述当前素材来源。现行规则见 `unified_art_direction.md`。

## 本轮范围

用户要求把树木、鸟居、标题背景改用已有素材。四张 PNG 均从用户提供的原作包逐字节复制，保留像素、尺寸与透明度，不重新画树、鸟居、山景或天空，不修改原包。

| 用途 | 原包中的路径 | 接入方式 |
| --- | --- | --- |
| 树冠 A | TH13.5 东方心绮楼/backGround/bg01/梩偭傁彫01.png | 256×256 原图，作为俯视林木树冠，不声称是新绘制的完整树干精灵 |
| 树冠 B | TH13.5 东方心绮楼/backGround/bg01/梩偭傁彫02.png | 256×256 原图；沿用原有确定性位置和大小变化，以现有哈希选取两种树冠 |
| 鸟居 | TH13.5 东方心绮楼/actor/mamizou/texture/torii.png | 167×275 透明图，等比例缩放；角色素材复用为无碰撞环境装饰，不冒称神社关卡贴图或新增角色能力 |
| 标题背景 | TH15.5 东方凭依华/event/pic/bg/bg_hakurei.png | 901×557 黑白博丽神社插画；等比例填充 16:9，居中裁去少量上下边缘并在绘制时染色；不是原作夜间场景复刻 |

原包中部分文件名本来就是上述乱码字符，清单完整保留，输出统一为 ASCII 文件名。来源记录不等于公开再分发授权。

## 实现与边界

- `tools/rebirth/prepare_scenery_assets.py` 生成四张原图副本与 `assets/internal_original/base/scenery/source_manifest.json`；`--check` 核对原文件字节、输出文件字节、哈希、尺寸与清单。不重新压缩 PNG，不提取颜色作为遮罩。
- `PixelLandscape` 不再逐像素生成图片，仅加载原作资源。`GameCanvas` 在初始化时缓存标题、鸟居及两张树冠，保持静态场景缓存，不逐帧加载、分配图片或创建实体 Node。
- 树木原有分布、避让道路及古印规则不变。鸟居仅将装饰位置由 `(0,-175)` 调为 `(0,-80)`，使完整轮廓出现在开局 HUD 下方；它仍无碰撞，不改变通行规则。
- 树冠与鸟居仍在背景装饰层，敌人、玩家、弹幕和判定点显示在其上方。标题原作角色精灵和全部菜单保留，移除程序飘落方块；标题底部衬底仅服务文字可读性，不作为场景美术。
- 本轮不替换地形地砖、古印中心标记、UI 框架、血条或全部环境光效，不增加地图内容。原图接入不代表美术已经全部完成。
- 同一 C# 项目用于 Windows/Web；不改核心玩法与版本号、不导出正式 EXE、不推送或部署。

## 本轮结果（2026-09-08）

- 原图逐字节一致性 4/4；原生构建无警告或错误，核心 29/29、UI／设置／存档／F3／绘制缓存回归及六组固定种子旅程通过。补齐更新日志新增字形后，字体覆盖 1195 字，未改字形轮廓。
- 原生改前：`artifacts/render-performance/scenery-before-20260908-171422`；修整后：`artifacts/render-performance/scenery-refined-20260908-172449`；最终字体导入后标题与日志截图：`artifacts/render-performance/scenery-final-ui-20260908-173532`。两角色原生颜色与动态批次检查：`artifacts/batch-render/20260908-173535`。
- 本地 Web 构建 `artifacts/web-builds/20260908-172621-470`；无隔离且无 SharedArrayBuffer 的五个场景检查通过，报告在 `scenery-verification/2026-09-08T09-28-58-668Z/report.json`。同构建两角色普通／异常默认色回归与 DPR 1/3 检查通过，已目视检查触控标题。
- 相同静态压力场景仍有 320 敌人、1600 弹丸和 400 掉落物，原生 draw calls 由 800 降至 254，Web 由此前 811 降至 265。仅说明去掉程序装饰后提交数减少，不是手机帧率验收。
- 对照图：`artifacts/scenery-art/title-comparison.png` 与 `battle-comparison.png`，左侧改前、右侧改后，来自实际原生截图，仅等比例最近邻缩小和拼接。首轮被 HUD 遮住鸟居的截图保留，不覆盖失败证据。
- 本轮没有正式导出、部署或完整重跑公网发布门禁；当前正式 alpha-0.1.3 成品和线上入口保持不变。

## 验证入口

- 原图核验：`python -X utf8 tools/rebirth/prepare_scenery_assets.py --check`。
- 原生构建与核心/UI：`tools/rebirth/verify.cmd`；场景截图使用 `tools/rebirth/verify_render.ps1`。
- Web：`build_web.cmd -Threadless` 后执行 `tools/platform/verify_scenery_art.cmd`，覆盖桌面、小窗口、触控标题及双角色静态战斗场景；保留现有颜色回归与 `verify_performance.cmd --compatible`。
- 截图和桌面压力场景不代替手机真机测试；视觉判断仍以实际画面为准，不把技术检查通过写成玩家已认可美术。
