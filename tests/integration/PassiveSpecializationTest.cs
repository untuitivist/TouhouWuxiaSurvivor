using Godot;
using TouhouWuxiaSurvivor.Gameplay.Progression.Definitions;
using TouhouWuxiaSurvivor.Gameplay.Progression.Runtime;

namespace TouhouWuxiaSurvivor.Tests.Integration;

/// <summary>
/// 验证四种条件武学只在对应战况下蓄势，离开条件后会衰减且不会污染永久构筑倍率。
/// </summary>
public partial class PassiveSpecializationTest : Node
{
    /// <summary>建立四套真实特化构筑并逐项验证射击、停步、移动和灵息触发状态。</summary>
    public override void _Ready()
    {
        try
        {
            VerifyContinuousFire();
            VerifyStationaryFocus();
            VerifyMovementMomentum();
            VerifySpiritFlow();
            GD.Print("Passive specialization test passed.");
            GetTree().Quit();
        }
        catch (Exception exception)
        {
            GD.PushError(exception.ToString());
            GetTree().Quit(1);
        }
    }

    /// <summary>连续成功齐射应逐步达到20%射速峰值，停止齐射后应回落到基础值。</summary>
    private static void VerifyContinuousFire()
    {
        (RunModifierState modifiers, PassiveSpecializationState state) =
            CreateState("hakurei_breathing", "breathing_swift");
        Require(modifiers.UsesContinuousFireMomentum &&
            Mathf.IsEqualApprox(state.FireRateMultiplier, 1.0f),
            "疾息周天未进入条件状态或在未射击时提前生效。");
        for (int index = 0; index < 12; index++)
        {
            state.RegisterVolley();
            state.AdvanceCombat(0.1);
        }
        Require(state.FireMomentum > 0.99f &&
            Mathf.IsEqualApprox(state.FireRateMultiplier, 1.20f),
            "连续齐射没有蓄满疾息射速预算。");
        state.AdvanceCombat(1.2);
        Require(state.FireMomentum < 0.01f,
            "停止射击后疾息蓄势没有衰减。");
    }

    /// <summary>停步应蓄满22%凝神攻势，重新移动后应快速失去该峰值。</summary>
    private static void VerifyStationaryFocus()
    {
        (_, PassiveSpecializationState state) =
            CreateState("hakurei_breathing", "breathing_focus");
        state.AdvanceMovement(false, 0.9);
        Require(Mathf.IsEqualApprox(state.AttackPowerMultiplier, 1.22f),
            "停步没有蓄满凝神攻势。");
        state.AdvanceMovement(true, 0.3);
        Require(Mathf.IsEqualApprox(state.AttackPowerMultiplier, 1.0f),
            "移动后凝神攻势没有解除。");
    }

    /// <summary>持续移动应蓄满18%逐风移速，停下后回落且不改变永久移动倍率。</summary>
    private static void VerifyMovementMomentum()
    {
        (RunModifierState modifiers, PassiveSpecializationState state) =
            CreateState("tengu_step", "tengu_gale");
        float permanent = modifiers.MoveSpeedMultiplier;
        state.AdvanceMovement(true, 1.1);
        Require(Mathf.IsEqualApprox(state.MoveSpeedMultiplier, 1.18f) &&
            Mathf.IsEqualApprox(modifiers.MoveSpeedMultiplier, permanent),
            "逐风蓄势未达到峰值或污染了永久倍率。");
        state.AdvanceMovement(false, 0.5);
        Require(Mathf.IsEqualApprox(state.MoveSpeedMultiplier, 1.0f),
            "停止移动后逐风蓄势没有解除。");
    }

    /// <summary>只有正值灵息会刷新三秒流云窗口，过期后移动倍率应恢复基础。</summary>
    private static void VerifySpiritFlow()
    {
        (_, PassiveSpecializationState state) =
            CreateState("spirit_gathering", "spirit_flow");
        state.RegisterSpiritCollected(0);
        Require(!state.SpiritFlowActive, "零值灵息错误触发了流云势。");
        state.RegisterSpiritCollected(1);
        Require(state.SpiritFlowActive &&
            Mathf.IsEqualApprox(state.MoveSpeedMultiplier, 1.18f),
            "拾取灵息没有触发流云移速。");
        state.AdvanceMovement(false, 3.1);
        Require(!state.SpiritFlowActive &&
            Mathf.IsEqualApprox(state.MoveSpeedMultiplier, 1.0f),
            "流云窗口结束后倍率没有恢复。");
    }

    /// <summary>把正式基础修行练至三重并选择指定特化，返回永久与临时两层运行状态。</summary>
    private static (RunModifierState, PassiveSpecializationState) CreateState(
        string upgradeId,
        string specializationId)
    {
        RunUpgradeDefinition definition = RunUpgradeCatalog.FindById(upgradeId)!;
        RunUpgradeSpecialization specialization = definition.Specializations.Single(
            item => item.Id == specializationId);
        var build = new RunBuildState();
        for (int rank = 0; rank < specialization.RequiredRank; rank++)
        {
            build.Apply(definition);
        }
        Require(build.ApplySpecialization(
            definition, specialization, specialization.MinimumRunLevel),
            $"无法选择测试特化：{specializationId}");
        var modifiers = new RunModifierState();
        modifiers.Refresh(build);
        return (modifiers, new PassiveSpecializationState(modifiers));
    }

    /// <summary>将契约失败转换为带明确中文原因的测试异常。</summary>
    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
