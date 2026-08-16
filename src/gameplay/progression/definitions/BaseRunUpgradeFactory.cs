namespace TouhouWuxiaSurvivor.Gameplay.Progression.Definitions;

/// <summary>
/// 建立本体六项二至四重强反馈修行与无尽延续，并声明可并行取得的自动战斗特化。
/// </summary>
public static class BaseRunUpgradeFactory
{
    /// <summary>
    /// 按稳定顺序返回完整本体目录；标签描述机制而非角色身份，所有内容包共用同一语义。
    /// </summary>
    public static IReadOnlyList<RunUpgradeDefinition> CreateAll() =>
    [
        new("needle_damage", "封魔针法", RunUpgradeKind.NeedleDamage,
            RunUpgradeCategory.MartialArt, 4, "普通弹与弹幕弹单弹伤害 +35%",
            affinities: [RunUpgradeAffinity.Force, RunUpgradeAffinity.Precision],
            specializations: CreatePair(
                "needle_piercing", "破甲针意", "弹丸贯穿一名额外敌人",
                [RunUpgradeAffinity.Force], RunSpecializationEffect.ProjectilePierce, 1.0f,
                "needle_rain", "散华针意", "普通弹额外 +2 发",
                [RunUpgradeAffinity.Precision, RunUpgradeAffinity.Formation],
                RunSpecializationEffect.OrdinaryProjectiles, 2.0f)),
        new("hakurei_breathing", "博丽呼吸法", RunUpgradeKind.FireRate,
            RunUpgradeCategory.InnerArt, 4, "射击速度 +18%",
            affinities: [RunUpgradeAffinity.Swiftness, RunUpgradeAffinity.Guard],
            specializations: CreatePair(
                "breathing_swift", "疾息周天", "连续射击蓄势，射速最多 +20%",
                [RunUpgradeAffinity.Swiftness],
                RunSpecializationEffect.ContinuousFireMomentum, 0.20f,
                "breathing_focus", "凝神周天", "停步凝神，攻势最多 +22%",
                [RunUpgradeAffinity.Guard, RunUpgradeAffinity.Force],
                RunSpecializationEffect.StationaryFocus, 0.22f)),
        new("tengu_step", "天狗步", RunUpgradeKind.MoveSpeed,
            RunUpgradeCategory.InnerArt, 3, "移动速度 +15%",
            affinities: [RunUpgradeAffinity.Swiftness, RunUpgradeAffinity.Guard],
            specializations: CreatePair(
                "tengu_gale", "逐风身", "连续移动蓄势，移速最多 +18%",
                [RunUpgradeAffinity.Swiftness],
                RunSpecializationEffect.MovementMomentum, 0.18f,
                "tengu_awareness", "听风身", "索敌范围额外 +22%",
                [RunUpgradeAffinity.Guard, RunUpgradeAffinity.Precision],
                RunSpecializationEffect.TargetRange, 0.22f)),
        new("soul_seeking", "追魂诀", RunUpgradeKind.TargetRange,
            RunUpgradeCategory.MartialArt, 3, "预判普通弹 +1 发，索敌与作用范围 +25%",
            affinities: [RunUpgradeAffinity.Precision, RunUpgradeAffinity.Formation],
            specializations: CreatePair(
                "soul_lock", "锁魂印", "索敌范围额外 +22%",
                [RunUpgradeAffinity.Precision], RunSpecializationEffect.TargetRange, 0.22f,
                "soul_net", "天罗印", "普通弹改为两翼预测收束阵",
                [RunUpgradeAffinity.Formation, RunUpgradeAffinity.Swiftness],
                RunSpecializationEffect.ConvergingOrdinary, 1.0f)),
        new("wind_riding", "天罗弹阵", RunUpgradeKind.ProjectileSpeed,
            RunUpgradeCategory.MartialArt, 3, "自机中心辐射弹幕 +4 发，两类弹丸弹速 +12%",
            affinities: [RunUpgradeAffinity.Swiftness, RunUpgradeAffinity.Force],
            specializations: CreatePair(
                "wind_breaker", "双仪旋阵", "辐射弹幕改为二重螺旋",
                [RunUpgradeAffinity.Force], RunSpecializationEffect.BarrageSpiralArms, 1.0f,
                "wind_thunder", "三才叠阵", "单修为三重；与双仪同修为四重螺旋",
                [RunUpgradeAffinity.Swiftness],
                RunSpecializationEffect.BarrageSpiralArms, 2.0f)),
        new("spirit_gathering", "聚灵诀", RunUpgradeKind.SpiritAttraction,
            RunUpgradeCategory.InnerArt, 3, "吸引范围 +50%，灵息收益 +10%",
            affinities: [RunUpgradeAffinity.Formation, RunUpgradeAffinity.Guard],
            specializations: CreatePair(
                "spirit_tide", "纳海势", "灵息价值额外 +25%",
                [RunUpgradeAffinity.Formation],
                RunSpecializationEffect.SpiritYield, 0.25f,
                "spirit_flow", "流云势", "拾取灵息后短时移速 +18%",
                [RunUpgradeAffinity.Guard, RunUpgradeAffinity.Swiftness],
                RunSpecializationEffect.SpiritFlowMomentum, 0.18f)),
        CreateEndless("endless_damage", "真元淬锋", RunUpgradeKind.EndlessDamage,
            "两类弹丸的共享单弹伤害持续提高", "needle_damage", 4,
            [RunUpgradeAffinity.Force, RunUpgradeAffinity.Precision]),
        CreateEndless("endless_fire_rate", "周天吐纳", RunUpgradeKind.EndlessFireRate,
            "射击速度以递减幅度持续提高", "hakurei_breathing", 4,
            [RunUpgradeAffinity.Swiftness, RunUpgradeAffinity.Guard]),
        CreateEndless("endless_move_speed", "无相身法", RunUpgradeKind.EndlessMoveSpeed,
            "移动速度以递减幅度持续提高", "tengu_step", 3,
            [RunUpgradeAffinity.Swiftness, RunUpgradeAffinity.Guard]),
        CreateEndless("endless_target_range", "神识外放", RunUpgradeKind.EndlessTargetRange,
            "索敌范围以递减幅度持续提高", "soul_seeking", 3,
            [RunUpgradeAffinity.Precision, RunUpgradeAffinity.Formation]),
        CreateEndless("endless_projectile_speed", "御气破空",
            RunUpgradeKind.EndlessProjectileSpeed,
            "弹丸速度以递减幅度持续提高", "wind_riding", 3,
            [RunUpgradeAffinity.Swiftness, RunUpgradeAffinity.Force]),
        CreateEndless("endless_spirit_attraction", "纳灵归元",
            RunUpgradeKind.EndlessSpiritAttraction,
            "灵息吸引范围以递减幅度持续提高", "spirit_gathering", 3,
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
        int prerequisiteRank,
        RunUpgradeAffinity[] affinities) => new(
            id, name, kind, RunUpgradeCategory.InnerArt, int.MaxValue, effectText,
            new RunUpgradeRequirement(prerequisiteId, prerequisiteRank), isRepeatable: true,
            affinities: affinities);

    /// <summary>
    /// 建立境界四、基础二重解锁的双分支；当前版本不写互斥，玩家可横向组合两种行为。
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
        new(firstId, firstName, firstEffectText, 4, 2, firstAffinities,
            firstEffect, firstValue),
        new(secondId, secondName, secondEffectText, 4, 2, secondAffinities,
            secondEffect, secondValue),
    ];
}
