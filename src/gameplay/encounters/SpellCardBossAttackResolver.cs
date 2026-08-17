using TouhouWuxiaSurvivor.Content;
using TouhouWuxiaSurvivor.Content.Characters;
using TouhouWuxiaSurvivor.Ecs.Combat;
using TouhouWuxiaSurvivor.Ecs.Combat.Bosses;
using TouhouWuxiaSurvivor.Gameplay.SpellCards.Definitions;

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
                group => group.Select(BossSpellAttackPatternFactory.Create).ToArray(),
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

}
