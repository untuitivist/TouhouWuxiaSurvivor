namespace TouhouWuxiaSurvivor.Gameplay.Progression.Definitions;

/// <summary>
/// 建立本体六项基础修行与六项无尽延续，并为每项基础修行声明同预算、互斥的自动战斗特化。
/// </summary>
public static class BaseRunUpgradeFactory
{
    /// <summary>
    /// 按稳定顺序返回完整本体目录；标签描述机制而非角色身份，所有内容包共用同一语义。
    /// </summary>
    public static IReadOnlyList<RunUpgradeDefinition> CreateAll() =>
    [
        new("needle_damage", "封魔针法", RunUpgradeKind.NeedleDamage,
            RunUpgradeCategory.MartialArt, 5, "攻势 +10%",
            affinities: [RunUpgradeAffinity.Force, RunUpgradeAffinity.Precision],
            specializations: CreatePair(
                "needle_piercing", "破甲针意", "弹丸贯穿一名额外敌人",
                [RunUpgradeAffinity.Force], RunSpecializationEffect.ProjectilePierce, 1.0f,
                "needle_rain", "散华针意", "每轮追加两枚散射弹",
                [RunUpgradeAffinity.Precision, RunUpgradeAffinity.Formation],
                RunSpecializationEffect.ExtraProjectiles, 2.0f)),
        new("hakurei_breathing", "博丽呼吸法", RunUpgradeKind.FireRate,
            RunUpgradeCategory.InnerArt, 5, "射击速度 +9%",
            affinities: [RunUpgradeAffinity.Swiftness, RunUpgradeAffinity.Guard],
            specializations: CreatePair(
                "breathing_swift", "疾息周天", "连续射击蓄势，射速最多 +20%",
                [RunUpgradeAffinity.Swiftness],
                RunSpecializationEffect.ContinuousFireMomentum, 0.20f,
                "breathing_focus", "凝神周天", "停步凝神，攻势最多 +22%",
                [RunUpgradeAffinity.Guard, RunUpgradeAffinity.Force],
                RunSpecializationEffect.StationaryFocus, 0.22f)),
        new("tengu_step", "天狗步", RunUpgradeKind.MoveSpeed,
            RunUpgradeCategory.InnerArt, 5, "移动速度 +7%",
            affinities: [RunUpgradeAffinity.Swiftness, RunUpgradeAffinity.Guard],
            specializations: CreatePair(
                "tengu_gale", "逐风身", "连续移动蓄势，移速最多 +18%",
                [RunUpgradeAffinity.Swiftness],
                RunSpecializationEffect.MovementMomentum, 0.18f,
                "tengu_awareness", "听风身", "索敌范围额外 +22%",
                [RunUpgradeAffinity.Guard, RunUpgradeAffinity.Precision],
                RunSpecializationEffect.TargetRange, 0.22f)),
        new("soul_seeking", "追魂诀", RunUpgradeKind.TargetRange,
            RunUpgradeCategory.MartialArt, 5, "索敌范围 +8%",
            affinities: [RunUpgradeAffinity.Precision, RunUpgradeAffinity.Formation],
            specializations: CreatePair(
                "soul_lock", "锁魂印", "索敌范围额外 +22%",
                [RunUpgradeAffinity.Precision], RunSpecializationEffect.TargetRange, 0.22f,
                "soul_net", "天罗印", "每轮追加两枚结阵弹",
                [RunUpgradeAffinity.Formation, RunUpgradeAffinity.Swiftness],
                RunSpecializationEffect.ExtraProjectiles, 2.0f)),
        new("wind_riding", "御风诀", RunUpgradeKind.ProjectileSpeed,
            RunUpgradeCategory.InnerArt, 5, "弹丸速度 +8%",
            affinities: [RunUpgradeAffinity.Swiftness, RunUpgradeAffinity.Force],
            specializations: CreatePair(
                "wind_breaker", "破空劲", "弹丸速度额外 +24%",
                [RunUpgradeAffinity.Force], RunSpecializationEffect.ProjectileSpeed, 0.24f,
                "wind_thunder", "奔雷劲", "弹幕改为正反旋转阵",
                [RunUpgradeAffinity.Swiftness], RunSpecializationEffect.SpiralPattern, 1.0f)),
        new("spirit_gathering", "聚灵诀", RunUpgradeKind.SpiritAttraction,
            RunUpgradeCategory.InnerArt, 5, "灵息吸引范围 +18%",
            affinities: [RunUpgradeAffinity.Formation, RunUpgradeAffinity.Guard],
            specializations: CreatePair(
                "spirit_tide", "纳海势", "灵息价值额外 +25%",
                [RunUpgradeAffinity.Formation],
                RunSpecializationEffect.SpiritYield, 0.25f,
                "spirit_flow", "流云势", "拾取灵息后短时移速 +18%",
                [RunUpgradeAffinity.Guard, RunUpgradeAffinity.Swiftness],
                RunSpecializationEffect.SpiritFlowMomentum, 0.18f)),
        CreateEndless("endless_damage", "真元淬锋", RunUpgradeKind.EndlessDamage,
            "弹丸伤害持续提高", "needle_damage",
            [RunUpgradeAffinity.Force, RunUpgradeAffinity.Precision]),
        CreateEndless("endless_fire_rate", "周天吐纳", RunUpgradeKind.EndlessFireRate,
            "射击速度以递减幅度持续提高", "hakurei_breathing",
            [RunUpgradeAffinity.Swiftness, RunUpgradeAffinity.Guard]),
        CreateEndless("endless_move_speed", "无相身法", RunUpgradeKind.EndlessMoveSpeed,
            "移动速度以递减幅度持续提高", "tengu_step",
            [RunUpgradeAffinity.Swiftness, RunUpgradeAffinity.Guard]),
        CreateEndless("endless_target_range", "神识外放", RunUpgradeKind.EndlessTargetRange,
            "索敌范围以递减幅度持续提高", "soul_seeking",
            [RunUpgradeAffinity.Precision, RunUpgradeAffinity.Formation]),
        CreateEndless("endless_projectile_speed", "御气破空",
            RunUpgradeKind.EndlessProjectileSpeed,
            "弹丸速度以递减幅度持续提高", "wind_riding",
            [RunUpgradeAffinity.Swiftness, RunUpgradeAffinity.Force]),
        CreateEndless("endless_spirit_attraction", "纳灵归元",
            RunUpgradeKind.EndlessSpiritAttraction,
            "灵息吸引范围以递减幅度持续提高", "spirit_gathering",
            [RunUpgradeAffinity.Formation, RunUpgradeAffinity.Guard]),
    ];

    /// <summary>
    /// 建立必须先练满对应基础修行的无尽延续，基础权重与所有其他来源保持一致。
    /// </summary>
    private static RunUpgradeDefinition CreateEndless(
        string id,
        string name,
        RunUpgradeKind kind,
        string effectText,
        string prerequisiteId,
        RunUpgradeAffinity[] affinities) => new(
            id, name, kind, RunUpgradeCategory.InnerArt, int.MaxValue, effectText,
            new RunUpgradeRequirement(prerequisiteId, 5), isRepeatable: true,
            affinities: affinities);

    /// <summary>
    /// 建立境界八、基础三重解锁的双分支，并显式写入双向互斥 ID 供数据审计。
    /// </summary>
    private static IReadOnlyList<RunUpgradeSpecialization> CreatePair(
        string firstId,
        string firstName,
        string firstEffectText,
        RunUpgradeAffinity[] firstAffinities,
        RunSpecializationEffect firstEffect,
        float firstValue,
        string secondId,
        string secondName,
        string secondEffectText,
        RunUpgradeAffinity[] secondAffinities,
        RunSpecializationEffect secondEffect,
        float secondValue) =>
    [
        new(firstId, firstName, firstEffectText, 8, 3, firstAffinities,
            firstEffect, firstValue, [secondId]),
        new(secondId, secondName, secondEffectText, 8, 3, secondAffinities,
            secondEffect, secondValue, [firstId]),
    ];
}
