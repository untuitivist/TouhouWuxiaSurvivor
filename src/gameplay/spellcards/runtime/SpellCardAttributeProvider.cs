using TouhouWuxiaSurvivor.Actors.Player;
using TouhouWuxiaSurvivor.Combat.Weapons;
using TouhouWuxiaSurvivor.Content.Characters;
using TouhouWuxiaSurvivor.Ecs.Combat.Projectiles;
using TouhouWuxiaSurvivor.Gameplay.SpellCards.Contracts;
using TouhouWuxiaSurvivor.Gameplay.Progression.Runtime;

namespace TouhouWuxiaSurvivor.Gameplay.SpellCards.Runtime;

/// <summary>
/// 从当前自机、自动武器和本局构筑读取奥义基础属性，避免效果类各自重复倍率公式。
/// </summary>
public sealed class SpellCardAttributeProvider : ISpellCardAttributeProvider
{
    private readonly AutoShooter _shooter;
    private readonly RunModifierState _modifiers;
    private readonly PlayerHealth _health;
    private readonly PlayableCharacterProfile _character;

    /// <summary>保存正式运行组件引用；每次捕获都读取最新构筑倍率而不是缓存旧最终值。</summary>
    public SpellCardAttributeProvider(
        AutoShooter shooter,
        RunModifierState modifiers,
        PlayerHealth health,
        PlayableCharacterProfile character)
    {
        _shooter = shooter ?? throw new ArgumentNullException(nameof(shooter));
        _modifiers = modifiers ?? throw new ArgumentNullException(nameof(modifiers));
        _health = health ?? throw new ArgumentNullException(nameof(health));
        _character = character ?? throw new ArgumentNullException(nameof(character));
    }

    /// <summary>
    /// 合成角色攻势、局外伤害和本局倍率；临时拾取不进入奥义，避免短时强化改写整套周期。
    /// </summary>
    public SpellCardBaseAttributes Capture()
    {
        float fireRate = Math.Max(0.1f,
            _modifiers.FireRateMultiplier * _shooter.PassiveFireRateMultiplier);
        float fireInterval = _shooter.BaseFireInterval / fireRate;
        float attackPower = (_shooter.Damage + _modifiers.DamageBonus) *
            _modifiers.AttackPowerMultiplier *
            _shooter.PassiveAttackPowerMultiplier *
            Math.Max(0.01f, _shooter.CharacterAttackMultiplier);
        return new SpellCardBaseAttributes(
            attackPower,
            fireInterval,
            _shooter.TargetRange * _modifiers.TargetRangeMultiplier,
            ProjectileKinematicsPolicy.NormalizeSpeed(
                _shooter.ProjectileSpeed * _modifiers.ProjectileSpeedMultiplier),
            _health.InvincibilityDuration,
            _character.UltimateIntervalSeconds *
                (fireInterval / Math.Max(0.01f, _shooter.BaseFireInterval)),
            _character.UltimateTargetCapacity,
            _shooter.SpawnDistance);
    }
}
