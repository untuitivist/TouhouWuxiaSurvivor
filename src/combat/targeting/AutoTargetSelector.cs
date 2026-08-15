using Godot;
using TouhouWuxiaSurvivor.Actors.Enemies;
using TouhouWuxiaSurvivor.Ecs.Combat;

namespace TouhouWuxiaSurvivor.Combat.Targeting;

/// <summary>
/// 统一自动武器的 ECS 与节点兼容索敌，始终保持最近目标这一基础玩法规则。
/// </summary>
public static class AutoTargetSelector
{
    /// <summary>
    /// 优先读取正式 ECS 最近目标；没有 ECS 目标时回退旧节点容器，调用方只消费统一运动快照。
    /// </summary>
    public static bool TrySelect(
        EcsCombatWorld? world,
        NearestEnemyTargetFinder fallback,
        Vector2 origin,
        float range,
        out TargetMotion motion)
    {
        if (world?.TryFindNearestTarget(origin, range, out motion) == true)
        {
            return true;
        }

        EnemyActor? target = fallback.FindNearest(origin, range);
        if (target is not null)
        {
            motion = new TargetMotion(target.GlobalPosition, target.Velocity);
            return true;
        }

        motion = default;
        return false;
    }
}
