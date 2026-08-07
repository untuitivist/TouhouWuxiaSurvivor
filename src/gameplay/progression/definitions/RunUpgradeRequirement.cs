namespace TouhouWuxiaSurvivor.Gameplay.Progression.Definitions;

/// <summary>
/// 描述一项局内升级所需的另一项最低重数，用于武学进阶和符卡悟得条件。
/// </summary>
public sealed class RunUpgradeRequirement
{
    public RunUpgradeKind RequiredKind { get; }
    public int MinimumRank { get; }

    /// <summary>
    /// 建立单项前置修炼要求，并将最低重数限制为至少一重。
    /// </summary>
    public RunUpgradeRequirement(RunUpgradeKind requiredKind, int minimumRank)
    {
        RequiredKind = requiredKind;
        MinimumRank = Math.Max(1, minimumRank);
    }
}
