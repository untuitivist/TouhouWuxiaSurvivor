using Godot;
using TouhouWuxiaSurvivor.Gameplay.Progression.Runtime;

namespace TouhouWuxiaSurvivor.Gameplay.Progression.Definitions;

/// <summary>
/// 集中声明本体首批局内升级，并从未满重项目中抽取互不重复的升级选项。
/// </summary>
public static class RunUpgradeCatalog
{
    public static IReadOnlyList<RunUpgradeDefinition> All { get; } =
    [
        new("needle_damage", "封魔针法", RunUpgradeKind.NeedleDamage,
            RunUpgradeCategory.MartialArt, 5, "弹丸伤害 +1"),
        new("hakurei_breathing", "博丽呼吸法", RunUpgradeKind.FireRate,
            RunUpgradeCategory.InnerArt, 5, "射击速度 +12%"),
        new("tengu_step", "天狗步", RunUpgradeKind.MoveSpeed,
            RunUpgradeCategory.InnerArt, 5, "移动速度 +8%"),
        new("soul_seeking", "追魂诀", RunUpgradeKind.TargetRange,
            RunUpgradeCategory.MartialArt, 5, "索敌范围 +10%"),
        new("wind_riding", "御风诀", RunUpgradeKind.ProjectileSpeed,
            RunUpgradeCategory.InnerArt, 5, "弹丸速度 +12%"),
        new("spirit_gathering", "聚灵诀", RunUpgradeKind.SpiritAttraction,
            RunUpgradeCategory.InnerArt, 5, "灵息吸引范围 +25%"),
        new("fantasy_seal", "灵符「梦想封印」", RunUpgradeKind.FantasySeal,
            RunUpgradeCategory.SpellCard, 1,
            "灵力满时自动放出八枚追踪灵玉",
            new RunUpgradeRequirement(RunUpgradeKind.NeedleDamage, 2)),
        new("evil_sealing_circle", "梦符「封魔阵」", RunUpgradeKind.EvilSealingCircle,
            RunUpgradeCategory.SpellCard, 1,
            "近身受围时自动展开伤敌护身结界",
            new RunUpgradeRequirement(RunUpgradeKind.SpiritAttraction, 2)),
    ];

    /// <summary>
    /// 使用独立随机源洗牌可升级项目并返回指定数量，已满重项目不会占据选择位。
    /// </summary>
    public static IReadOnlyList<RunUpgradeDefinition> CreateOffer(
        RandomNumberGenerator random,
        RunBuildState build,
        int choiceCount = 3)
    {
        var candidates = All.Where(build.CanUpgrade).ToList();
        for (int index = candidates.Count - 1; index > 0; index--)
        {
            int swapIndex = random.RandiRange(0, index);
            (candidates[index], candidates[swapIndex]) =
                (candidates[swapIndex], candidates[index]);
        }

        return candidates.Take(Math.Max(0, choiceCount)).ToArray();
    }
}
