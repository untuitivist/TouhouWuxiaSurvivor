namespace TouhouWuxiaSurvivor.Ecs.Combat.Bosses;

/// <summary>
/// 保存一次角色 Boss 符卡演出所需的纯战斗数据，不引用内容清单、纹理或场景节点。
/// </summary>
public sealed class BossAttackPattern
{
    public string SpellCardId { get; }
    public string DisplayName { get; }
    public BossProjectilePatternKind PatternKind { get; }
    public float FireInterval { get; }
    public float ProjectileSpeed { get; }
    public int Damage { get; }
    public int ShotCount { get; }
    public float SpreadDegrees { get; }
    public float SpawnDistance { get; }
    public int VisualStyleId { get; }

    /// <summary>
    /// 建立经过边界整理的攻击档案，使损坏内容无法向高频弹幕循环传播零间隔或无效数值。
    /// </summary>
    public BossAttackPattern(
        string spellCardId,
        string displayName,
        BossProjectilePatternKind patternKind,
        float fireInterval,
        float projectileSpeed,
        int damage,
        int shotCount,
        float spreadDegrees,
        float spawnDistance,
        int visualStyleId)
    {
        SpellCardId = Require(spellCardId, nameof(spellCardId));
        DisplayName = Require(displayName, nameof(displayName));
        PatternKind = patternKind;
        FireInterval = Math.Max(0.1f, fireInterval);
        ProjectileSpeed = Math.Max(1.0f, projectileSpeed);
        Damage = Math.Max(1, damage);
        ShotCount = Math.Max(1, shotCount);
        SpreadDegrees = Math.Max(0.0f, spreadDegrees);
        SpawnDistance = Math.Max(1.0f, spawnDistance);
        VisualStyleId = Math.Max(0, visualStyleId);
    }

    /// <summary>拒绝空稳定身份或演出名，使 Boss 进入战斗前就暴露错误清单。</summary>
    private static string Require(string value, string parameterName) =>
        !string.IsNullOrWhiteSpace(value)
            ? value
            : throw new ArgumentException("Boss attack identity cannot be empty.", parameterName);
}
