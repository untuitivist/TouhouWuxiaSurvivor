namespace TouhouWuxiaSurvivor.Gameplay.SpellCards.Definitions;

/// <summary>
/// 描述一张由内容包声明的自动符卡，并把原作身份、角色归属、解锁规则和战斗参数集中为唯一数据源。
/// </summary>
public sealed class SpellCardDefinition
{
    public string Id { get; }
    public string SourcePackId { get; }
    public string FullName { get; }
    public string ShortName { get; }
    public string OwnerCharacterId { get; }
    public string OwnerName { get; }
    public SpellCardCanonLevel CanonLevel { get; }
    public string SourceNote { get; }
    public string WuxiaStyle { get; }
    public string EffectDescription { get; }
    public SpellCardEffectKind EffectKind { get; }
    public SpellCardGeometryKind GeometryKind { get; }
    public SpellCardActivationKind ActivationKind { get; }
    public string UnlockUpgradeId { get; }
    public string PrerequisiteUpgradeId { get; }
    public int MinimumRank { get; }
    public SpellCardCombatProfile Combat { get; }

    /// <summary>
    /// 组合清单解析后的稳定身份与平衡字段；构造时拒绝空身份并限制前置至少一重。
    /// </summary>
    public SpellCardDefinition(
        string id,
        string sourcePackId,
        string fullName,
        string shortName,
        string ownerCharacterId,
        string ownerName,
        SpellCardCanonLevel canonLevel,
        string sourceNote,
        string wuxiaStyle,
        string effectDescription,
        SpellCardEffectKind effectKind,
        SpellCardGeometryKind geometryKind,
        SpellCardActivationKind activationKind,
        string prerequisiteUpgradeId,
        int minimumRank,
        SpellCardCombatProfile combat)
    {
        Id = Require(id, nameof(id));
        SourcePackId = Require(sourcePackId, nameof(sourcePackId));
        FullName = Require(fullName, nameof(fullName));
        ShortName = Require(shortName, nameof(shortName));
        OwnerCharacterId = Require(ownerCharacterId, nameof(ownerCharacterId));
        OwnerName = Require(ownerName, nameof(ownerName));
        CanonLevel = canonLevel;
        SourceNote = Require(sourceNote, nameof(sourceNote));
        WuxiaStyle = Require(wuxiaStyle, nameof(wuxiaStyle));
        EffectDescription = Require(effectDescription, nameof(effectDescription));
        EffectKind = effectKind;
        GeometryKind = geometryKind;
        ActivationKind = activationKind;
        UnlockUpgradeId = $"spell_{Id}";
        PrerequisiteUpgradeId = Require(prerequisiteUpgradeId, nameof(prerequisiteUpgradeId));
        MinimumRank = Math.Max(1, minimumRank);
        Combat = combat ?? throw new ArgumentNullException(nameof(combat));
    }

    /// <summary>
    /// 校验必需文本并返回原值，使损坏内容在目录加载阶段立即暴露而不是进入战斗后静默回退。
    /// </summary>
    private static string Require(string value, string parameterName) =>
        !string.IsNullOrWhiteSpace(value)
            ? value
            : throw new ArgumentException("Spell card text cannot be empty.", parameterName);
}
