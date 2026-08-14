using Godot;
using TouhouWuxiaSurvivor.Actors.Enemies;
using TouhouWuxiaSurvivor.Ecs.Combat;
using TouhouWuxiaSurvivor.Gameplay.SpellCards.Geometry;

namespace TouhouWuxiaSurvivor.Gameplay.SpellCards.Effects;

/// <summary>
/// 适配 ECS 与兼容节点两种战斗后端，为几何策略提供相同的候选查询和按点伤害语义。
/// </summary>
public sealed class SpellCardCombatBackend
{
    private readonly Node2D _enemies;
    private readonly EcsCombatWorld? _ecsWorld;

    /// <summary>保存战斗容器与可选 ECS 世界；正式游戏优先使用连续数据池。</summary>
    public SpellCardCombatBackend(Node2D enemies, EcsCombatWorld? ecsWorld)
    {
        _enemies = enemies ?? throw new ArgumentNullException(nameof(enemies));
        _ecsWorld = ecsWorld;
    }

    /// <summary>返回射程内全部候选位置并按离玩家距离排序，让几何策略能够重新组织顺序。</summary>
    public IReadOnlyList<Vector2> SelectCandidates(Vector2 origin, float range) =>
        SelectCandidateTargets(origin, range)
            .Select(target => target.InitialPosition)
            .ToArray();

    /// <summary>返回射程内候选的稳定身份和初始位置，让几何仍按坐标规划而投射物能够跨帧追踪。</summary>
    public IReadOnlyList<SpellCardTargetReference> SelectCandidateTargets(
        Vector2 origin,
        float range)
    {
        if (_ecsWorld is not null)
        {
            return _ecsWorld.SelectEnemyTargets(origin, range)
                .Select(target => SpellCardTargetReference.FromEcs(
                    target.Entity, target.Position))
                .ToArray();
        }

        return SpellCardTargetSelector.SelectNearest(
                _enemies, origin, range, int.MaxValue)
            .Select(SpellCardTargetReference.FromLegacy)
            .ToArray();
    }

    /// <summary>为集中型奥义返回最高威胁目标；无候选时返回空集合以维持失败重试语义。</summary>
    public IReadOnlyList<Vector2> SelectHighestThreat(Vector2 origin, float range)
        => SelectHighestThreatTargets(origin, range)
            .Select(target => target.InitialPosition)
            .ToArray();

    /// <summary>返回集中型奥义的稳定最高威胁身份；无候选时保持空集合和协调器重试语义。</summary>
    public IReadOnlyList<SpellCardTargetReference> SelectHighestThreatTargets(
        Vector2 origin,
        float range)
    {
        if (_ecsWorld is not null)
        {
            return _ecsWorld.TryFindHighestThreatTarget(
                    origin, range, out var entity, out Vector2 position)
                ? [SpellCardTargetReference.FromEcs(entity, position)]
                : [];
        }
        EnemyActor? target = SpellCardTargetSelector.SelectHighestThreat(
            _enemies, origin, range);
        return target is null ? [] : [SpellCardTargetReference.FromLegacy(target)];
    }

    /// <summary>读取目标的最新活体位置；死亡、释放或 ECS 回收后返回 false，不会重新选取其他敌人。</summary>
    public bool TryGetTargetPosition(
        SpellCardTargetReference target,
        out Vector2 position)
    {
        ArgumentNullException.ThrowIfNull(target);
        if (_ecsWorld is not null && target.EcsEntity.IsValid)
        {
            return _ecsWorld.TryGetEnemyPosition(target.EcsEntity, out position);
        }

        EnemyActor? actor = target.LegacyActor;
        if (actor is not null && GodotObject.IsInstanceValid(actor) && actor.IsAlive)
        {
            position = actor.GlobalPosition;
            return true;
        }

        position = default;
        return false;
    }

    /// <summary>按稳定身份对仍存活的原目标施加伤害，目标失效时拒绝伤害邻近替代目标。</summary>
    public bool DamageTarget(SpellCardTargetReference target, int damage)
    {
        ArgumentNullException.ThrowIfNull(target);
        if (_ecsWorld is not null && target.EcsEntity.IsValid)
        {
            return _ecsWorld.DamageEnemy(target.EcsEntity, damage);
        }

        EnemyActor? actor = target.LegacyActor;
        return actor is not null && GodotObject.IsInstanceValid(actor) &&
            actor.IsAlive && actor.ReceiveDamage(damage);
    }

    /// <summary>
    /// 将几何轨迹的落点映射回本次候选身份；派生出来且不等于敌人原坐标的落点明确保持固定。
    /// </summary>
    public static SpellCardTargetReference? MatchTarget(
        Vector2 targetPosition,
        IReadOnlyList<SpellCardTargetReference> candidates,
        ISet<SpellCardTargetReference>? assignedTargets = null)
    {
        SpellCardTargetReference? match = candidates.FirstOrDefault(candidate =>
            candidate.InitialPosition.IsEqualApprox(targetPosition) &&
            (assignedTargets is null || !assignedTargets.Contains(candidate)));
        if (match is not null)
        {
            assignedTargets?.Add(match);
        }

        return match;
    }

    /// <summary>在指定命中点结算一个目标，命中半径取至少一点以兼容零范围的集中符卡。</summary>
    public bool DamageAt(Vector2 position, float impactRange, int damage)
    {
        float radius = Math.Max(1.0f, impactRange);
        if (_ecsWorld is not null)
        {
            return _ecsWorld.DamageNearestEnemies(position, radius, damage, 1, 1.0f) > 0;
        }

        EnemyActor? target = SpellCardTargetSelector.SelectNearest(
            _enemies, position, radius, 1).FirstOrDefault();
        return target is not null && target.ReceiveDamage(damage);
    }

    /// <summary>
    /// 对几何规划选中的不同落点逐一结算距离衰减伤害，目标数量由规划阶段严格限制。
    /// </summary>
    public int DamageImpacts(
        Vector2 origin,
        SpellCardGeometryPlan plan,
        int damage,
        float range,
        float minimumMultiplier)
    {
        int hits = 0;
        float radius = Math.Max(1.0f, range);
        foreach (Vector2 position in plan.ImpactTargets)
        {
            float distance = position.DistanceTo(origin);
            int scaled = AreaDamageSystem.CalculateDamage(
                damage, distance, radius, minimumMultiplier);
            hits += DamageAt(position, 1.0f, scaled) ? 1 : 0;
        }

        return hits;
    }
}
