using Godot;
using TouhouWuxiaSurvivor.Actors.Enemies;
using TouhouWuxiaSurvivor.Content;
using TouhouWuxiaSurvivor.Ecs.Combat;
using TouhouWuxiaSurvivor.Gameplay.Progression.Definitions;
using TouhouWuxiaSurvivor.Gameplay.Progression.Runtime;
using TouhouWuxiaSurvivor.Gameplay.SpellCards.Definitions;

namespace TouhouWuxiaSurvivor.Tests.Integration;

/// <summary>
/// 验证奥义采用四主攻加二护持的横向容量、候选限流，以及范围伤害的最近目标与衰减预算。
/// </summary>
public partial class SpellCardSlotBalanceTest : Node
{
    /// <summary>依次执行纯数据契约，并用明确退出码报告槽位或范围伤害回归。</summary>
    public override void _Ready()
    {
        try
        {
            VerifySlotCapacity();
            VerifyOfferContainsAtMostOneSpell();
            VerifyNearestTargetFalloff();
            GD.Print("Spell card slot balance test passed.");
            GetTree().Quit();
        }
        catch (Exception exception)
        {
            GD.PushError(exception.ToString());
            GetTree().Quit(1);
        }
    }

    /// <summary>
    /// 填满四个主攻与两个护持槽，确认新奥义被拒绝且阻断原因明确区分两种容量。
    /// </summary>
    private static void VerifySlotCapacity()
    {
        var build = new RunBuildState();
        SpellCardDefinition[] offensive = SpellCardCatalog.All.Where(card =>
            SpellCardSlotPolicy.Classify(card) == SpellCardSlotKind.Offensive).Take(5).ToArray();
        SpellCardDefinition[] support = SpellCardCatalog.All.Where(card =>
            SpellCardSlotPolicy.Classify(card) == SpellCardSlotKind.Support).Take(3).ToArray();
        Require(offensive.Length == 5 && support.Length == 3,
            "The catalog lacks enough cards to verify both slot caps.");

        foreach (SpellCardDefinition card in offensive.Take(4).Concat(support.Take(2)))
        {
            Unlock(build, card);
        }

        Require(SpellCardSlotPolicy.CountOccupied(build, SpellCardSlotKind.Offensive) == 4 &&
            SpellCardSlotPolicy.CountOccupied(build, SpellCardSlotKind.Support) == 2,
            "The build did not occupy the expected 4+2 spell slots.");
        SatisfyPrerequisite(build, offensive[4]);
        SatisfyPrerequisite(build, support[2]);
        RunUpgradeDefinition blockedOffensive = FindUpgrade(offensive[4]);
        RunUpgradeDefinition blockedSupport = FindUpgrade(support[2]);
        Require(!build.CanUpgrade(blockedOffensive) &&
            build.GetUpgradeBlockReason(blockedOffensive)?.Contains("主攻奥义已满") == true,
            "A fifth offensive spell was not blocked with a clear reason.");
        Require(!build.CanUpgrade(blockedSupport) &&
            build.GetUpgradeBlockReason(blockedSupport)?.Contains("护持奥义已满") == true,
            "A third support spell was not blocked with a clear reason.");
    }

    /// <summary>
    /// 在所有作品启用且大量奥义满足前置时遍历固定种子，确认每轮三选一最多出现一张奥义。
    /// </summary>
    private static void VerifyOfferContainsAtMostOneSpell()
    {
        var build = new RunBuildState();
        foreach (RunUpgradeDefinition definition in RunUpgradeCatalog.All.Where(item =>
            item.Category != RunUpgradeCategory.SpellCard && !item.IsRepeatable))
        {
            while (build.CanUpgrade(definition))
            {
                build.Apply(definition);
            }
        }

        var generator = new RunOfferGenerator();
        var content = new ContentPackSelection(ContentPackCatalog.All.Select(pack => pack.Id));
        for (ulong seed = 1; seed <= 300; seed++)
        {
            var runtimeRandom = new RandomNumberGenerator { Seed = seed };
            IReadOnlyList<RunUpgradeChoice> offer = generator.CreateOffer(
                runtimeRandom, build, content, 12, 3);
            Require(offer.Count(choice =>
                    choice.Definition.Category == RunUpgradeCategory.SpellCard) <= 1,
                $"Offer seed {seed} contained more than one spell card.");
            var catalogRandom = new RandomNumberGenerator { Seed = seed };
            IReadOnlyList<RunUpgradeDefinition> compatibility = RunUpgradeCatalog.CreateOffer(
                catalogRandom, build, content, 3);
            Require(compatibility.Count(choice =>
                    choice.Category == RunUpgradeCategory.SpellCard) <= 1,
                $"Compatibility offer seed {seed} contained more than one spell card.");
        }
    }

    /// <summary>
    /// 以乱序距离加入三个敌人，确认只选择最近两个，且越远目标承受的伤害严格降低。
    /// </summary>
    private static void VerifyNearestTargetFalloff()
    {
        var enemies = new EnemyPool();
        EnemyDefinition definition = EnemyCatalog.All[0];
        enemies.Add(new Vector2(90.0f, 0.0f), definition);
        enemies.Add(new Vector2(10.0f, 0.0f), definition);
        enemies.Add(new Vector2(50.0f, 0.0f), definition);
        var hits = new List<(int index, int damage)>();
        int count = new AreaDamageSystem().Apply(
            enemies, Vector2.Zero, 100.0f, 100, 2, 0.45f,
            (index, damage) => hits.Add((index, damage)));

        Require(count == 2 && hits.Select(hit => hit.index).SequenceEqual([1, 2]),
            "Area damage did not choose the nearest distinct targets.");
        Require(hits[0].damage == 95 && hits[1].damage == 73 &&
            AreaDamageSystem.CalculateDamage(100, 100.0f, 100.0f, 0.45f) == 45,
            "Area damage did not apply the planned center-to-edge falloff.");
    }

    /// <summary>满足一张奥义的前置并应用它，模拟正式构筑选择而不绕过容量规则。</summary>
    private static void Unlock(RunBuildState build, SpellCardDefinition card)
    {
        SatisfyPrerequisite(build, card);
        Require(build.Apply(FindUpgrade(card)), $"Could not unlock spell {card.Id}.");
    }

    /// <summary>只补足指定奥义所需基础修炼，便于把前置判定与容量判定分别验证。</summary>
    private static void SatisfyPrerequisite(RunBuildState build, SpellCardDefinition card)
    {
        RunUpgradeDefinition prerequisite = RunUpgradeCatalog.FindById(
            card.PrerequisiteUpgradeId)!;
        while (build.GetRank(prerequisite.Id) < card.MinimumRank)
        {
            Require(build.Apply(prerequisite), $"Could not satisfy prerequisite for {card.Id}.");
        }
    }

    /// <summary>按稳定解锁 ID 返回正式升级定义，缺失时立即让测试失败。</summary>
    private static RunUpgradeDefinition FindUpgrade(SpellCardDefinition card) =>
        RunUpgradeCatalog.FindById(card.UnlockUpgradeId) ??
        throw new InvalidOperationException($"Missing upgrade for spell {card.Id}.");

    /// <summary>将任一策划契约失败转换为包含具体原因的测试异常。</summary>
    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
