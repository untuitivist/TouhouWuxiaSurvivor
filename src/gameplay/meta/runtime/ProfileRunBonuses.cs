using TouhouWuxiaSurvivor.Gameplay.Meta.Definitions;
using TouhouWuxiaSurvivor.Gameplay.Meta.Persistence;

namespace TouhouWuxiaSurvivor.Gameplay.Meta.Runtime;

/// <summary>
/// 把持久档案投影为本局初始化所需的四项只读基础加成。
/// </summary>
public sealed class ProfileRunBonuses
{
    public int MaxHealthBonus { get; }
    public int DamageBonus { get; }
    public float MoveSpeedMultiplier { get; }
    public float SpiritAttractionMultiplier { get; }

    /// <summary>
    /// 从经过修复的档案读取各修行重数，并按策划步长生成稳定局内倍率。
    /// </summary>
    public ProfileRunBonuses(ProgressionProfileData profile)
    {
        MaxHealthBonus = GetRank(profile, CultivationKind.MaxHealth);
        DamageBonus = GetRank(profile, CultivationKind.Damage);
        MoveSpeedMultiplier = 1.0f + GetRank(profile, CultivationKind.MoveSpeed) * 0.02f;
        SpiritAttractionMultiplier = 1.0f +
            GetRank(profile, CultivationKind.SpiritAttraction) * 0.08f;
    }

    /// <summary>
    /// 按效果类型定位唯一修行定义并读取重数，目录缺失时安全返回零。
    /// </summary>
    private static int GetRank(ProgressionProfileData profile, CultivationKind kind)
    {
        CultivationDefinition? definition = CultivationCatalog.All.FirstOrDefault(
            candidate => candidate.Kind == kind);
        return definition is null ? 0 : profile.GetRank(definition.Id);
    }
}
