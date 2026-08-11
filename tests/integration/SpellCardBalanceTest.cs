using Godot;
using TouhouWuxiaSurvivor.Content;
using TouhouWuxiaSurvivor.Content.Characters;
using TouhouWuxiaSurvivor.Gameplay.Progression.Definitions;
using TouhouWuxiaSurvivor.Gameplay.Progression.Runtime;
using TouhouWuxiaSurvivor.Gameplay.SpellCards.Definitions;
using TouhouWuxiaSurvivor.Gameplay.SpellCards.Runtime;
using TouhouWuxiaSurvivor.Settings;

namespace TouhouWuxiaSurvivor.Tests.Integration;

/// <summary>
/// 验证符卡目录、前置构筑、灵力边界与无主动按键契约，防止奥义退化成额外操作负担。
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
            VerifyPowerState();
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
    /// 确认二十部作品都有代表符卡，身份、角色归属、来源性质和灵力数值均可解析。
    /// </summary>
    private static void VerifyCatalog()
    {
        Require(SpellCardCatalog.All.Count == 42,
            "The all-work catalog must contain 42 representative spell cards.");
        Require(SpellCardCatalog.All.Select(card => card.Id).Distinct().Count() == 42 &&
            SpellCardCatalog.All.All(card =>
                CharacterCatalog.FindById(card.OwnerCharacterId) is not null),
            "Spell card IDs or stable owner identities are invalid.");
        Require(SpellCardCatalog.All.All(card =>
                card.Combat.PowerCost is > 0 and <= SpellPowerState.MaximumPower),
            "Every spell card cost must fit the shared power pool.");
        foreach (ContentPackDefinition pack in ContentPackCatalog.All)
        {
            int expected = pack.Number == 6 ? 4 : 2;
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

    /// <summary>
    /// 确认灵息换算会裁剪到上限，扣费失败不改变状态，成功扣费保留准确余量。
    /// </summary>
    private static void VerifyPowerState()
    {
        var power = new SpellPowerState();
        Require(power.GainFromSpirit(3) == 12 && power.CurrentPower == 12,
            "Spirit-to-power conversion is incorrect.");
        Require(!power.TrySpend(70) && power.CurrentPower == 12,
            "Failed spending changed spell power.");
        power.GainFromSpirit(100);
        Require(power.CurrentPower == 100 && power.TrySpend(70) && power.CurrentPower == 30,
            "Power cap or successful spending is incorrect.");
    }

    /// <summary>
    /// 确认数据驱动符卡在前置重数前不可选，达到要求后可且只能悟得一次。
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

        Require(build.Apply(spell) && !build.CanUpgrade(spell),
            "Spell card did not obey its one-rank cap.");
    }

    /// <summary>
    /// 确认未勾选任何作品时符卡池为空，启用单一作品时只开放该作品的传承。
    /// </summary>
    private static void VerifyContentIsolation()
    {
        Require(SpellCardCatalog.GetEnabled(ContentPackSelection.BaseOnly).Count == 0,
            "Base-only runs leaked official spell cards.");
        ContentPackDefinition th06 = ContentPackCatalog.All.Single(pack => pack.Number == 6);
        var selection = new ContentPackSelection([th06.Id]);
        Require(SpellCardCatalog.GetEnabled(selection).Count == 4 &&
            SpellCardCatalog.GetEnabled(selection).All(card => card.SourcePackId == th06.Id),
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
