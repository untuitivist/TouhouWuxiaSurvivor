namespace TouhouWuxiaSurvivor.Ecs.Combat.Bosses;

/// <summary>
/// 隔离 ECS 与内容目录；战斗系统只按角色身份和血量阶段请求不可变攻击档案。
/// </summary>
public interface IBossAttackResolver
{
    /// <summary>为指定角色与阶段返回符卡攻击；尚未制作的角色返回 false 并沿用通用弹幕。</summary>
    bool TryResolve(
        string characterId,
        BossBulletPhase phase,
        out BossAttackPattern pattern);
}
