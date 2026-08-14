using TouhouWuxiaSurvivor.Gameplay.SpellCards.Definitions;

namespace TouhouWuxiaSurvivor.Gameplay.Progression.Definitions;

/// <summary>
/// 按符卡的通用战斗原型推导亲和，不读取作品或角色身份，保证新增内容包沿用同一横向规则。
/// </summary>
public static class SpellCardAffinityResolver
{
    /// <summary>
    /// 将四类效果原型映射为两项通用标签，使全部定时奥义仍能自然参与横向构筑倾向。
    /// </summary>
    public static IReadOnlyList<RunUpgradeAffinity> Resolve(SpellCardDefinition card) =>
        card.EffectKind switch
        {
            SpellCardEffectKind.HomingVolley =>
                [RunUpgradeAffinity.Precision, RunUpgradeAffinity.Formation],
            SpellCardEffectKind.FocusedVolley =>
                [RunUpgradeAffinity.Force, RunUpgradeAffinity.Precision],
            SpellCardEffectKind.AreaBurst =>
                [RunUpgradeAffinity.Formation, RunUpgradeAffinity.Force],
            SpellCardEffectKind.GuardField =>
                [RunUpgradeAffinity.Guard, RunUpgradeAffinity.Formation],
            _ => [RunUpgradeAffinity.Precision],
        };

    /// <summary>
}
