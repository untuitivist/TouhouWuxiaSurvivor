namespace TouhouWuxiaSurvivor.Gameplay.Progression.Runtime;

/// <summary>
/// 保存疾息、凝神、逐风和流云的短时战斗状态；永久构筑仍由 RunModifierState 单独拥有。
/// </summary>
public sealed class PassiveSpecializationState
{
    private readonly RunModifierState _modifiers;
    private float _fireMomentum;
    private float _focusMomentum;
    private float _movementMomentum;
    private double _fireGraceLeft;
    private double _spiritFlowLeft;

    /// <summary>绑定永久构筑投影，使后续选择特化时无需重新创建临时状态。</summary>
    public PassiveSpecializationState(RunModifierState modifiers) =>
        _modifiers = modifiers ?? throw new ArgumentNullException(nameof(modifiers));

    public float FireRateMultiplier => _modifiers.UsesContinuousFireMomentum
        ? PassiveSpecializationPolicy.Scale(
            _fireMomentum, PassiveSpecializationPolicy.ContinuousFireBonus)
        : 1.0f;

    public float AttackPowerMultiplier => _modifiers.UsesStationaryFocus
        ? PassiveSpecializationPolicy.Scale(
            _focusMomentum, PassiveSpecializationPolicy.StationaryFocusBonus)
        : 1.0f;

    public float MoveSpeedMultiplier =>
        (_modifiers.UsesMovementMomentum
            ? PassiveSpecializationPolicy.Scale(
                _movementMomentum, PassiveSpecializationPolicy.MovementMomentumBonus)
            : 1.0f) *
        (_modifiers.UsesSpiritFlow && _spiritFlowLeft > 0.0
            ? 1.0f + PassiveSpecializationPolicy.SpiritFlowBonus
            : 1.0f);

    public float FireMomentum => _fireMomentum;
    public float FocusMomentum => _focusMomentum;
    public float MovementMomentum => _movementMomentum;
    public bool SpiritFlowActive => _modifiers.UsesSpiritFlow && _spiritFlowLeft > 0.0;

    /// <summary>推进连续射击蓄势；超过宽限期没有成功齐射时按较快节奏衰减。</summary>
    public void AdvanceCombat(double delta)
    {
        double step = Math.Max(0.0, delta);
        _fireGraceLeft = Math.Max(0.0, _fireGraceLeft - step);
        if (!_modifiers.UsesContinuousFireMomentum)
        {
            _fireMomentum = 0.0f;
            return;
        }

        double duration = _fireGraceLeft > 0.0
            ? PassiveSpecializationPolicy.FireChargeSeconds
            : PassiveSpecializationPolicy.FireDecaySeconds;
        float target = _fireGraceLeft > 0.0 ? 1.0f : 0.0f;
        _fireMomentum = MoveToward(_fireMomentum, target, step / duration);
    }

    /// <summary>登记一次真正生成弹丸的齐射，空放和无目标重试不会维持疾息蓄势。</summary>
    public void RegisterVolley()
    {
        if (_modifiers.UsesContinuousFireMomentum)
        {
            _fireGraceLeft = PassiveSpecializationPolicy.FireGraceSeconds;
        }
    }

    /// <summary>按是否移动推进逐风与凝神，并在同一物理时钟中递减流云持续时间。</summary>
    public void AdvanceMovement(bool moving, double delta)
    {
        double step = Math.Max(0.0, delta);
        _focusMomentum = AdvanceConditional(
            _focusMomentum,
            _modifiers.UsesStationaryFocus && !moving,
            step,
            PassiveSpecializationPolicy.FocusChargeSeconds,
            PassiveSpecializationPolicy.FocusDecaySeconds);
        _movementMomentum = AdvanceConditional(
            _movementMomentum,
            _modifiers.UsesMovementMomentum && moving,
            step,
            PassiveSpecializationPolicy.MovementChargeSeconds,
            PassiveSpecializationPolicy.MovementDecaySeconds);
        _spiritFlowLeft = _modifiers.UsesSpiritFlow
            ? Math.Max(0.0, _spiritFlowLeft - step)
            : 0.0;
    }

    /// <summary>拾取任意灵息后刷新流云窗口；未选择流云势时不会保留隐藏状态。</summary>
    public void RegisterSpiritCollected(int value)
    {
        if (_modifiers.UsesSpiritFlow && value > 0)
        {
            _spiritFlowLeft = PassiveSpecializationPolicy.SpiritFlowDurationSeconds;
        }
    }

    private static float AdvanceConditional(
        float current,
        bool active,
        double delta,
        double chargeSeconds,
        double decaySeconds) => MoveToward(
            current,
            active ? 1.0f : 0.0f,
            delta / (active ? chargeSeconds : decaySeconds));

    private static float MoveToward(float current, float target, double amount) =>
        target > current
            ? Math.Min(target, current + (float)amount)
            : Math.Max(target, current - (float)amount);
}
