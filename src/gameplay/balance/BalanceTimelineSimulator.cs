using TouhouWuxiaSurvivor.Content;
using TouhouWuxiaSurvivor.Content.Characters;
using TouhouWuxiaSurvivor.Gameplay.Difficulty;
using TouhouWuxiaSurvivor.Gameplay.Progression.Runtime;
using TouhouWuxiaSurvivor.Gameplay.Spawning;
using TouhouWuxiaSurvivor.Gameplay.SpellCards.Definitions;

namespace TouhouWuxiaSurvivor.Gameplay.Balance;

/// <summary>
/// 以一秒步长运行确定性策划预算，输出约定分钟节点而不模拟碰撞、走位或随机掉落。
/// </summary>
public sealed class BalanceTimelineSimulator
{
    public static IReadOnlyList<int> DefaultMilestones { get; } =
        [0, 2, 5, 10, 20, 30, 60, 120];
    /// <summary>
    /// 对指定路线和内容选择运行完整时间轴；角色由路线映射到正式定位中的一个稳定代表。
    /// </summary>
    public IReadOnlyList<BalanceTimelineSnapshot> Simulate(
        BalanceBuildKind buildKind,
        ContentPackSelection content,
        IEnumerable<int>? milestones = null)
    {
        int[] targets = (milestones ?? DefaultMilestones)
            .Select(value => Math.Max(0, value))
            .Distinct()
            .Order()
            .ToArray();
        if (targets.Length == 0)
        {
            return [];
        }

        CharacterDefinition character = SelectCharacter(buildKind);
        var build = new RunBuildState();
        var experience = new BalanceExperienceLedger();
        var spawnFlow = new BalanceSpawnFlowState();
        var result = new List<BalanceTimelineSnapshot>(targets.Length);
        int targetIndex = 0;
        int maximumSeconds = checked(targets[^1] * 60);
        for (int elapsedSeconds = 0; elapsedSeconds <= maximumSeconds; elapsedSeconds++)
        {
            EndlessDifficultySnapshot difficulty = EndlessDifficultyCurve.EvaluateSeconds(
                elapsedSeconds, EnemySpawnPacing.DefaultAliveHardLimit);
            BalanceEnemySnapshot enemy = BalanceEnemyBudget.Evaluate(
                elapsedSeconds, content, difficulty, spawnFlow.AliveCount,
                spawnFlow.LastAcceptedSpawnsPerSecond);
            BalanceCombatMetrics combat = BalanceCombatProjector.Evaluate(
                elapsedSeconds, character, build, buildKind);
            if (targetIndex < targets.Length && elapsedSeconds == targets[targetIndex] * 60)
            {
                result.Add(CreateSnapshot(targets[targetIndex], buildKind, experience,
                    combat, enemy, difficulty, content));
                targetIndex++;
            }

            if (elapsedSeconds == maximumSeconds)
            {
                break;
            }

            double defeatCapacity = CalculateDefeatCapacity(combat, enemy);
            BalanceSpawnFlowSnapshot flow = spawnFlow.Advance(difficulty, defeatCapacity);
            double killsPerSecond = flow.DefeatsPerSecond;
            double collectionRate = CalculateCollectionRate(combat);
            double experienceGain = killsPerSecond * enemy.AverageSpiritValue *
                combat.SpiritYieldMultiplier * collectionRate;
            experience.AddExperience(experienceGain, build, buildKind, content);
        }

        return result;
    }

    /// <summary>
    /// 同时模拟四条路线，字典枚举顺序固定为构筑枚举顺序，方便测试输出稳定比较。
    /// </summary>
    public IReadOnlyDictionary<BalanceBuildKind, IReadOnlyList<BalanceTimelineSnapshot>>
        SimulateAll(ContentPackSelection content) =>
        Enum.GetValues<BalanceBuildKind>().ToDictionary(
            kind => kind,
            kind => Simulate(kind, content));

    /// <summary>
    /// 将战斗处理能力限制在敌群实际供给内；转换率表示走位、转火和过量伤害造成的预算损耗。
    /// </summary>
    private static double CalculateDefeatCapacity(
        BalanceCombatMetrics combat,
        BalanceEnemySnapshot enemy)
    {
        double combatCapacity = combat.TotalDps /
            Math.Max(1.0, enemy.AverageScaledHealth) * 0.68;
        return Math.Max(0.0, combatCapacity);
    }

    /// <summary>
    /// 把移动与吸附覆盖折算为灵息收集率并限制在合理区间，效用路线因此有经济收益但不会翻倍。
    /// </summary>
    private static double CalculateCollectionRate(BalanceCombatMetrics combat) =>
        Math.Clamp(0.58 + 0.09 * Math.Sqrt(combat.MoveSpeedMultiplier) +
            0.15 * Math.Sqrt(combat.SpiritAttractionMultiplier), 0.72, 0.98);

    /// <summary>
    /// 从计算结果建立公开快照；槽位容量预算只由共享策略决定，与启用作品数量完全无关。
    /// </summary>
    private static BalanceTimelineSnapshot CreateSnapshot(
        int elapsedMinutes,
        BalanceBuildKind buildKind,
        BalanceExperienceLedger experience,
        BalanceCombatMetrics combat,
        BalanceEnemySnapshot enemy,
        EndlessDifficultySnapshot difficulty,
        ContentPackSelection content)
    {
        double kills = Math.Min(CalculateDefeatCapacity(combat, enemy),
            enemy.SpawnSupplyPerSecond + Math.Max(0.0, enemy.ProjectedAliveCount));
        double ratio = combat.ReadinessScore / Math.Max(0.01, enemy.Pressure);
        double spiritEconomy = CalculateCollectionRate(combat) *
            combat.SpiritYieldMultiplier;
        double slotBudget = SpellCardSlotPolicy.MaximumOffensiveSlots +
            SpellCardSlotPolicy.MaximumSupportSlots * 0.65;
        return new BalanceTimelineSnapshot(elapsedMinutes, buildKind, experience.Level,
            experience.TotalExperience, combat.WeaponDps, combat.SpellDps, combat.TotalDps,
            combat.ReadinessScore, difficulty.EnemyHealthMultiplier,
            difficulty.EnemyDamageMultiplier, difficulty.RewardMultiplier,
            difficulty.ScheduledSpawnsPerSecond, kills, enemy.Pressure, ratio,
            spiritEconomy, combat.OffensiveSpellCount, combat.SupportSpellCount,
            combat.EndlessRankCount,
            SpellCardCatalog.GetEnabled(content).Count,
            SpellCardSlotPolicy.MaximumOffensiveSlots,
            SpellCardSlotPolicy.MaximumSupportSlots, slotBudget);
    }

    /// <summary>
    /// 为基础、强攻、速射和效用路线选取均衡、力量、连射和阵法定位代表，不依赖目录排列猜测。
    /// </summary>
    private static CharacterDefinition SelectCharacter(BalanceBuildKind buildKind)
    {
        CharacterCombatRole role = buildKind switch
        {
            BalanceBuildKind.Assault => CharacterCombatRole.Power,
            BalanceBuildKind.Rapid => CharacterCombatRole.Rapid,
            BalanceBuildKind.Utility => CharacterCombatRole.Formation,
            _ => CharacterCombatRole.Balanced,
        };
        return CharacterCatalog.All.First(character => character.CombatRole == role);
    }
}
