using Godot;
using TouhouWuxiaSurvivor.Combat.Weapons;
using TouhouWuxiaSurvivor.Content.Characters;
using TouhouWuxiaSurvivor.Ecs.Combat.Projectiles;

namespace TouhouWuxiaSurvivor.Tests.Integration;

/// <summary>
/// 验证全部角色使用显式定位、横向预算受控，并保证 Boss 基础接触伤害不会一击击倒任何自机。
/// </summary>
public partial class CharacterBalanceContractTest : Node
{
    /// <summary>
    /// 执行角色数值完整性测试，并通过进程退出码向自动测试入口报告失败。
    /// </summary>
    public override void _Ready()
    {
        try
        {
            VerifyCompleteRoleCoverage();
            VerifyPlayableBudgets();
            VerifyTenSecondAttackBudgets();
            VerifyBossBudgets();
            VerifyContactDamageSafety();
            GD.Print("Character balance contract test passed.");
            GetTree().Quit();
        }
        catch (Exception exception)
        {
            GD.PushError(exception.ToString());
            GetTree().Quit(1);
        }
    }

    /// <summary>
    /// 确认角色目录与定位表一一对应，且六种战斗定位都实际分配给了角色。
    /// </summary>
    private static void VerifyCompleteRoleCoverage()
    {
        string[] catalogNames = CharacterCatalog.All.Select(character => character.DisplayName).ToArray();
        Require(catalogNames.Length == CharacterRoleCatalog.RegisteredNames.Count,
            "Character role registration count does not match the canonical catalog.");
        Require(catalogNames.All(name => CharacterRoleCatalog.RegisteredNames.Contains(name)),
            "A canonical character is missing an explicit combat role.");
        foreach (CharacterCombatRole role in Enum.GetValues<CharacterCombatRole>())
        {
            Require(CharacterCatalog.All.Any(character => character.CombatRole == role),
                $"Combat role has no assigned character: {role}");
        }
    }

    /// <summary>
    /// 限制自机定位预算在紧密区间内，同时确认工厂对同一定位始终产生相同配置。
    /// </summary>
    private static void VerifyPlayableBudgets()
    {
        foreach (CharacterDefinition character in CharacterCatalog.All)
        {
            float budget = CharacterBalanceBudget.EvaluatePlayable(character.PlayableProfile);
            Require(budget is >= 0.90f and <= 1.02f,
                $"Playable budget is outside the horizontal band: {character.DisplayName}/{budget:F3}");
            PlayableCharacterProfile expected = CharacterCombatProfileFactory.CreatePlayable(character.CombatRole);
            Require(expected.MaxHealth == character.PlayableProfile.MaxHealth &&
                expected.MoveSpeedMultiplier == character.PlayableProfile.MoveSpeedMultiplier &&
                expected.AttackMultiplier == character.PlayableProfile.AttackMultiplier &&
                expected.AttackIntervalMultiplier ==
                    character.PlayableProfile.AttackIntervalMultiplier &&
                expected.UltimateIntervalSeconds == character.PlayableProfile.UltimateIntervalSeconds &&
                expected.UltimateTargetCapacity == character.PlayableProfile.UltimateTargetCapacity,
                $"Playable profile does not match its explicit role: {character.DisplayName}");
        }
    }

    /// <summary>
    /// 以正式首次延迟和整数伤害核对十秒普攻；力量少而重、速攻多而轻，合计仍处同一横向带。
    /// </summary>
    private static void VerifyTenSecondAttackBudgets()
    {
        var expected = new Dictionary<CharacterCombatRole, (int Volleys, int Damage)>
        {
            [CharacterCombatRole.Balanced] = (36, 360),
            [CharacterCombatRole.Power] = (32, 384),
            [CharacterCombatRole.Rapid] = (40, 360),
            [CharacterCombatRole.Swift] = (36, 360),
            [CharacterCombatRole.Formation] = (36, 360),
            [CharacterCombatRole.Guardian] = (35, 350),
        };
        foreach (CharacterCombatRole role in Enum.GetValues<CharacterCombatRole>())
        {
            PlayableCharacterProfile profile =
                CharacterCombatProfileFactory.CreatePlayable(role);
            double interval = AutoAttackCadence.CalculateInterval(
                0.28, profile.AttackIntervalMultiplier, 1.0);
            int volleys = AutoAttackCadence.CountVolleys(10.0, interval);
            int volleyDamage = ProjectileDamageBudget.CalculateVolleyDamage(
                10.0 * profile.AttackMultiplier, 1.0, 1);
            int totalDamage = volleys * volleyDamage;
            Require((volleys, totalDamage) == expected[role],
                $"Ten-second attack budget drifted: {role}/{volleys}/{totalDamage}.");
            Require(totalDamage is >= 350 and <= 384,
                $"Ten-second attack damage left the horizontal band: {role}/{totalDamage}.");
        }
    }

    /// <summary>
    /// 限制 Boss 定位预算的上下界，允许不同追击方式产生威胁差异但禁止作品级数值膨胀。
    /// </summary>
    private static void VerifyBossBudgets()
    {
        foreach (CharacterDefinition character in CharacterCatalog.All)
        {
            float budget = CharacterBalanceBudget.EvaluateBoss(character.BossProfile);
            Require(budget is >= 0.82f and <= 1.15f,
                $"Boss budget is outside the horizontal band: {character.DisplayName}/{budget:F3}");
        }
    }

    /// <summary>
    /// 用全角色最低生命核对最高基础接触伤害，确保任何 Boss 都不能一次接触直接结算本局。
    /// </summary>
    private static void VerifyContactDamageSafety()
    {
        float minimumPlayableHealth = CharacterCatalog.All.Min(character => character.PlayableProfile.MaxHealth);
        float maximumBossContact = CharacterCatalog.All.Max(character => character.BossProfile.ContactDamage);
        Require(maximumBossContact < minimumPlayableHealth,
            "Boss contact damage can one-shot a base playable character.");
        Require(maximumBossContact <= 2.0f,
            "Boss base contact damage exceeded the authored safety ceiling.");
    }

    /// <summary>
    /// 将角色数值契约失败转换为带有明确原因的测试异常。
    /// </summary>
    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
