using TouhouWuxiaSurvivor.Gameplay.SpellCards.Definitions;

namespace TouhouWuxiaSurvivor.Gameplay.SpellCards.Geometry;

/// <summary>
/// 注册全部空间策略并以强类型键查询，新增机制只需加入一个独立策略类和注册项。
/// </summary>
public static class SpellCardGeometryCatalog
{
    private static readonly IReadOnlyDictionary<SpellCardGeometryKind, ISpellCardGeometryStrategy>
        Strategies = new ISpellCardGeometryStrategy[]
        {
            new OrbitSpellCardGeometry(),
            new FanSpellCardGeometry(),
            new LineSpellCardGeometry(),
            new RingSpellCardGeometry(),
            new BackstabSpellCardGeometry(),
        }.ToDictionary(strategy => strategy.Kind);

    /// <summary>返回指定几何的唯一策略；缺少注册时立即失败，避免游戏中静默回退。</summary>
    public static ISpellCardGeometryStrategy Get(SpellCardGeometryKind kind) =>
        Strategies.TryGetValue(kind, out ISpellCardGeometryStrategy? strategy)
            ? strategy
            : throw new InvalidOperationException($"Unregistered spell geometry: {kind}");

    /// <summary>暴露只读策略集合，供内容契约确认每个枚举都有实际执行实现。</summary>
    public static IReadOnlyCollection<ISpellCardGeometryStrategy> All =>
        Strategies.Values.ToArray();
}
