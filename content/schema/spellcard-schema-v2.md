# Spell Card Schema v2

奥义不消耗灵力，也没有共享充能条。每张奥义拥有独立恢复周期，并可选择周期、敌群或受击三种自动运转方式。

## Runtime Contract

- `activation`：`periodic` 到期自动施展；`crowd` 到期后等待敌群条件；`on_damaged` 到期后等待玩家受击信号。

- `interval_scale`：角色奥义基础周天的倍率，必须大于零。
- `range_scale`：当前实效索敌范围的倍率，必须大于零。
- `damage_scale`：当前实效攻击力的倍率，必须大于零。
- `target_scale`：角色奥义承载数量的倍率；范围与护身效果允许为零。
- `activation_threshold_scale`：角色奥义承载数量映射到敌群触发门槛的倍率，必须大于零；现有内容以 `0.5` 保留基准三敌门槛并随角色成长。
- `defense_scale`：角色基础受击保护时间的倍率；非护身效果允许为零。
- `projectile_speed_scale`：当前实效弹速的倍率，必须大于零。
- `impact_range_scale`：以最终施展范围为基准的命中半径倍率；不使用时允许为零。
- `travel_duration_scale`：以 `最终范围 / 最终弹速` 为基准的存续时间倍率，必须大于零。
- `spawn_distance_scale`：以武器出弹距离为基准的生成距离倍率，必须大于零。

最终数值只在施展瞬间由统一解析器计算。`activation` 只决定时机，不改变效果倍率。内容包不得保存 `power_cost`、`cooldown_seconds`、`effect_range`、`damage`、`target_count`、`defense_seconds` 或旧 `trigger`。

## Ownership

内容包只提供横向选择项及其倍率。角色基础属性、局内构筑倍率、定时调度、战斗效果与界面投影分别由独立模块负责，任何 DLC 都不得建立高于其他作品的纵向成长层级。

## Validation

在仓库根目录运行：

```text
node tools/content/migrate_spellcard_schema_v2.mjs
```

检查器会验证本体与 20 个内容包合计 46 张奥义、字段边界、旧字段禁入及 UTF-8 without BOM。
