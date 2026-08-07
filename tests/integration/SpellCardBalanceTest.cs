using Godot;
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
    /// 确认两张灵梦符卡具有不同 ID、效果和合理的共享灵力消耗。
    /// </summary>
    private static void VerifyCatalog()
    {
        Require(SpellCardCatalog.ReimuLoadout.Count == 2,
            "Reimu must have two implemented spell cards.");
        Require(SpellCardCatalog.ReimuLoadout.Select(card => card.Id).Distinct().Count() == 2 &&
            SpellCardCatalog.ReimuLoadout.Select(card => card.EffectKind).Distinct().Count() == 2,
            "Spell card IDs and effect kinds must be unique.");
        Require(SpellCardCatalog.ReimuLoadout.All(card =>
                card.Combat.PowerCost is > 0 and <= SpellPowerState.MaximumPower),
            "Every spell card cost must fit the shared power pool.");
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
    /// 确认两张符卡在前置二重前不会出现，满足条件后可且只能悟得一次。
    /// </summary>
    private static void VerifyBuildRequirements()
    {
        var build = new RunBuildState();
        RunUpgradeDefinition needle = FindUpgrade(RunUpgradeKind.NeedleDamage);
        RunUpgradeDefinition attraction = FindUpgrade(RunUpgradeKind.SpiritAttraction);
        RunUpgradeDefinition fantasy = FindUpgrade(RunUpgradeKind.FantasySeal);
        RunUpgradeDefinition circle = FindUpgrade(RunUpgradeKind.EvilSealingCircle);
        Require(!build.CanUpgrade(fantasy) && !build.CanUpgrade(circle),
            "Spell cards appeared before prerequisites.");
        build.Apply(needle);
        build.Apply(needle);
        build.Apply(attraction);
        build.Apply(attraction);
        Require(build.Apply(fantasy) && build.Apply(circle) &&
            !build.CanUpgrade(fantasy) && !build.CanUpgrade(circle),
            "Spell card prerequisite or one-rank cap is incorrect.");
    }

    /// <summary>
    /// 确认设置目录没有施放或切换符卡动作，玩家只承担移动与构筑选择。
    /// </summary>
    private static void VerifyNoActiveBindings() => Require(
        InputActionCatalog.All.All(action =>
            action.Id != "cast_spell_card" && action.Id != "cycle_spell_card"),
        "Spell cards must not add active input actions.");

    /// <summary>
    /// 按稳定效果类型查找升级定义，使测试不依赖目录下标或中文显示名。
    /// </summary>
    private static RunUpgradeDefinition FindUpgrade(RunUpgradeKind kind) =>
        RunUpgradeCatalog.All.Single(definition => definition.Kind == kind);

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
