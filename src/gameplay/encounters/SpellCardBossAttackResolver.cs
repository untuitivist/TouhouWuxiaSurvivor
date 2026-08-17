using TouhouWuxiaSurvivor.Content;
using TouhouWuxiaSurvivor.Content.Characters;
using TouhouWuxiaSurvivor.Ecs.Combat;
using TouhouWuxiaSurvivor.Ecs.Combat.Bosses;
using TouhouWuxiaSurvivor.Gameplay.SpellCards.Contracts;
using TouhouWuxiaSurvivor.Gameplay.SpellCards.Definitions;
using TouhouWuxiaSurvivor.Gameplay.SpellCards.Effects;
using TouhouWuxiaSurvivor.Gameplay.SpellCards.Scaling;

namespace TouhouWuxiaSurvivor.Gameplay.Encounters;

/// <summary>
/// 将声明 Boss 符卡能力的内容包解析为攻击；其余作品保持通用弹幕直至完成迁移验收。
/// </summary>
public sealed class SpellCardBossAttackResolver : IBossAttackResolver
{
    private static readonly HashSet<string> SupportedSources = ContentPackCatalog.Installed
        .Where(pack => pack.HasCapability(ContentPackCapabilityIds.BossSpellSequences))
        .Select(pack => pack.Id)
        .ToHashSet(StringComparer.Ordinal);
    private readonly IReadOnlyDictionary<string, BossAttackPattern[]> _attacks;

    /// <summary>在局开始时一次性解析全部受支持角色，避免每波弹幕重复扫描内容目录和换算倍率。</summary>
    public SpellCardBossAttackResolver(RunContentContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        HashSet<string> activeSources = context.ActiveContentPacks
            .Select(pack => pack.Id).ToHashSet(StringComparer.Ordinal);
        _attacks = SpellCardCatalog.All
            .Where(card => SupportedSources.Contains(card.SourcePackId) &&
                activeSources.Contains(card.SourcePackId))
            .GroupBy(card => card.OwnerCharacterId, StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => group.Select(CreatePattern).ToArray(),
                StringComparer.Ordinal);
    }

    /// <summary>按高、中、低血量阶段轮换角色自己的符卡；无归属数据时显式返回 false。</summary>
    public bool TryResolve(
        string characterId,
        BossBulletPhase phase,
        out BossAttackPattern pattern)
    {
        pattern = null!;
        if (!_attacks.TryGetValue(characterId, out BossAttackPattern[]? attacks) ||
            attacks.Length == 0)
        {
            return false;
        }

        int phaseIndex = phase switch
        {
            BossBulletPhase.AimedFan => 0,
            BossBulletPhase.Ring => 1,
            BossBulletPhase.AlternatingSpiral => 2,
            _ => 0,
        };
        pattern = attacks[phaseIndex % attacks.Length];
        return true;
    }

    /// <summary>用角色基础属性解析一张符卡，并把内容几何映射成 ECS 可执行的纯数据攻击档案。</summary>
    private static BossAttackPattern CreatePattern(SpellCardDefinition card)
    {
        CharacterDefinition character = CharacterCatalog.GetRequired(card.OwnerCharacterId);
        SpellCardBaseAttributes attributes = BossSpellCardAttributeFactory.Create(character);
        ResolvedSpellCardCombat combat = SpellCardScalingResolver.Resolve(
            card.Combat, attributes);
        return new BossAttackPattern(
            card.Id,
            card.ShortName,
            MapGeometry(card.GeometryKind),
            combat.IntervalSeconds,
            combat.ProjectileSpeed,
            combat.Damage,
            combat.TargetCount,
            Math.Clamp(combat.EffectRange / 18.0f, 6.0f, 30.0f),
            combat.SpawnDistance,
            SpellCardVisualBindingCatalog.GetBindingId(card.Id));
    }

    /// <summary>将内容层几何转换为独立 ECS 语法，避免高频系统依赖符卡清单枚举。</summary>
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
