using Godot;

namespace TouhouWuxiaSurvivor.Gameplay.SpellCards.Geometry;

/// <summary>
/// 保存几何策略输出的不同命中点、投射物轨迹和演出中心，不包含任何额外数值倍率。
/// </summary>
public sealed class SpellCardGeometryPlan
{
    public IReadOnlyList<Vector2> ImpactTargets { get; }
    public IReadOnlyList<SpellCardTrajectory> Projectiles { get; }
    public Vector2 VisualCenter { get; }

    /// <summary>复制规划集合，避免后续候选列表变化影响已经开始的符卡施展。</summary>
    public SpellCardGeometryPlan(
        IEnumerable<Vector2> impactTargets,
        IEnumerable<SpellCardTrajectory> projectiles,
        Vector2 visualCenter)
    {
        ImpactTargets = impactTargets.ToArray();
        Projectiles = projectiles.ToArray();
        VisualCenter = visualCenter;
    }
}
