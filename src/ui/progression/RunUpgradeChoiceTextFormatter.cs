using TouhouWuxiaSurvivor.Gameplay.Progression.Definitions;
using TouhouWuxiaSurvivor.Gameplay.Progression.Runtime;
using TouhouWuxiaSurvivor.Gameplay.SpellCards.Definitions;
using TouhouWuxiaSurvivor.Ui.SpellCards;
using TouhouWuxiaSurvivor.Ui.Stats.Build;

namespace TouhouWuxiaSurvivor.Ui.Progression;

/// <summary>
/// 把局内候选投影为固定两行的紧凑文本，使数据层保留完整说明而升级界面不被长奥义文案撑宽。
/// </summary>
public static class RunUpgradeChoiceTextFormatter
{
    /// <summary>
    /// 根据候选类型选择普通升重、特化或奥义布局；探索标记和亲和始终保留在首行。
    /// </summary>
    public static string Format(RunUpgradeChoice choice, RunBuildState build)
    {
        ArgumentNullException.ThrowIfNull(choice);
        ArgumentNullException.ThrowIfNull(build);
        string affinity = RunUpgradeAffinityFormatter.FormatMany(choice.Affinities);
        string route = FormatRole(choice.OfferRole);
        if (choice.Specialization is not null)
        {
            return $"{route}特化 · {choice.Specialization.DisplayName}    [{affinity}]\n" +
                choice.Specialization.EffectText;
        }

        if (choice.Definition.SpellCardId is not null)
        {
            return FormatSpellCard(choice.Definition, route, affinity,
                build.GetRank(choice.Definition.Id));
        }

        return $"{route}{choice.Definition.FormatChoice(build.GetRank(choice.Definition.Id))}" +
            $"    [{affinity}]";
    }

    /// <summary>
    /// 奥义只在三选一显示短名、触发、几何和槽位；完整效果继续由图鉴与 E 构筑详情承担。
    /// </summary>
    private static string FormatSpellCard(
        RunUpgradeDefinition definition,
        string route,
        string affinity,
        int currentRank)
    {
        SpellCardDefinition card = SpellCardCatalog.FindById(definition.SpellCardId!) ??
            throw new InvalidOperationException($"Unknown spell card: {definition.SpellCardId}.");
        string slot = SpellCardSlotPolicy.Classify(card) == SpellCardSlotKind.Offensive
            ? "主攻"
            : "护持";
        string rankText = currentRank <= 0 ? "1/2 · 悟得" : "2/2 · 化境";
        return $"{route}奥义 · {card.ShortName}    {rankText}    [{affinity}]\n" +
            $"{SpellCardActivationText.GetShortName(card.ActivationKind)} · " +
            $"{SpellCardGeometryText.GetName(card.GeometryKind)} · {slot}";
    }

    /// <summary>
    /// 将本轮候选职责同步到纯文本表示，供无障碍提示和兼容测试保留完整语义。
    /// </summary>
    private static string FormatRole(RunUpgradeOfferRole role) => role switch
    {
        RunUpgradeOfferRole.Momentum => "精进 · ",
        RunUpgradeOfferRole.Complement => "成势 · ",
        RunUpgradeOfferRole.Exploration => "补缺 · ",
        _ => string.Empty,
    };
}
