# alpha-0.0.5 与 alpha-0.0.6：实际功能对比

核对日期：2026-09-06。

alpha-0.0.8 后续增量：改为每人三条专属能力与四类公共修习，取消共同飞剑/雷链池；灵梦御札、阴阳玉、驻留封魔阵与梦想封印，魔理沙星弹、星尘与持续 Master Spark。每人攻击轨道由四条降为三条，换取角色机制差异，不宣称“所有维度都增加”。原型的地图与终局框架保持，详情见该版日志。

本报告固定对比 alpha-0.0.5 源码快照与 alpha-0.0.6 交付，不随新版改写历史结论。后续 alpha-0.0.7 恢复了 E 构筑查看、Esc/P 暂停、音量滑块与游戏内历史日志，并改进三选一信息与返回逻辑；具体增量见 CHANGELOG.md。本轮未恢复此表列出的其余系统。

## 对比基线与结论

- 旧侧为重写前源码快照 `d229b36`，项目版本标识 `alpha-0.0.5`；新侧为导出交付提交 `41a893a`，版本 `alpha-0.0.6`。
- 旧 EXE 生成于 2026-08-16，此后仍有未导出的源码修改。本文以保存下来的源码运行链为主，不声称旧 EXE 已包含之后每一项修补，也没有重新完整试玩旧版。
- 已验证旧 `src/`、`content/`、`assets/`、`tests/integration/` 相对快照没有变动，可以直接检查；主项目只编译 `game/**/*.cs`。
- **新版不是旧版的全面增强版，而是大幅收缩内容范围、重建短局战斗循环的独立原型。** 真正的新增集中在主动生存操作、擦弹蓄势和古印目标；旧版的探索广度、内容体系、养成与工具界面明显更多。

## 逐项对比

| 维度 | 重写前版本 | 新版 | 评价 |
| --- | --- | --- | --- |
| 基础方向 | 东方 + 武侠 + 幸存者，Godot C# | 同样的题材与技术语言 | 保留，不是新增 |
| 核心输入 | 移动与构筑选择，自动战斗 | 增加 Space 闪身、Shift 慢移与小判定显示 | 实质新增，操作要求也提高 |
| 擦弹与爆发 | 没有当前这套逐弹擦弹蓄势链 | 每弹只计一次擦弹，满剑意自动攻击、清弹、吸取 | 实质新增 |
| 局内目标 | 无限世界探索、动态压力、最终角色 Boss | 三处固定古印停留净化；固定时点终局 | 从广域探索改为有限场景目标 |
| 世界 | 确定性区块生成、流送、浮动原点、多群系/结构 | 3000×2200 固定战场和装饰性场景 | 明显收缩 |
| 地图界面 | 可拖拽、缩放、回中并显示探索信息的大地图 | 只显示玩家、古印和 Boss 的简易小地图 | 明显简化 |
| 内容包 | 本体 + TH01–TH20 的独立可选清单、版本/能力/激活隔离 | 不加载旧内容包，运行规则写在新 C# 代码中 | 未迁移，扩展方式退回代码修改 |
| 可选角色 | 清单与角色目录登记 132 个身份，按启用来源筛选 | 固定灵梦、魔理沙两名行者 | 内容明显减少；旧数量不等于全部做精 |
| 敌人与 Boss | 本体九类敌人及作品包生态，角色 Boss/符卡目录 | 毛玉、妖精、冲锋、精英、Boss 五种行为类别；一名终局来客 | 多样性减少，战斗脚本更集中 |
| 武学与升级 | 六路有限修行、六路无尽延续、亲和权重、阵形特化、独立奥义 | 四门五重武学 + 四项三重通用修炼 + 恢复选项 | 重新设计，不是保留原体系后加四武器 |
| 符卡表现 | 51 个定义；原作身份、来源、弹型、姿态和复合时序解析 | 通用御剑/绕身/爆破/雷链与自定义 Boss 环形、扇形弹 | 旧演出系统未迁移；东方内容辨识度存在下降风险 |
| 升级分配 | 根据已选亲和等规则加权，区分精进/成势/补缺 | 从未满级项目中不放回随机抽取，接近满级时加入恢复 | 算法简化；没有保留旧亲和引导 |
| 自动瞄准 | 求目标匀速下最早有效拦截点，失败回退直瞄 | 用距离/弹速估计提前量 | 保留预测概念，但求解精度简化 |
| 难度推进 | 最近 30 秒 K/S 达九成才换档；同种敌人基础属性固定 | 按时间增加刷新量；部分敌人属性随生成时刻成长；240 秒 Boss | 从表现驱动改为时间驱动，不是同规则优化 |
| 数量约束 | 不设敌人存活软上限 | 普通刷新阈值 320，弹丸集合 1600，掉落集合 700 | 有利于约束规模，但旧规则已改变；精英/Boss独立生成 |
| 结局 | 击破最终 Boss 可结算或保留构筑进入无尽 | 胜败结算后重开或返回标题 | 闭环原本就有；无尽被移除 |
| 局外成长 | 货币、累计收益、永久修行与局内加成 | 仅偏好、最佳退治/擦弹、通关次数与最快胜利 | 数值成长移除；单局更独立但长期目标减少 |
| 图鉴/状态页 | 六类图鉴、来源信息、详细构筑/属性页 | 暂停页的简要构筑和数据 | 未迁移，不是界面换皮 |
| 设置 | 主/音乐/音效音量、双键位、窗口、分辨率、VSync、FPS | 音乐/音效开关、减少震屏、F11 | 明显减少 |
| 版本日志 | 完整 CHANGELOG + 游戏内浏览页 | 完整仓库日志保留；未接游戏内浏览入口 | 文件没有丢，但游戏内功能缺失；现已补成品旁日志 |
| 技术架构 | ECS/OOP 混合，内容契约和运行时目录 | 独立纯 .NET 模拟 + Godot 自绘/UI | 更小更直接，但不是扩展性与性能全面升级 |
| 测试/诊断 | 80 个旧集成测试源码文件、世界验证工具、运行遥测/诊断界面 | 19 项新核心测试、界面/存档冒烟、自动流程与截图 | 新范围得到验证，不覆盖被移除的旧系统；无新旧同机性能 A/B |
| 美术与分辨率 | 640×360 逻辑视口、既有像素 UI 和原作映射 | 1280×720、自绘标题/场景/HUD/卡片，仍复用已有精灵、地砖、音频 | 表现路线改变，不能声称资产从零全新制作 |
| 交付 | 单文件 Windows EXE，PCK/.NET 内嵌 | 同样的单文件口径 | 保留，不是技术升级；两者都不是 Web 构建 |

## 哪些判断仍不能下结论

- “更好玩”：六次新版本导航机器人通关只证明一套策略能通关；没有旧版同条件真人 A/B，不能由此证明新作更好玩。
- “更快”：426 个旧源文件变为 20 个新源文件不能证明性能更高；场景范围、实体数量和内容系统均不同，需匹配负载后测量。
- “旧版 132 人物/51 符卡都是完成品”：清单显示 20 个可选包里，TH06 为 development，其余 19 个为 inventory。应区分登记、接入和完成度。
- “删除的代码无法找回”：旧源码与数据留在原位和 Git 中，但恢复其运行功能仍需设计和移植，不是勾一个开关就能接入新版。

## 建议的产品决策

若目标是“原游戏变得更好”，而不只是“换一个更小的新游戏”，下一轮应先明确恢复哪些旧能力。
建议把地图/群系探索、基础内容与图鉴、按键设置、游戏内更新日志列入需要决定的清单，再评估是否保留新版闪身、擦弹与古印。
不要为了做 Web 再未经确认删一轮功能，也不要把全部旧库存机械搬回当作质量提升。

## 主要代码证据

- 旧地图/世界：`src/world/generation/WorldGenerator.cs`、`src/world/streaming/ChunkStreamer.cs`、`src/ui/map/`。
- 旧内容与角色：`content/base/pack.json`、`content/packs/*/pack.json`、`src/content/characters/CharacterCatalog.cs`、`src/ui/content/ContentPackSelectionPanel.cs`。
- 旧武学与符卡：`src/gameplay/progression/definitions/BaseRunUpgradeFactory.cs`、`src/gameplay/progression/runtime/RunOfferGenerator.cs`、`src/gameplay/spellcards/definitions/SpellCardCatalog.cs`。
- 旧压力与预判：`src/gameplay/pacing/AdaptiveRunPacingState.cs`、`src/combat/targeting/InterceptAimSolver.cs`。
- 旧成长与设置：`src/gameplay/meta/persistence/ProgressionProfileData.cs`、`src/gameplay/meta/runtime/ProfileRunBonuses.cs`、`src/settings/GameSettingsData.cs`。
- 旧结局与日志：`src/ui/completion/RunCompletionOverlay.cs`、`src/ui/changelog/ChangelogPanel.cs`、`src/ui/menu/MainMenu.cs`。
- 新规则：`game/core/RunState.cs`、`game/core/RunEncounters.cs`、`game/core/RunProgression.cs`、`game/core/RunWeapons.cs`、`game/core/ArtCatalog.cs`。
- 新界面与存档：`game/presentation/GameRoot.cs`、`game/presentation/MenuScreens.cs`、`game/presentation/RunScreens.cs`、`game/presentation/ProfileStore.cs`。
- 编译与交付边界：`TouhouWuxiaSurvivor.csproj`、`export_presets.cfg`、`docs/rebirth_validation.md`。
