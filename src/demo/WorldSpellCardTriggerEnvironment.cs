using Godot;
using TouhouWuxiaSurvivor.Actors.Player;
using TouhouWuxiaSurvivor.Ecs.Combat;
using TouhouWuxiaSurvivor.Gameplay.SpellCards.Contracts;

namespace TouhouWuxiaSurvivor.Demo;

/// <summary>
/// 把正式世界的玩家与 ECS 状态适配为奥义触发窄接口，不向触发策略泄露场景节点或实体池。
/// </summary>
public sealed class WorldSpellCardTriggerEnvironment : ISpellCardTriggerEnvironment
{
    private const float CrowdEvaluationInterval = 0.2f;
    private readonly Node2D _player;
    private readonly PlayerHealth _health;
    private readonly EcsCombatWorld _ecsWorld;

    public long DamageRevision => _health.DamageRevision;
    public float CrowdEvaluationIntervalSeconds => CrowdEvaluationInterval;

    /// <summary>保存与本局同寿命的正式战斗组件；适配器只做查询，不拥有这些节点。</summary>
    public WorldSpellCardTriggerEnvironment(
        Node2D player,
        PlayerHealth health,
        EcsCombatWorld ecsWorld)
    {
        _player = player ?? throw new ArgumentNullException(nameof(player));
        _health = health ?? throw new ArgumentNullException(nameof(health));
        _ecsWorld = ecsWorld ?? throw new ArgumentNullException(nameof(ecsWorld));
    }

    /// <summary>以玩家当前位置无分配统计范围内存活敌人，实际阈值由角色基础承载与卡牌倍率决定。</summary>
    public int CountEnemiesInRange(float effectRange) =>
        _ecsWorld.CountEnemiesInRange(_player.GlobalPosition, effectRange);
}
