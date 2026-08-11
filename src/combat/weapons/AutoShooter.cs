using Godot;
using TouhouWuxiaSurvivor.Actors.Player;
using TouhouWuxiaSurvivor.Combat.Projectiles;
using TouhouWuxiaSurvivor.Combat.Targeting;
using TouhouWuxiaSurvivor.Ecs.Combat.Projectiles;
using TouhouWuxiaSurvivor.Ecs.Combat;
using TouhouWuxiaSurvivor.Gameplay.Progression.Runtime;

namespace TouhouWuxiaSurvivor.Combat.Weapons;

/// <summary>
/// 周期性选择射程内最近敌人并自动射击，完全不需要玩家提供瞄准或开火输入。
/// </summary>
public partial class AutoShooter : Node2D
{
    private NearestEnemyTargetFinder? _targetFinder;
    private Node2D? _projectileContainer;
    private PlayerBuffController? _buffs;
    private PlayerHealth? _health;
    private RunModifierState? _runModifiers;
    private ProjectileEcsRuntime? _ecsProjectiles;
    private EcsCombatWorld? _ecsWorld;
    private double _cooldownLeft;
    private float _spiralPhase;
    private bool _wasSpiralActive;
    private float _lastFireRateMultiplier = 1.0f;

    [Export]
    public PackedScene? ProjectileScene { get; set; }

    [Export(PropertyHint.Range, "0.05,5.0,0.05")]
    public float BaseFireInterval { get; set; } = 0.18f;

    [Export(PropertyHint.Range, "32,1200,8")]
    public float TargetRange { get; set; } = 460.0f;

    [Export(PropertyHint.Range, "32,1200,8")]
    public float ProjectileSpeed { get; set; } = 360.0f;

    [Export(PropertyHint.Range, "1,100,1")]
    public int Damage { get; set; } = 1;

    [Export(PropertyHint.Range, "0,64,1")]
    public float SpawnDistance { get; set; } = 18.0f;

    public int ShotsFired { get; private set; }
    public event Action? VolleyFired;

    /// <summary>
    /// 绑定本局敌人和投射物容器以及玩家强化状态，为后续自动射击建立显式依赖。
    /// </summary>
    public void Configure(
        Node2D enemyContainer,
        Node2D projectileContainer,
        PlayerBuffController buffs,
        PlayerHealth health,
        RunModifierState runModifiers,
        ProjectileEcsRuntime? ecsProjectiles = null,
        EcsCombatWorld? ecsWorld = null)
    {
        _targetFinder = new NearestEnemyTargetFinder(enemyContainer);
        _projectileContainer = projectileContainer;
        _buffs = buffs;
        _health = health;
        _runModifiers = runModifiers;
        _ecsProjectiles = ecsProjectiles;
        _ecsWorld = ecsWorld;
        _cooldownLeft = 0.15;
    }

    /// <summary>
    /// 读取当前强化并推进武器冷却；强化螺旋弹幕可在没有目标时发射，普通攻击只射击最近目标。
    /// </summary>
    public override void _Process(double delta)
    {
        if (_targetFinder is null || _health?.IsDead == true ||
            (_ecsWorld is null && _ecsProjectiles is null &&
                (_projectileContainer is null || ProjectileScene is null)))
        {
            return;
        }

        bool spiralActive = _buffs?.IsSpiralActive == true;
        if (spiralActive && !_wasSpiralActive)
        {
            _spiralPhase = 0.0f;
        }

        _wasSpiralActive = spiralActive;
        float temporaryFireRate = _buffs?.FireRateMultiplier ?? 1.0f;
        float fireRate = Math.Max(0.1f,
            temporaryFireRate * (_runModifiers?.FireRateMultiplier ?? 1.0f));
        double effectiveInterval = Math.Max(0.01f, BaseFireInterval / fireRate);
        if (fireRate > _lastFireRateMultiplier)
        {
            _cooldownLeft = Math.Min(_cooldownLeft, effectiveInterval);
        }

        _lastFireRateMultiplier = fireRate;
        _cooldownLeft -= delta;
        if (_cooldownLeft > 0.0)
        {
            return;
        }

        if (spiralActive)
        {
            FireSpiralVolley();
            VolleyFired?.Invoke();
            _cooldownLeft = effectiveInterval;
            return;
        }

        float effectiveRange = TargetRange * (_runModifiers?.TargetRangeMultiplier ?? 1.0f);
        Vector2 targetPosition = default;
        bool hasTarget = _ecsWorld?.TryFindNearest(GlobalPosition, effectiveRange, out targetPosition) == true;
        var target = hasTarget ? null : _targetFinder.FindNearest(GlobalPosition, effectiveRange);
        if (!hasTarget && target is null)
        {
            _cooldownLeft = 0.1;
            return;
        }

        FireAt(hasTarget ? targetPosition : target!.GlobalPosition);
        VolleyFired?.Invoke();
        _cooldownLeft = effectiveInterval;
    }

    /// <summary>
    /// 计算目标方向并生成一颗普通自动索敌子弹。
    /// </summary>
    private void FireAt(Vector2 targetPosition)
    {
        Vector2 baseDirection = GlobalPosition.DirectionTo(targetPosition);
        SpawnProjectile(baseDirection);
    }

    /// <summary>
    /// 按当前相位向正反两个方向发射，并以 π/12 推进相位形成与示例一致的旋转弹幕。
    /// </summary>
    private void FireSpiralVolley()
    {
        Vector2 direction = Vector2.Right.Rotated(_spiralPhase);
        SpawnProjectile(direction);
        SpawnProjectile(-direction);
        _spiralPhase = Mathf.Wrap(_spiralPhase + Mathf.Pi / 12.0f, 0.0f, Mathf.Tau);
    }

    /// <summary>
    /// 实例化单颗子弹、设置全局发射位置和战斗参数，并累计可观测的发射计数。
    /// </summary>
    private void SpawnProjectile(Vector2 direction)
    {
        if (_ecsWorld is null && _ecsProjectiles is null && (ProjectileScene is null || _projectileContainer is null))
        {
            return;
        }

        float speed = ProjectileSpeed * (_runModifiers?.ProjectileSpeedMultiplier ?? 1.0f);
        int damage = Damage + (_runModifiers?.DamageBonus ?? 0);
        Vector2 spawnPosition = GlobalPosition + direction.Normalized() * SpawnDistance;
        if (_ecsWorld is not null)
        {
            _ecsWorld.SpawnProjectile(spawnPosition, direction, speed, damage);
        }
        else if (_ecsProjectiles is not null)
        {
            _ecsProjectiles.Spawn(spawnPosition, direction, speed, damage);
        }
        else
        {
            var projectile = ProjectileScene!.Instantiate<PlayerProjectile>();
            projectile.Configure(direction, speed, damage);
            _projectileContainer!.AddChild(projectile);
            projectile.GlobalPosition = spawnPosition;
        }

        ShotsFired++;
    }
}
