using Godot;

namespace TouhouWuxiaSurvivor.Gameplay.SpellCards.Effects;

/// <summary>
/// 保存正式奥义执行器所需的视觉场景资源，使自动触发协调器不携带 Godot 资产依赖。
/// </summary>
public partial class SpellCardEffectAssets : Node
{
    [Export]
    public PackedScene? FantasySealOrbScene { get; set; }

    [Export]
    public PackedScene? SealingCircleScene { get; set; }

    /// <summary>取得必需的追踪灵玉场景；缺失时在世界装配阶段立即报告明确错误。</summary>
    public PackedScene RequireFantasySealOrb() => FantasySealOrbScene ??
        throw new InvalidOperationException("Fantasy Seal orb scene is not assigned.");

    /// <summary>取得必需的结界场景；缺失时在世界装配阶段立即报告明确错误。</summary>
    public PackedScene RequireSealingCircle() => SealingCircleScene ??
        throw new InvalidOperationException("Sealing circle scene is not assigned.");
}
