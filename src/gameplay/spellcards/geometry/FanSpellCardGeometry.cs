using Godot;
using TouhouWuxiaSurvivor.Gameplay.SpellCards.Definitions;

namespace TouhouWuxiaSurvivor.Gameplay.SpellCards.Geometry;

/// <summary>
/// 以最近目标为扇面中轴，优先覆盖前方锥形区域，并让投射物从玩家身前横列散开。
/// </summary>
public sealed class FanSpellCardGeometry : ISpellCardGeometryStrategy
{
    private const float HalfArcRadians = Mathf.Pi * 0.42f;
    public SpellCardGeometryKind Kind => SpellCardGeometryKind.Fan;

    /// <summary>按是否落入扇面、夹角和距离排序，维持与其他几何相同的命中数量。</summary>
    public SpellCardGeometryPlan CreatePlan(SpellCardGeometryRequest request)
    {
        Vector2 axis = SpellCardGeometryPlanning.PrimaryDirection(request);
        IEnumerable<Vector2> ordered = request.Candidates
            .Select(position => (
                position,
                angle: Math.Abs(SpellCardGeometryPlanning.SignedAngle(
                    axis, request.Origin.DirectionTo(position))),
                distance: request.Origin.DistanceSquaredTo(position)))
            .OrderBy(item => item.angle > HalfArcRadians)
            .ThenBy(item => item.angle)
            .ThenBy(item => item.distance)
            .Select(item => item.position);
        IReadOnlyList<Vector2> impacts = SpellCardGeometryPlanning.TakeImpacts(request, ordered);
        Vector2 side = axis.Orthogonal();
        IReadOnlyList<SpellCardTrajectory> trajectories =
            SpellCardGeometryPlanning.CreateTrajectories(
                request,
                impacts,
                (index, count, _) =>
                {
                    float offset = index - (count - 1) * 0.5f;
                    return request.Origin + axis * request.SpawnDistance +
                        side * offset * request.SpawnDistance * 0.42f;
                });
        return new SpellCardGeometryPlan(impacts, trajectories, request.Origin);
    }
}
