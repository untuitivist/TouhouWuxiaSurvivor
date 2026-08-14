using Godot;

namespace TouhouWuxiaSurvivor.Gameplay.SpellCards.Geometry;

/// <summary>
/// 保存一次几何规划的全部输入；候选目标由施放后端提供，策略不接触场景树或 ECS 实体池。
/// </summary>
public sealed class SpellCardGeometryRequest
{
    public Vector2 Origin { get; }
    public IReadOnlyList<Vector2> Candidates { get; }
    public int TargetCount { get; }
    public float EffectRange { get; }
    public float SpawnDistance { get; }
    public bool Focused { get; }

    /// <summary>建立已经过边界整理的不可变请求，使五种策略使用完全相同的数量预算。</summary>
    public SpellCardGeometryRequest(
        Vector2 origin,
        IReadOnlyList<Vector2> candidates,
        int targetCount,
        float effectRange,
        float spawnDistance,
        bool focused)
    {
        Origin = origin;
        Candidates = candidates ?? throw new ArgumentNullException(nameof(candidates));
        TargetCount = Math.Max(1, targetCount);
        EffectRange = Math.Max(1.0f, effectRange);
        SpawnDistance = Math.Max(1.0f, spawnDistance);
        Focused = focused;
    }

    /// <summary>集中型只选择一个落点，其余类型最多选择目标预算数量个不同落点。</summary>
    public int ImpactLimit => Focused ? 1 : TargetCount;
}
