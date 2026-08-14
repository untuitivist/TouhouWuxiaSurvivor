using Godot;
using TouhouWuxiaSurvivor.Gameplay.SpellCards.Definitions;
using TouhouWuxiaSurvivor.Gameplay.SpellCards.Runtime;
using TouhouWuxiaSurvivor.Gameplay.SpellCards.Triggers;
using TouhouWuxiaSurvivor.Tests.Support;

namespace TouhouWuxiaSurvivor.Tests.Integration;

/// <summary>
/// 验证无资源自动奥义协调器在多卡轮转、失败重试、动态缩放与终局状态上的完整契约。
/// </summary>
public partial class SpellCardCoordinatorTimingTest : Node
{
    /// <summary>依次执行协调器契约，任何断言失败都以非零退出码结束无头测试。</summary>
    public override void _Ready()
    {
        try
        {
            VerifyIndependentCyclesWithoutStarvation();
            VerifyFailedCastUsesShortRetry();
            VerifyIntervalChangePreservesProgress();
            VerifyRunEndBlockAndConfigureReset();
            GD.Print("Spell card coordinator timing test passed.");
            GetTree().Quit();
        }
        catch (Exception exception)
        {
            GD.PushError(exception.ToString());
            GetTree().Quit(1);
        }
    }

    /// <summary>确认两张不同周期奥义只重置自身，并按各自到期时间持续获得施展机会。</summary>
    private static void VerifyIndependentCyclesWithoutStarvation()
    {
        SpellCardDefinition first = CreateCard("test_alpha");
        SpellCardDefinition second = CreateCard("test_beta");
        var executor = new TestSpellCardEffectExecutor { CastLockSeconds = 0.1f };
        executor.SetInterval(first.Id, 2.0f);
        executor.SetInterval(second.Id, 3.0f);
        var source = new TestSpellCardUnlockSource();
        source.SetCards(first, second);
        var coordinator = new SpellCardCoordinator();
        coordinator.Configure(executor, source, new SpellCardTriggerFactory(),
            new TestSpellCardTriggerEnvironment());

        coordinator._Process(0.0);
        coordinator._Process(2.0);
        coordinator._Process(1.0);
        coordinator._Process(1.0);

        Require(executor.SuccessfulCardIds.SequenceEqual(
            new[] { first.Id, second.Id, first.Id }),
            "Independent spell cycles starved one card or reset both timers.");
        coordinator.Free();
    }

    /// <summary>确认缺少施展条件时仅等待起手短延迟，不会重新等待整轮奥义周期。</summary>
    private static void VerifyFailedCastUsesShortRetry()
    {
        SpellCardDefinition card = CreateCard("test_retry");
        var executor = new TestSpellCardEffectExecutor
        {
            CastLockSeconds = 0.25f,
            RejectCasts = true,
        };
        executor.SetInterval(card.Id, 4.0f);
        var source = new TestSpellCardUnlockSource();
        source.SetCards(card);
        var coordinator = new SpellCardCoordinator();
        coordinator.Configure(executor, source, new SpellCardTriggerFactory(),
            new TestSpellCardTriggerEnvironment());

        coordinator._Process(0.0);
        coordinator._Process(4.0);
        Require(executor.AttemptedCardIds.Count == 1,
            "Ready spell did not attempt its first cast.");
        Require(Mathf.IsEqualApprox(coordinator.NextCastRemaining, 0.25f),
            "Failed spell reset to its full interval instead of a short retry.");

        coordinator._Process(0.1);
        Require(executor.AttemptedCardIds.Count == 1,
            "Failed spell retried before its short retry delay elapsed.");
        coordinator._Process(0.16);
        Require(executor.AttemptedCardIds.Count == 2,
            "Failed spell did not retry after its short delay elapsed.");

        executor.RejectCasts = false;
        coordinator._Process(0.25);
        Require(executor.SuccessfulCardIds.SequenceEqual(new[] { card.Id }),
            "Spell did not recover after its failed-cast condition cleared.");
        Require(Mathf.IsEqualApprox(coordinator.NextCastRemaining, 4.0f),
            "Successful retry did not restart the spell's full cycle.");
        coordinator.Free();
    }

    /// <summary>确认实效周期改变时按已完成比例换算剩余时间，而非丢失已走进度。</summary>
    private static void VerifyIntervalChangePreservesProgress()
    {
        SpellCardDefinition card = CreateCard("test_rescale");
        var executor = new TestSpellCardEffectExecutor();
        executor.SetInterval(card.Id, 8.0f);
        var source = new TestSpellCardUnlockSource();
        source.SetCards(card);
        var coordinator = new SpellCardCoordinator();
        coordinator.Configure(executor, source, new SpellCardTriggerFactory(),
            new TestSpellCardTriggerEnvironment());

        coordinator._Process(0.0);
        coordinator._Process(2.0);
        executor.SetInterval(card.Id, 4.0f);
        coordinator._Process(0.0);
        Require(Mathf.IsEqualApprox(coordinator.NextCastRemaining, 3.0f),
            "Faster interval did not preserve completed progress ratio.");

        executor.SetInterval(card.Id, 10.0f);
        coordinator._Process(0.0);
        Require(Mathf.IsEqualApprox(coordinator.NextCastRemaining, 7.5f),
            "Slower interval did not preserve completed progress ratio.");
        coordinator.Free();
    }

    /// <summary>确认终局阻断禁止施展，而重新配置新局会解除阻断并建立完整首周期。</summary>
    private static void VerifyRunEndBlockAndConfigureReset()
    {
        SpellCardDefinition card = CreateCard("test_run_end");
        var executor = new TestSpellCardEffectExecutor();
        executor.SetInterval(card.Id, 1.0f);
        var source = new TestSpellCardUnlockSource();
        source.SetCards(card);
        var coordinator = new SpellCardCoordinator();
        var environment = new TestSpellCardTriggerEnvironment();
        coordinator.Configure(executor, source, new SpellCardTriggerFactory(), environment);
        coordinator._Process(0.0);

        coordinator.BlockForRunEnd();
        coordinator._Process(2.0);
        Require(coordinator.IsRunEndBlocked && executor.AttemptedCardIds.Count == 0,
            "Run-end block allowed a ready spell to cast.");

        coordinator.Configure(executor, source, new SpellCardTriggerFactory(), environment);
        Require(!coordinator.IsRunEndBlocked,
            "Configure did not reset the previous run-end block.");
        coordinator._Process(0.0);
        Require(Mathf.IsEqualApprox(coordinator.NextCastRemaining, 1.0f),
            "Configure did not rebuild a full first cycle for the new run.");
        coordinator._Process(1.0);
        Require(executor.SuccessfulCardIds.SequenceEqual(new[] { card.Id }),
            "Reconfigured coordinator remained blocked in the new run.");
        coordinator.Free();
    }

    /// <summary>创建只含合法倍率的测试奥义，使测试关注协调器而不依赖内容包目录。</summary>
    private static SpellCardDefinition CreateCard(string id) => new(
        id,
        "test_pack",
        id,
        id,
        "test_owner",
        "测试角色",
        SpellCardCanonLevel.Official,
        "测试来源",
        "测试武学",
        "测试自动奥义",
        SpellCardEffectKind.HomingVolley,
        SpellCardGeometryKind.Orbit,
        SpellCardActivationKind.Periodic,
        "test_prerequisite",
        1,
        new SpellCardCombatProfile(1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 0.0f,
            1.0f, 1.0f, 1.0f, 1.0f));

    /// <summary>将契约违约转为含有具体原因的测试异常。</summary>
    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
