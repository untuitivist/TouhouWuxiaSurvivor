namespace TouhouWuxiaSurvivor.Gameplay.SpellCards.Definitions;

using TouhouWuxiaSurvivor.Gameplay.Progression.Definitions;

/// <summary>
/// 描述一张具有原作身份和武侠化战斗定位的符卡，不承载任何场景节点状态。
/// </summary>
public sealed class SpellCardDefinition
{
    public string Id { get; }
    public string FullName { get; }
    public string ShortName { get; }
    public string OwnerName { get; }
    public string SourceWork { get; }
    public string WuxiaStyle { get; }
    public string EffectDescription { get; }
    public SpellCardEffectKind EffectKind { get; }
    public RunUpgradeKind UnlockKind { get; }
    public SpellCardCombatProfile Combat { get; }

    /// <summary>
    /// 组合符卡原作元数据、武侠表达和独立战斗参数，供运行时、图鉴与界面共用。
    /// </summary>
    public SpellCardDefinition(
        string id,
        string fullName,
        string shortName,
        string ownerName,
        string sourceWork,
        string wuxiaStyle,
        string effectDescription,
        SpellCardEffectKind effectKind,
        RunUpgradeKind unlockKind,
        SpellCardCombatProfile combat)
    {
        Id = id;
        FullName = fullName;
        ShortName = shortName;
        OwnerName = ownerName;
        SourceWork = sourceWork;
        WuxiaStyle = wuxiaStyle;
        EffectDescription = effectDescription;
        EffectKind = effectKind;
        UnlockKind = unlockKind;
        Combat = combat;
    }
}
