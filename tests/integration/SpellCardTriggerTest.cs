using Godot;
using TouhouWuxiaSurvivor.Gameplay.SpellCards.Contracts;
using TouhouWuxiaSurvivor.Gameplay.SpellCards.Definitions;
using TouhouWuxiaSurvivor.Gameplay.SpellCards.Triggers;
using TouhouWuxiaSurvivor.Tests.Support;

namespace TouhouWuxiaSurvivor.Tests.Integration;

/// <summary>
/// 验证三类无资源自动触发策略的事件边界、数值阈值与查询节流，不依赖实际战斗场景。
/// </summary>
public partial class SpellCardTriggerTest : Node
{
    /// <summary>运行所有策略契约；任一违约均以非零退出码结束无头测试。</summary>
    public override void _Ready()
    {
        try
        {
            VerifyPeriodicTrigger();
            VerifyCrowdTriggerAndThrottle();
            VerifyOnDamagedDoesNotQueueCooldownEvents();
            VerifyOnDamagedSignalsAreIndependent();
            GD.Print("Spell card trigger test passed.");
            GetTree().Quit();
        }
        catch (Exception exception)
        {
            GD.PushError(exception.ToString());
            GetTree().Quit(1);
        }
    }

    /// <summary>确认周期型只随恢复状态触发，消费后在同一到期状态可供短重试重新判定。</summary>
    private static void VerifyPeriodicTrigger()
    {
        var trigger = new PeriodicSpellCardTrigger();
        var environment = new TestSpellCardTriggerEnvironment();
        SpellCardDefinition card = CreateCard("periodic", SpellCardActivationKind.Periodic);
        ResolvedSpellCardCombat combat = CreateCombat();
        trigger.Advance(new SpellCardTriggerContext(card, combat, environment, 0.1f, false));
        Require(!trigger.IsTriggered, "Periodic trigger fired before cooldown was ready.");
        trigger.Advance(new SpellCardTriggerContext(card, combat, environment, 0.1f, true));
        Require(trigger.IsTriggered, "Periodic trigger did not fire when cooldown became ready.");
        trigger.Consume();
        Require(!trigger.IsTriggered, "Periodic trigger was not consumed.");
    }

    /// <summary>确认敌群型读取解析阈值与效果范围，并按环境间隔限制未满足时的重复查询。</summary>
    private static void VerifyCrowdTriggerAndThrottle()
    {
        var trigger = new CrowdSpellCardTrigger();
        var environment = new TestSpellCardTriggerEnvironment
        {
            CrowdEvaluationIntervalSeconds = 0.25f,
            NearbyEnemyCount = 2,
        };
        SpellCardDefinition card = CreateCard("crowd", SpellCardActivationKind.Crowd);
        ResolvedSpellCardCombat combat = CreateCombat(activationThreshold: 3);
        trigger.Advance(new SpellCardTriggerContext(card, combat, environment, 0.0f, true));
        Require(!trigger.IsTriggered && environment.CrowdQueryCount == 1,
            "Crowd trigger ignored its resolved threshold.");
        trigger.Advance(new SpellCardTriggerContext(card, combat, environment, 0.1f, true));
        Require(environment.CrowdQueryCount == 1,
            "Crowd trigger queried again before its evaluation interval.");
        environment.NearbyEnemyCount = 3;
        trigger.Advance(new SpellCardTriggerContext(card, combat, environment, 0.16f, true));
        Require(trigger.IsTriggered && environment.CrowdQueryCount == 2,
            "Crowd trigger did not fire after the throttled threshold evaluation.");
        Require(Mathf.IsEqualApprox(environment.LastQueryRange, combat.EffectRange),
            "Crowd trigger did not query with the resolved effect range.");
    }

    /// <summary>确认冷却期发生的受击被追平，恢复完成后必须等待新的受击事件才会触发。</summary>
    private static void VerifyOnDamagedDoesNotQueueCooldownEvents()
    {
        var trigger = new OnDamagedSpellCardTrigger();
        var environment = new TestSpellCardTriggerEnvironment();
        SpellCardDefinition card = CreateCard("damaged", SpellCardActivationKind.OnDamaged);
        ResolvedSpellCardCombat combat = CreateCombat();
        trigger.Advance(new SpellCardTriggerContext(card, combat, environment, 0.1f, false));
        environment.ReportDamage();
        trigger.Advance(new SpellCardTriggerContext(card, combat, environment, 0.1f, false));
        trigger.Advance(new SpellCardTriggerContext(card, combat, environment, 0.1f, true));
        Require(!trigger.IsTriggered, "Cooldown damage was queued into a delayed cast.");
        environment.ReportDamage();
        trigger.Advance(new SpellCardTriggerContext(card, combat, environment, 0.1f, true));
        Require(trigger.IsTriggered, "New ready-state damage did not trigger the spell.");
    }

    /// <summary>确认同一受击序号可被多张独立奥义同时观察，而不会被全局消费导致事件丢失。</summary>
    private static void VerifyOnDamagedSignalsAreIndependent()
    {
        var first = new OnDamagedSpellCardTrigger();
        var second = new OnDamagedSpellCardTrigger();
        var environment = new TestSpellCardTriggerEnvironment();
        SpellCardDefinition card = CreateCard("shared_damage", SpellCardActivationKind.OnDamaged);
        ResolvedSpellCardCombat combat = CreateCombat();
        var initial = new SpellCardTriggerContext(card, combat, environment, 0.0f, true);
        first.Advance(initial);
        second.Advance(initial);
        environment.ReportDamage();
        var damaged = new SpellCardTriggerContext(card, combat, environment, 0.0f, true);
        first.Advance(damaged);
        second.Advance(damaged);
        Require(first.IsTriggered && second.IsTriggered,
            "A shared damage signal was lost between independent spell observers.");
    }

    /// <summary>建立测试用最终战斗值，触发阈值保持显式以隔离策略和属性解析器。</summary>
    private static ResolvedSpellCardCombat CreateCombat(int activationThreshold = 2) => new(
        2.0f, 96.0f, 10, 2, activationThreshold, 1.0f, 300.0f, 20.0f,
        0.5f, 12.0f, 0.2f);

    /// <summary>建立指定自动触发类型的合法测试奥义，不依赖内容包目录。</summary>
    private static SpellCardDefinition CreateCard(string id, SpellCardActivationKind activation) =>
        new(id, "test_pack", id, id, "test_owner", "测试角色",
            SpellCardCanonLevel.Official, "测试来源", "测试武学", "测试自动奥义",
            SpellCardEffectKind.HomingVolley, SpellCardGeometryKind.Orbit,
            SpellBulletStyleKind.Orb,
            activation, "test_prerequisite", 1,
            new SpellCardCombatProfile(1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 0.0f,
                1.0f, 1.0f, 1.0f, 1.0f));

    /// <summary>将策略契约违约转为包含具体原因的测试异常。</summary>
    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
