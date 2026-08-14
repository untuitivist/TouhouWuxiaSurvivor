using Godot;

namespace TouhouWuxiaSurvivor.Gameplay.SpellCards.Geometry;

/// <summary>
/// 提供几何策略共用的数量约束、投射物复制和方向运算，避免各策略自行解释预算。
/// </summary>
public static class SpellCardGeometryPlanning
{
    /// <summary>从已排序序列截取本次允许命中的不同目标，并始终返回独立数组。</summary>
    public static IReadOnlyList<Vector2> TakeImpacts(
        SpellCardGeometryRequest request,
        IEnumerable<Vector2> ordered) => ordered
            .Take(request.ImpactLimit)
            .ToArray();

    /// <summary>以最近目标方向作为几何主轴；没有目标时使用向右的稳定回退方向。</summary>
    public static Vector2 PrimaryDirection(SpellCardGeometryRequest request)
    {
        if (request.Candidates.Count == 0)
        {
            return Vector2.Right;
        }

        Vector2 direction = request.Origin.DirectionTo(request.Candidates[0]);
        return direction.IsZeroApprox() ? Vector2.Right : direction;
    }

    /// <summary>
    /// 按集中或分散语义建立轨迹；集中型复制同一落点到完整投射物预算，分散型每个落点一枚。
    /// </summary>
    public static IReadOnlyList<SpellCardTrajectory> CreateTrajectories(
        SpellCardGeometryRequest request,
        IReadOnlyList<Vector2> impacts,
        Func<int, int, Vector2, Vector2> spawnFactory,
        Func<int, float>? curvatureFactory = null)
    {
        if (impacts.Count == 0)
        {
            return [];
        }

        int count = request.Focused ? request.TargetCount : impacts.Count;
        var result = new SpellCardTrajectory[count];
        for (int index = 0; index < count; index++)
        {
            Vector2 target = impacts[request.Focused ? 0 : index];
            result[index] = new SpellCardTrajectory(
                spawnFactory(index, count, target),
                target,
                curvatureFactory?.Invoke(index) ?? 0.0f);
        }

        return result;
    }

    /// <summary>计算点相对主轴的有符号夹角，供扇面和贯线策略做确定性排序。</summary>
    public static float SignedAngle(Vector2 axis, Vector2 direction) =>
        Mathf.Atan2(axis.Cross(direction), axis.Dot(direction));
}
