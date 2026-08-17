using Godot;
using TouhouWuxiaSurvivor.Gameplay.SpellCards.Definitions;

namespace TouhouWuxiaSurvivor.Ui.Compendium;

/// <summary>
/// 把奥义的正式阵式语义投影为图鉴动画位置与速度；只负责演出，不复制战斗伤害规则。
/// </summary>
public static class CompendiumSpellPreviewLayout
{
    /// <summary>按阵式、弹丸序号和动画时刻返回预览位置与瞬时飞行方向。</summary>
    public static (Vector2 Position, Vector2 Velocity) Resolve(
        SpellCardGeometryKind geometry,
        int index,
        int count,
        double animationTime,
        Rect2 area)
    {
        Vector2 center = area.GetCenter() - new Vector2(0.0f, 5.0f);
        float radius = MathF.Max(18.0f, MathF.Min(area.Size.X, area.Size.Y) * 0.34f);
        float time = (float)animationTime;
        return geometry switch
        {
            SpellCardGeometryKind.Orbit => ResolveOrbit(center, radius, index, count, time),
            SpellCardGeometryKind.Fan => ResolveFan(center, radius, index, count, time),
            SpellCardGeometryKind.Line => ResolveLine(center, radius, index, count, time),
            SpellCardGeometryKind.Ring => ResolveRing(center, radius, index, count, time),
            SpellCardGeometryKind.Backstab => ResolveBackstab(center, radius, index, count, time),
            _ => throw new ArgumentOutOfRangeException(nameof(geometry)),
        };
    }

    /// <summary>让环身阵沿切线巡游，方向弹的朝向会连续跟随圆周轨迹。</summary>
    private static (Vector2, Vector2) ResolveOrbit(
        Vector2 center, float radius, int index, int count, float time)
    {
        float angle = time * 1.7f + Mathf.Tau * index / count;
        Vector2 radial = Vector2.FromAngle(angle);
        return (center + radial * radius * 0.72f,
            new Vector2(-radial.Y, radial.X) * radius * 1.7f);
    }

    /// <summary>让扇面阵从自身向右展开，弹道角度由序号稳定分布。</summary>
    private static (Vector2, Vector2) ResolveFan(
        Vector2 center, float radius, int index, int count, float time)
    {
        float angle = Mathf.Lerp(-0.68f, 0.68f, count <= 1 ? 0.5f : index / (count - 1.0f));
        Vector2 direction = Vector2.FromAngle(angle);
        float progress = PositiveFraction(time * 0.55f + index * 0.045f);
        return (center + direction * Mathf.Lerp(5.0f, radius, progress), direction);
    }

    /// <summary>让贯线阵保持平行队列向前推进，避免误画成环绕弹。</summary>
    private static (Vector2, Vector2) ResolveLine(
        Vector2 center, float radius, int index, int count, float time)
    {
        float progress = PositiveFraction(time * 0.62f + index * 0.08f);
        float lane = (index - (count - 1) * 0.5f) * 5.0f;
        return (center + new Vector2(Mathf.Lerp(-radius, radius, progress), lane), Vector2.Right);
    }

    /// <summary>让扩环阵由自身向外辐射，姿态使用真实径向速度。</summary>
    private static (Vector2, Vector2) ResolveRing(
        Vector2 center, float radius, int index, int count, float time)
    {
        float angle = Mathf.Tau * index / count;
        Vector2 direction = Vector2.FromAngle(angle);
        float progress = PositiveFraction(time * 0.48f);
        return (center + direction * Mathf.Lerp(4.0f, radius, progress), direction);
    }

    /// <summary>让背袭阵从目标两侧向中心夹击，交替方向可直观看出前后姿态。</summary>
    private static (Vector2, Vector2) ResolveBackstab(
        Vector2 center, float radius, int index, int count, float time)
    {
        float side = index % 2 == 0 ? -1.0f : 1.0f;
        float progress = PositiveFraction(time * 0.52f + index * 0.06f);
        float y = (index - (count - 1) * 0.5f) * 4.0f;
        Vector2 direction = new(-side, 0.0f);
        return (center + new Vector2(side * Mathf.Lerp(radius, 4.0f, progress), y), direction);
    }

    /// <summary>返回对负时间偏移也稳定落在零到一之间的循环进度。</summary>
    private static float PositiveFraction(float value) => value - MathF.Floor(value);
}
