using TouhouWuxiaSurvivor.Gameplay.Progression.Definitions;
using TouhouWuxiaSurvivor.Gameplay.Progression.Runtime;
using TouhouWuxiaSurvivor.Gameplay.SpellCards.Definitions;
using TouhouWuxiaSurvivor.Ui.SpellCards;
using TouhouWuxiaSurvivor.Ui.Stats.Build;

namespace TouhouWuxiaSurvivor.Ui.Progression;

/// <summary>
/// 把候选及当前构筑转换为紧凑卡片字段，统一升级层和测试使用的显示语义。
/// </summary>
public static class RunUpgradeChoicePresentationFactory
{
    /// <summary>
    /// 根据普通升重、特化或奥义建立标题、效果、下一重与轨迹数据。
    /// </summary>
    public static RunUpgradeChoicePresentation Create(
        RunUpgradeChoice choice,
        RunBuildState build)
    {
        ArgumentNullException.ThrowIfNull(choice);
        ArgumentNullException.ThrowIfNull(build);
        RunUpgradeDefinition definition = choice.Definition;
        int currentRank = build.GetRank(definition.Id);
        string role = FormatRole(choice.OfferRole);
        string affinity = RunUpgradeAffinityFormatter.FormatMany(choice.Affinities);
        if (choice.Specialization is not null)
        {
            return new(role, "特化", choice.Specialization.DisplayName,
                choice.Specialization.EffectText, affinity, "定型",
                $"{definition.DisplayName} · {choice.Specialization.EffectText}", 0, 1, 1);
        }

        if (definition.SpellCardId is not null)
        {
            return CreateSpellCard(choice, role, affinity);
        }

        int previewRank = checked(currentRank + 1);
        string rank = definition.IsRepeatable
            ? $"第 {previewRank} 重"
            : $"{previewRank}/{definition.MaxRank}";
        int trackLength = definition.IsRepeatable ? 0 : definition.MaxRank;
        return new(role, definition.GetCategoryName(), definition.DisplayName,
            definition.EffectText, affinity, rank,
            $"{definition.DisplayName} · {rank}\n{definition.EffectText}",
            currentRank, previewRank, trackLength);
    }

    /// <summary>
    /// 将当前最高亲和投影为升级层副标题；尚未选择时不伪造默认流派。
    /// </summary>
    public static string FormatCurrentRoute(RunBuildState build)
    {
        ArgumentNullException.ThrowIfNull(build);
        int maximum = Enum.GetValues<RunUpgradeAffinity>().Max(build.GetAffinity);
        if (maximum <= 0)
        {
            return "尚未定势";
        }

        RunUpgradeAffinity[] dominant = Enum.GetValues<RunUpgradeAffinity>()
            .Where(affinity => build.GetAffinity(affinity) == maximum).ToArray();
        return $"当前主脉 · {RunUpgradeAffinityFormatter.FormatMany(dominant)}";
    }

    /// <summary>
    /// 为奥义候选保留短名、触发、几何与槽位，完整原作说明仍由图鉴和 E 页承担。
    /// </summary>
    private static RunUpgradeChoicePresentation CreateSpellCard(
        RunUpgradeChoice choice,
        string role,
        string affinity)
    {
        SpellCardDefinition card = SpellCardCatalog.FindById(
            choice.Definition.SpellCardId!) ?? throw new InvalidOperationException(
            $"Unknown spell card: {choice.Definition.SpellCardId}.");
        string slot = SpellCardSlotPolicy.Classify(card) == SpellCardSlotKind.Offensive
            ? "主攻"
            : "护持";
        string effect = $"{SpellCardActivationText.GetShortName(card.ActivationKind)} · " +
            $"{SpellCardGeometryText.GetName(card.GeometryKind)} · {slot}";
        return new(role, "奥义", card.ShortName, effect, affinity, "悟得",
            $"{card.FullName}\n{card.EffectDescription}", 0, 1, 1);
    }

    /// <summary>
    /// 返回候选职责的两字短名；职责只解释本轮构成，不代表品质或强弱。
    /// </summary>
    private static string FormatRole(RunUpgradeOfferRole role) => role switch
    {
        RunUpgradeOfferRole.Momentum => "精进",
        RunUpgradeOfferRole.Complement => "成势",
        RunUpgradeOfferRole.Exploration => "补缺",
        _ => "机缘",
    };
}
