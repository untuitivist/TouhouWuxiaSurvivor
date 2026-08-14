using TouhouWuxiaSurvivor.Gameplay.SpellCards.Definitions;

namespace TouhouWuxiaSurvivor.Gameplay.SpellCards.Geometry;

/// <summary>
/// 把候选目标与实效参数规划为空间命中方案，使内容数据可以组合效果类型和独立几何。
/// </summary>
public interface ISpellCardGeometryStrategy
{
    SpellCardGeometryKind Kind { get; }

    /// <summary>在不改变伤害和目标预算的前提下生成确定性的选敌、起手位置与轨迹。</summary>
    SpellCardGeometryPlan CreatePlan(SpellCardGeometryRequest request);
}
