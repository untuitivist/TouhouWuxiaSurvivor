using Godot;
using TouhouWuxiaSurvivor.Gameplay.SpellCards.Definitions;

namespace TouhouWuxiaSurvivor.Gameplay.SpellCards.Geometry;

/// <summary>
/// 从玩家周身均匀起手并直线追向最近目标，形成稳定的周天灵玉编队。
/// </summary>
public sealed class OrbitSpellCardGeometry : ISpellCardGeometryStrategy
{
    public SpellCardGeometryKind Kind => SpellCardGeometryKind.Orbit;

    /// <summary>保留距离排序，并把每枚投射物均匀布置在玩家周围。</summary>
    public SpellCardGeometryPlan CreatePlan(SpellCardGeometryRequest request)
    {
        IReadOnlyList<Vector2> impacts = SpellCardGeometryPlanning.TakeImpacts(
            request, request.Candidates);
        IReadOnlyList<SpellCardTrajectory> trajectories =
            SpellCardGeometryPlanning.CreateTrajectories(
                request,
                impacts,
                (index, count, _) => request.Origin +
                    Vector2.FromAngle(Mathf.Tau * index / count) * request.SpawnDistance);
        return new SpellCardGeometryPlan(impacts, trajectories, request.Origin);
    }
}
