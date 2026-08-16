using Godot;
using TouhouWuxiaSurvivor.Content;
using TouhouWuxiaSurvivor.Content.Characters;
using TouhouWuxiaSurvivor.Gameplay.Progression.Definitions;
using TouhouWuxiaSurvivor.Gameplay.Progression.Runtime;
using TouhouWuxiaSurvivor.Gameplay.SpellCards.Definitions;
using TouhouWuxiaSurvivor.Gameplay.SpellCards.Contracts;
using TouhouWuxiaSurvivor.Gameplay.SpellCards.Runtime;
using TouhouWuxiaSurvivor.Gameplay.SpellCards.Scaling;
using TouhouWuxiaSurvivor.Settings;

namespace TouhouWuxiaSurvivor.Tests.Integration;

/// <summary>
/// 验证奥义目录、触发策略、系数缩放与无主动按键契约，防止自动体系退化成充能技能。
/// </summary>
public partial class SpellCardBalanceTest : Node
{
    /// <summary>
    /// 运行全部纯数据断言，并使用明确退出码报告任一符卡策划契约回归。
    /// </summary>
    public override void _Ready()
    {
        try
        {
            VerifyCatalog();
            VerifyActivationCoverage();
            VerifyScaling();
            VerifyBuildRequirements();
            VerifyContentIsolation();
            VerifyNoActiveBindings();
            GD.Print("Spell card balance test passed.");
            GetTree().Quit();
        }
        catch (Exception exception)
        {
            GD.PushError(exception.ToString());
            GetTree().Quit(1);
        }
    }

    /// <summary>
    /// 确认二十部作品都有代表奥义，身份、角色归属、来源性质和缩放系数均可解析。
    /// </summary>
    private static void VerifyCatalog()
    {
        Require(SpellCardCatalog.All.Count == 51,
            "The all-work catalog must contain 51 representative spell cards.");
        Require(SpellCardCatalog.All.Select(card => card.Id).Distinct().Count() == 51 &&
            SpellCardCatalog.All.All(card =>
                CharacterCatalog.FindById(card.OwnerCharacterId) is not null),
            "Spell card IDs or stable owner identities are invalid.");
        Require(SpellCardCatalog.All.All(card =>
                card.Combat.IntervalScale > 0.0f && card.Combat.RangeScale > 0.0f &&
                card.Combat.DamageScale > 0.0f && card.Combat.TargetScale >= 0.0f &&
                card.Combat.ActivationThresholdScale > 0.0f &&
                card.Combat.DefenseScale >= 0.0f &&
                card.Combat.ProjectileSpeedScale > 0.0f),
            "Every spell card must define valid scaling dimensions.");
        Require(SpellCardCatalog.All.Count(card =>
                card.SourcePackId == ContentPackCatalog.Base.Id) == 6,
            "Base must own its complete six-card loadout.");
        foreach (ContentPackDefinition pack in ContentPackCatalog.All)
        {
            int expected = pack.Number == 6 ? 7 : 2;
            SpellCardDefinition[] cards = SpellCardCatalog.All.Where(
                card => card.SourcePackId == pack.Id).ToArray();
            Require(cards.Length == expected,
                $"Unexpected spell count for {pack.Id}: {cards.Length}");
            Require(cards.All(card => pack.Number <= 5
                    ? card.CanonLevel == SpellCardCanonLevel.AdaptedPreSpellCard &&
                        card.SourceNote.Contains("原作无符卡规则", StringComparison.Ordinal)
                    : card.CanonLevel == SpellCardCanonLevel.Official),
                $"Spell canon boundary is incorrect for {pack.Id}.");
        }
    }

    /// <summary>确认三类无资源自动触发均有正式内容，且旧灵力字段不会进入运行定义。</summary>
    private static void VerifyActivationCoverage()
    {
        var counts = SpellCardCatalog.All.GroupBy(card => card.ActivationKind)
            .ToDictionary(group => group.Key, group => group.Count());
        Require(counts.GetValueOrDefault(SpellCardActivationKind.Periodic) == 16 &&
            counts.GetValueOrDefault(SpellCardActivationKind.Crowd) == 25 &&
            counts.GetValueOrDefault(SpellCardActivationKind.OnDamaged) == 10,
            "Spell activation catalog did not preserve the 16/25/10 design matrix.");
    }

    /// <summary>
    /// 确认解析器只消费角色基础属性，并让攻势、范围、弹速、目标与周天保持单调成长。
    /// </summary>
    private static void VerifyScaling()
    {
        SpellCardDefinition card = SpellCardCatalog.All.Single(
            item => item.FullName == "灵符「梦想封印」");
        var baseline = new SpellCardBaseAttributes(
            10.0f, 0.18f, 460.0f, 360.0f, 1.0f, 5.25f, 6, 18.0f);
        var upgraded = new SpellCardBaseAttributes(
            30.0f, 0.12f, 690.0f, 540.0f, 1.5f, 3.5f, 9, 18.0f);
        ResolvedSpellCardCombat before = SpellCardScalingResolver.Resolve(
            card.Combat, baseline);
        ResolvedSpellCardCombat after = SpellCardScalingResolver.Resolve(
            card.Combat, upgraded);
        Require(before.Damage == 7 && Mathf.IsEqualApprox(before.EffectRange, 560.0f) &&
            before.TargetCount == 5 && before.ActivationThreshold == 3 &&
            Mathf.IsEqualApprox(before.IntervalSeconds, 4.0f),
            "Schema v2 did not preserve the reference spell behavior.");
        Require(after.Damage > before.Damage && after.EffectRange > before.EffectRange &&
            after.TargetCount > before.TargetCount && after.ProjectileSpeed > before.ProjectileSpeed &&
            after.IntervalSeconds < before.IntervalSeconds,
            "Spell card scaling did not follow upgraded base attributes.");
    }

    /// <summary>
    /// 确认数据驱动奥义在前置重数前不可选，达到要求后依次悟得、化境并在二重圆满。
    /// </summary>
    private static void VerifyBuildRequirements()
    {
        var build = new RunBuildState();
        SpellCardDefinition card = SpellCardCatalog.All[0];
        RunUpgradeDefinition spell = RunUpgradeCatalog.FindById(card.UnlockUpgradeId)!;
        RunUpgradeDefinition prerequisite = RunUpgradeCatalog.FindById(
            card.PrerequisiteUpgradeId)!;
        Require(!build.CanUpgrade(spell), "Spell card appeared before its prerequisite.");
        for (int rank = 0; rank < card.MinimumRank; rank++)
        {
            Require(build.Apply(prerequisite), "Could not apply a spell prerequisite.");
        }

        Require(build.Apply(spell) && build.GetRank(spell.Id) == 1 &&
            build.CanUpgrade(spell),
            "A newly learned spell card could not reach mastery rank.");
        Require(build.Apply(spell) && build.GetRank(spell.Id) == 2 &&
            !build.CanUpgrade(spell),
            "A mastered spell card did not stop at its two-rank cap.");
    }

    /// <summary>
    /// 确认未勾选任何作品时符卡池为空，启用单一作品时只开放该作品的传承。
    /// </summary>
    private static void VerifyContentIsolation()
    {
        Require(SpellCardCatalog.GetEnabled(ContentPackSelection.BaseOnly).Count == 6 &&
            SpellCardCatalog.GetEnabled(ContentPackSelection.BaseOnly).All(card =>
                card.SourcePackId == ContentPackCatalog.Base.Id),
            "Base-only runs must expose the complete permanent 4+2 spell loadout.");
        ContentPackDefinition th06 = ContentPackCatalog.All.Single(pack => pack.Number == 6);
        var selection = new ContentPackSelection([th06.Id]);
        Require(SpellCardCatalog.GetEnabled(selection).Count == 13 &&
            SpellCardCatalog.GetEnabled(selection).Count(card =>
                card.SourcePackId == ContentPackCatalog.Base.Id) == 6 &&
            SpellCardCatalog.GetEnabled(selection).Count(card => card.SourcePackId == th06.Id) == 7,
            "Single-pack spell filtering is incorrect.");
    }

    /// <summary>
    /// 确认设置目录没有施放或切换符卡动作，玩家只承担移动与构筑选择。
    /// </summary>
    private static void VerifyNoActiveBindings() => Require(
        InputActionCatalog.All.All(action =>
            action.Id != "cast_spell_card" && action.Id != "cycle_spell_card"),
        "Spell cards must not add active input actions.");

    /// <summary>
    /// 将策划契约失败转换为包含具体原因的测试异常。
    /// </summary>
    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
