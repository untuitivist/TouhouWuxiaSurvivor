using Godot;
using TouhouWuxiaSurvivor.Actors.Player;
using TouhouWuxiaSurvivor.Combat.Projectiles;
using TouhouWuxiaSurvivor.Combat.Targeting;
using TouhouWuxiaSurvivor.Ecs.Combat.Projectiles;
using TouhouWuxiaSurvivor.Ecs.Combat;
using TouhouWuxiaSurvivor.Gameplay.Difficulty;
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
    private PassiveSpecializationState? _passiveSpecializations;
    private double _cooldownLeft;
    private double _localElapsedSeconds;
    private long _volleySequence;
    private bool _wasSpiralActive;
    private float _lastFireRateMultiplier = 1.0f;

    [Export]
    public PackedScene? ProjectileScene { get; set; }

    [Export(PropertyHint.Range, "0.05,5.0,0.05")]
    public float BaseFireInterval { get; set; } = 0.28f;

    [Export(PropertyHint.Range, "32,1200,8")]
    public float TargetRange { get; set; } = 460.0f;

    [Export(PropertyHint.Range, "32,1200,8")]
    public float ProjectileSpeed { get; set; } = 360.0f;

    [Export(PropertyHint.Range, "1,100,1")]
    public int Damage { get; set; } = 10;

    public float CharacterAttackMultiplier { get; set; } = 1.0f;

    /// <summary>获取或设置角色自身普攻间隔倍率；低于一更快，高于一更慢。</summary>
    public float CharacterAttackIntervalMultiplier { get; set; } = 1.0f;

    [Export(PropertyHint.Range, "0,64,1")]
    public float SpawnDistance { get; set; } = 18.0f;

    public int ShotsFired { get; private set; }
    /// <summary>获取最近一次实际生成的弹丸数；性能降级或没有目标时为零。</summary>
    public int LastVolleyProjectileCount { get; private set; }
    /// <summary>获取最近一次规划齐射的总伤与单弹范围，供状态面板读取正式整数预算。</summary>
    public ProjectileVolleyDamageSnapshot CurrentVolleyDamage { get; private set; }
    /// <summary>获取最近一次纯函数生成的弹幕计划，供调试界面和自动测试读取。</summary>
    public PlayerBarrageSnapshot CurrentBarrage { get; private set; }
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
        _passiveSpecializations = (GetParentOrNull<Node>() as PlayerController)?
            .PassiveSpecializations;
        _cooldownLeft = AutoAttackCadence.InitialDelaySeconds;
        _localElapsedSeconds = 0.0;
        _volleySequence = 0L;
        LastVolleyProjectileCount = 0;
    }

    /// <summary>
    /// 读取 ECS 生存时间和现有强化推进冷却，自动选择单发、扇形或旋转弹幕，全程不读取操作输入。
    /// </summary>
    public override void _Process(double delta)
    {
        if (_targetFinder is null || _health?.IsDead == true ||
            (_ecsWorld is null && _ecsProjectiles is null &&
                (_projectileContainer is null || ProjectileScene is null)))
        {
            return;
        }

        _localElapsedSeconds += Math.Max(0.0, delta);
        _passiveSpecializations?.AdvanceCombat(delta);
        bool spiralActive = _buffs?.IsSpiralActive == true ||
            _runModifiers?.UsesSpiralPattern == true;
        if (spiralActive && !_wasSpiralActive)
        {
            _volleySequence = 0L;
        }

        _wasSpiralActive = spiralActive;
        float fireRate = GetEffectiveFireRate();
        double effectiveInterval = AutoAttackCadence.CalculateInterval(
            BaseFireInterval, CharacterAttackIntervalMultiplier, fireRate);
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

        double elapsedSeconds = _ecsWorld?.ElapsedSeconds ?? _localElapsedSeconds;
        CurrentBarrage = PlayerBarrageCurve.EvaluateSeconds(elapsedSeconds,
            spiralActive, _volleySequence, GetActiveProjectileCount(),
            _runModifiers?.ExtraProjectiles ?? 0);
        LastVolleyProjectileCount = 0;
        if (CurrentBarrage.ProjectileCount == 0)
        {
            _cooldownLeft = CurrentBarrage.RetryIntervalSeconds;
            return;
        }

        float effectiveRange = TargetRange * (_runModifiers?.TargetRangeMultiplier ?? 1.0f);
        Vector2 targetPosition = default;
        bool hasTarget = _ecsWorld?.TryFindNearest(GlobalPosition, effectiveRange, out targetPosition) == true;
        var target = hasTarget ? null : _targetFinder.FindNearest(GlobalPosition, effectiveRange);
        if (!hasTarget && target is null && CurrentBarrage.RequiresTarget)
        {
            _volleySequence++;
            _cooldownLeft = 0.1;
            return;
        }

        Vector2 baseDirection = hasTarget
            ? GlobalPosition.DirectionTo(targetPosition)
            : target is not null
                ? GlobalPosition.DirectionTo(target.GlobalPosition)
                : Vector2.Right;
        LastVolleyProjectileCount = FireVolley(baseDirection, CurrentBarrage);
        _volleySequence++;
        if (LastVolleyProjectileCount > 0)
        {
            _passiveSpecializations?.RegisterVolley();
            VolleyFired?.Invoke();
        }

        _cooldownLeft = effectiveInterval;
    }

    /// <summary>
    /// 合成临时掉落与局内构筑的射速倍率，并整理非有限结果，避免冷却进入非数状态。
    /// </summary>
    private float GetEffectiveFireRate()
    {
        float temporaryMultiplier = _buffs?.FireRateMultiplier ?? 1.0f;
        float buildMultiplier = _runModifiers?.FireRateMultiplier ?? 1.0f;
        float passiveMultiplier = _passiveSpecializations?.FireRateMultiplier ?? 1.0f;
        float combined = temporaryMultiplier * buildMultiplier * passiveMultiplier;
        return float.IsFinite(combined) ? Math.Max(0.1f, combined) : 100.0f;
    }

    /// <summary>
    /// 读取当前后端活跃弹丸数，使 Godot 节点、独立投射物 ECS 和整合战斗 ECS 共用相同安全预算。
    /// </summary>
    private int GetActiveProjectileCount()
    {
        if (_ecsWorld is not null)
        {
            return _ecsWorld.ProjectileCount;
        }

        return _ecsProjectiles?.ActiveCount ?? _projectileContainer?.GetChildCount() ?? 0;
    }

    /// <summary>
    /// 按计划生成面向目标的单发或扇形，或者均分整圆的旋转环，并返回实际成功生成数量。
    /// </summary>
    private int FireVolley(Vector2 baseDirection, PlayerBarrageSnapshot barrage)
    {
        Vector2 aimedDirection = baseDirection.IsZeroApprox() ? Vector2.Right : baseDirection.Normalized();
        CurrentVolleyDamage = ProjectVolleyDamage(barrage);
        int spawned = 0;
        if (barrage.Mode == PlayerBarrageMode.RotatingRing)
        {
            double step = Math.Tau / barrage.ProjectileCount;
            for (int index = 0; index < barrage.ProjectileCount; index++)
            {
                float angle = (float)(barrage.RotationRadians + step * index);
                spawned += SpawnProjectile(
                    Vector2.Right.Rotated(angle),
                    CurrentVolleyDamage.GetPrimaryDamage(index),
                    CurrentVolleyDamage.GetSecondaryDamage(index)) ? 1 : 0;
            }

            return spawned;
        }

        double center = (barrage.ProjectileCount - 1) * 0.5;
        for (int index = 0; index < barrage.ProjectileCount; index++)
        {
            float angle = (float)(barrage.RotationRadians +
                (index - center) * barrage.AngularStepRadians);
            spawned += SpawnProjectile(
                aimedDirection.Rotated(angle),
                CurrentVolleyDamage.GetPrimaryDamage(index),
                CurrentVolleyDamage.GetSecondaryDamage(index)) ? 1 : 0;
        }

        return spawned;
    }

    /// <summary>
    /// 向当前兼容后端写入单颗子弹，钳制极端速度与伤害溢出，并返回是否确实完成生成。
    /// </summary>
    private bool SpawnProjectile(Vector2 direction, int damage, int secondaryHitDamage)
    {
        if (_ecsWorld is null && _ecsProjectiles is null && (ProjectileScene is null || _projectileContainer is null))
        {
            return false;
        }

        float speed = GetEffectiveProjectileSpeed();
        Vector2 spawnPosition = GlobalPosition + direction.Normalized() * SpawnDistance;
        int maximumHits = secondaryHitDamage > 0
            ? 1 + (_runModifiers?.ProjectilePierceCount ?? 0)
            : 1;
        if (_ecsWorld is not null)
        {
            _ecsWorld.SpawnProjectile(spawnPosition, direction, speed, damage,
                maximumHits, secondaryHitDamage);
        }
        else if (_ecsProjectiles is not null)
        {
            _ecsProjectiles.Spawn(spawnPosition, direction, speed, damage,
                maximumHits, secondaryHitDamage);
        }
        else
        {
            var projectile = ProjectileScene!.Instantiate<PlayerProjectile>();
            projectile.Configure(direction, speed, damage, maximumHits, secondaryHitDamage);
            _projectileContainer!.AddChild(projectile);
            projectile.GlobalPosition = spawnPosition;
        }

        ShotsFired++;
        return true;
    }

    /// <summary>
    /// 把整轮伤害预算均匀拆到实际弹丸，余数优先分配给前几发以保持整数总伤稳定。
    /// </summary>
    public ProjectileVolleyDamageSnapshot ProjectVolleyDamage(
        PlayerBarrageSnapshot barrage)
    {
        double rawSingle = ((double)Damage + (_runModifiers?.DamageBonus ?? 0)) *
            (_runModifiers?.AttackPowerMultiplier ?? 1.0f) *
            (_passiveSpecializations?.AttackPowerMultiplier ?? 1.0f) *
            Math.Max(0.01f, CharacterAttackMultiplier);
        int maximumHits = 1 + (_runModifiers?.ProjectilePierceCount ?? 0);
        return ProjectileDamageBudget.Project(
            rawSingle, barrage.VolleyDamageBudget,
            barrage.ProjectileCount, maximumHits);
    }

    /// <summary>
    /// 返回构筑倍率经过正式运动上限后的弹速，状态面板与运行时可据此避免显示不可兑现的数值。
    /// </summary>
    public float GetEffectiveProjectileSpeed() => ProjectileKinematicsPolicy.NormalizeSpeed(
        ProjectileSpeed * (_runModifiers?.ProjectileSpeedMultiplier ?? 1.0f));

    /// <summary>暴露当前条件射速倍率，供奥义与状态面板复用而不读取临时掉落强化。</summary>
    public float PassiveFireRateMultiplier =>
        _passiveSpecializations?.FireRateMultiplier ?? 1.0f;

    /// <summary>暴露当前凝神攻势倍率，使奥义与普通齐射在同一时刻继承相同构筑状态。</summary>
    public float PassiveAttackPowerMultiplier =>
        _passiveSpecializations?.AttackPowerMultiplier ?? 1.0f;
}
