using Godot;
using TouhouWuxiaSurvivor.Gameplay.SpellCards.Definitions;

namespace TouhouWuxiaSurvivor.Gameplay.SpellCards.Geometry;

/// <summary>
/// 在每个被选目标背向玩家的一侧生成攻势，再折返刺向目标，形成隙间或暗袭式夹击。
/// </summary>
public sealed class BackstabSpellCardGeometry : ISpellCardGeometryStrategy
{
    public SpellCardGeometryKind Kind => SpellCardGeometryKind.Backstab;

    /// <summary>仍按最近目标选敌，但每枚投射物从目标背后出现并带轻微交替弯曲。</summary>
    public SpellCardGeometryPlan CreatePlan(SpellCardGeometryRequest request)
    {
        IReadOnlyList<Vector2> impacts = SpellCardGeometryPlanning.TakeImpacts(
            request, request.Candidates);
        IReadOnlyList<SpellCardTrajectory> trajectories =
            SpellCardGeometryPlanning.CreateTrajectories(
                request,
                impacts,
                (index, _, target) =>
                {
                    Vector2 direction = request.Origin.DirectionTo(target);
                    if (direction.IsZeroApprox())
                    {
                        direction = Vector2.Right;
                    }

                    float side = (index & 1) == 0 ? 0.32f : -0.32f;
                    return target + direction * request.SpawnDistance +
                        direction.Orthogonal() * request.SpawnDistance * side;
                },
                index => (index & 1) == 0 ? -0.8f : 0.8f);
        Vector2 center = impacts.Count == 0 ? request.Origin : impacts[0];
        return new SpellCardGeometryPlan(impacts, trajectories, center);
    }
}
