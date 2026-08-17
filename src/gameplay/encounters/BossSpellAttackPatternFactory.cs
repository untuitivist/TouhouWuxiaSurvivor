using TouhouWuxiaSurvivor.Content.Characters;
using TouhouWuxiaSurvivor.Ecs.Combat.Bosses;
using TouhouWuxiaSurvivor.Gameplay.SpellCards.Contracts;
using TouhouWuxiaSurvivor.Gameplay.SpellCards.Definitions;
using TouhouWuxiaSurvivor.Gameplay.SpellCards.Effects;
using TouhouWuxiaSurvivor.Gameplay.SpellCards.Scaling;

namespace TouhouWuxiaSurvivor.Gameplay.Encounters;

/// <summary>
/// 将任意符卡定义转换为纯 ECS Boss 攻击档案，使阶段选择、测试与未来序列编排共享同一入口。
/// </summary>
public static class BossSpellAttackPatternFactory
{
    /// <summary>使用符卡所属角色的基础属性换算数值，并保留原作时序与复合弹型。</summary>
    public static BossAttackPattern Create(SpellCardDefinition card)
    {
        ArgumentNullException.ThrowIfNull(card);
        CharacterDefinition character = CharacterCatalog.GetRequired(card.OwnerCharacterId);
        SpellCardBaseAttributes attributes = BossSpellCardAttributeFactory.Create(character);
        ResolvedSpellCardCombat combat = SpellCardScalingResolver.Resolve(
            card.Combat, attributes);
        return new BossAttackPattern(
            card.Id,
            card.ShortName,
            MapPattern(card.Pattern.Kind, card.GeometryKind),
            combat.IntervalSeconds,
            combat.ProjectileSpeed,
            combat.Damage,
            combat.TargetCount,
            Math.Clamp(combat.EffectRange / 18.0f, 6.0f, 30.0f),
            combat.SpawnDistance,
            SpellCardVisualBindingCatalog.GetBindingId(card.Id),
            new[] { card.BulletStyleKind }
                .Concat(card.Pattern.AccentBulletStyles).Select(style => (int)style).ToArray(),
            card.Pattern.WaveCount,
            card.Pattern.PhaseRatio,
            card.Pattern.HoldRatio,
            card.Pattern.TurnRateScale);
    }

    /// <summary>优先映射原作时序语法；未迁移库存才回退为旧空间几何。</summary>
    private static BossProjectilePatternKind MapPattern(
        SpellCardPatternKind pattern,
        SpellCardGeometryKind geometry) => pattern switch
        {
            SpellCardPatternKind.LegacyGeometry => MapGeometry(geometry),
            SpellCardPatternKind.HomingOrbit => BossProjectilePatternKind.HomingOrbit,
            SpellCardPatternKind.SealPulse => BossProjectilePatternKind.SealPulse,
            SpellCardPatternKind.StraightBeam => BossProjectilePatternKind.StraightBeam,
            SpellCardPatternKind.StardustFan => BossProjectilePatternKind.StardustFan,
            SpellCardPatternKind.AimedArc => BossProjectilePatternKind.AimedArc,
            SpellCardPatternKind.FreezeRelease => BossProjectilePatternKind.FreezeRelease,
            SpellCardPatternKind.RotatingStream => BossProjectilePatternKind.RotatingStream,
            SpellCardPatternKind.ElementalCycle => BossProjectilePatternKind.ElementalCycle,
            SpellCardPatternKind.TimeStopRedirect => BossProjectilePatternKind.TimeStopRedirect,
            SpellCardPatternKind.AimedTrail => BossProjectilePatternKind.AimedTrail,
            SpellCardPatternKind.SweepingBeam => BossProjectilePatternKind.SweepingBeam,
            _ => throw new ArgumentOutOfRangeException(nameof(pattern), pattern, null),
        };

    /// <summary>将旧内容的空间几何转换为独立 ECS 语法，避免工厂向高频系统泄漏内容类型。</summary>
    private static BossProjectilePatternKind MapGeometry(SpellCardGeometryKind geometry) =>
        geometry switch
        {
            SpellCardGeometryKind.Orbit => BossProjectilePatternKind.Orbit,
            SpellCardGeometryKind.Fan => BossProjectilePatternKind.Fan,
            SpellCardGeometryKind.Line => BossProjectilePatternKind.Line,
            SpellCardGeometryKind.Ring => BossProjectilePatternKind.Ring,
            SpellCardGeometryKind.Backstab => BossProjectilePatternKind.Backstab,
            _ => throw new ArgumentOutOfRangeException(nameof(geometry), geometry, null),
        };
}
