using Godot;
using TouhouWuxiaSurvivor.Gameplay.SpellCards.Definitions;

namespace TouhouWuxiaSurvivor.Gameplay.SpellCards.Geometry;

/// <summary>
/// 优先命中射程外缘的敌人，并让弹丸沿相反圆周弯入，形成收束或扩散的环形攻势。
/// </summary>
public sealed class RingSpellCardGeometry : ISpellCardGeometryStrategy
{
    public SpellCardGeometryKind Kind => SpellCardGeometryKind.Ring;

    /// <summary>从远到近选择外环目标，投射物起点均匀环绕且交替向内弯曲。</summary>
    public SpellCardGeometryPlan CreatePlan(SpellCardGeometryRequest request)
    {
        IEnumerable<Vector2> ordered = request.Candidates
            .OrderByDescending(position => request.Origin.DistanceSquaredTo(position));
        IReadOnlyList<Vector2> impacts = SpellCardGeometryPlanning.TakeImpacts(request, ordered);
        IReadOnlyList<SpellCardTrajectory> trajectories =
            SpellCardGeometryPlanning.CreateTrajectories(
                request,
                impacts,
                (index, count, _) => request.Origin +
                    Vector2.FromAngle(Mathf.Tau * index / count) * request.SpawnDistance,
                index => (index & 1) == 0 ? 1.35f : -1.35f);
        return new SpellCardGeometryPlan(impacts, trajectories, request.Origin);
    }
}
