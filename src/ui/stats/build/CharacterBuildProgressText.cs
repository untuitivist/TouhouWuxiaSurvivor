using TouhouWuxiaSurvivor.Gameplay.Progression.Definitions;

namespace TouhouWuxiaSurvivor.Ui.Stats.Build;

/// <summary>
/// 将升级当前重与下一重投影为两段明确说明，使界面不再让玩家猜测效果文本描述哪一重。
/// </summary>
public static class CharacterBuildProgressText
{
    /// <summary>返回普通升级当前已经生效的状态；尚未取得时明确标为未生效。</summary>
    public static string FormatCurrent(RunUpgradeDefinition definition, int rank)
    {
        if (definition.Category == RunUpgradeCategory.SpellCard)
        {
            return rank > 0
                ? $"当前：已悟得；{definition.EffectText}"
                : "当前：尚未悟得";
        }

        if (rank <= 0)
        {
            return "当前：尚未修习";
        }

        string rankText = definition.IsRepeatable
            ? $"第 {rank} 重"
            : $"{rank}/{definition.MaxRank} 重";
        return $"当前：{rankText}生效；每重{definition.EffectText}";
    }

    /// <summary>返回下一次选择会得到的效果；满重、一次性奥义和无尽路线采用不同终点语义。</summary>
    public static string FormatNext(RunUpgradeDefinition definition, int rank)
    {
        if (definition.Category == RunUpgradeCategory.SpellCard)
        {
            return rank > 0
                ? "后续：此奥义已完成悟得"
                : $"悟得后：{definition.EffectText}";
        }

        if (!definition.IsRepeatable && rank >= definition.MaxRank)
        {
            return "后续：基础圆满，可转修对应无尽心法";
        }

        string rankText = definition.IsRepeatable
            ? $"第 {rank + 1} 重"
            : $"{rank + 1}/{definition.MaxRank} 重";
        return $"下一重：{rankText}；{definition.EffectText}";
    }

    /// <summary>返回特化当前是否定型，并直接展示所选分支改变的属性或弹幕行为。</summary>
    public static string FormatSpecializationCurrent(
        RunUpgradeSpecialization specialization,
        bool selected) => selected
        ? $"当前：已定型；{specialization.EffectText}"
        : "当前：尚未选择此分支";

    /// <summary>返回选择特化后的永久局内行为，已定型分支明确标记不会继续升重。</summary>
    public static string FormatSpecializationNext(
        RunUpgradeSpecialization specialization,
        bool selected) => selected
        ? "后续：特化不升重，本局持续生效"
        : $"选择后：{specialization.EffectText}";
}
