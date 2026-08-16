namespace TouhouWuxiaSurvivor.Gameplay.Progression.Definitions;

/// <summary>
/// 描述达到境界和重数后可选择的一条永久局内分支；只有显式声明的身份才互相排斥。
/// </summary>
public sealed class RunUpgradeSpecialization
{
    public string Id { get; }
    public string DisplayName { get; }
    public string EffectText { get; }
    public int MinimumRunLevel { get; }
    public int RequiredRank { get; }
    public IReadOnlyList<RunUpgradeAffinity> Affinities { get; }
    public IReadOnlySet<string> ExcludedSpecializationIds { get; }
    public RunSpecializationEffect Effect { get; }
    public float EffectValue { get; }

    /// <summary>
    /// 建立经过身份、阈值和标签校验的不可变特化数据，效果数值采用与对应倍率一致的绝对增量。
    /// </summary>
    public RunUpgradeSpecialization(
        string id,
        string displayName,
        string effectText,
        int minimumRunLevel,
        int requiredRank,
        IEnumerable<RunUpgradeAffinity> affinities,
        RunSpecializationEffect effect,
        float effectValue,
        IEnumerable<string>? excludedSpecializationIds = null)
    {
        Id = Require(id, nameof(id));
        DisplayName = Require(displayName, nameof(displayName));
        EffectText = Require(effectText, nameof(effectText));
        MinimumRunLevel = Math.Max(1, minimumRunLevel);
        RequiredRank = Math.Max(1, requiredRank);
        Affinities = affinities.Distinct().ToArray();
        Effect = effect;
        EffectValue = Math.Max(0.0f, effectValue);
        ExcludedSpecializationIds = new HashSet<string>(
            excludedSpecializationIds ?? [], StringComparer.Ordinal);
    }

    /// <summary>
    /// 拒绝空白稳定 ID 和显示文本，使损坏分支在目录构建阶段立即暴露。
    /// </summary>
    private static string Require(string value, string parameterName) =>
        !string.IsNullOrWhiteSpace(value)
            ? value
            : throw new ArgumentException("Specialization text cannot be empty.", parameterName);
}
