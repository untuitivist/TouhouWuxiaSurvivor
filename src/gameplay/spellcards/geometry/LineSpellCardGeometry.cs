using Godot;
using TouhouWuxiaSurvivor.Gameplay.SpellCards.Definitions;

namespace TouhouWuxiaSurvivor.Gameplay.SpellCards.Geometry;

/// <summary>
/// 沿最近目标方向建立贯线，优先选择轴线附近且由近到远排列的敌人。
/// </summary>
public sealed class LineSpellCardGeometry : ISpellCardGeometryStrategy
{
    public SpellCardGeometryKind Kind => SpellCardGeometryKind.Line;

    /// <summary>按垂直轴线距离和前向距离排序，并从玩家身后蓄势后直取目标。</summary>
    public SpellCardGeometryPlan CreatePlan(SpellCardGeometryRequest request)
    {
        Vector2 axis = SpellCardGeometryPlanning.PrimaryDirection(request);
        IEnumerable<Vector2> ordered = request.Candidates
            .Select(position =>
            {
                Vector2 delta = position - request.Origin;
                float forward = axis.Dot(delta);
                float lateral = Math.Abs(axis.Cross(delta));
                return (position, behind: forward < 0.0f, lateral, forward);
            })
            .OrderBy(item => item.behind)
            .ThenBy(item => item.lateral)
            .ThenBy(item => item.forward)
            .Select(item => item.position);
        IReadOnlyList<Vector2> impacts = SpellCardGeometryPlanning.TakeImpacts(request, ordered);
        IReadOnlyList<SpellCardTrajectory> trajectories =
            SpellCardGeometryPlanning.CreateTrajectories(
                request,
                impacts,
                (_, _, _) => request.Origin - axis * request.SpawnDistance);
        Vector2 center = impacts.Count == 0 ? request.Origin : impacts[^1];
        return new SpellCardGeometryPlan(impacts, trajectories, center);
    }
}
