using TouhouWuxiaSurvivor.Gameplay.Progression.Definitions;

namespace TouhouWuxiaSurvivor.Ui.Stats.Build;

/// <summary>
/// 保存一种构筑亲和的当前点数、占比和稳定顺序，供雷达图、条带或筛选控件共用。
/// </summary>
public sealed class CharacterBuildAffinityView
{
    public RunUpgradeAffinity Affinity { get; }
    public string DisplayName { get; }
    public int Value { get; }
    public float Share { get; }
    public bool IsDominant { get; }
    public int SortOrder { get; }

    /// <summary>
    /// 建立单项不可变亲和投影，并把占比夹在合法区间以保护绘图控件。
    /// </summary>
    public CharacterBuildAffinityView(
        RunUpgradeAffinity affinity,
        string displayName,
        int value,
        float share,
        bool isDominant,
        int sortOrder)
    {
        Affinity = affinity;
        DisplayName = displayName;
        Value = Math.Max(0, value);
        Share = Math.Clamp(share, 0.0f, 1.0f);
        IsDominant = isDominant;
        SortOrder = sortOrder;
    }
}
