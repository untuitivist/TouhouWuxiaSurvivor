namespace TouhouWuxiaSurvivor.Gameplay.Progression.Definitions;

/// <summary>
/// 表示三选一中的一次普通升重或特化选择，并携带仅用于解释候选来源的探索标记。
/// </summary>
public sealed class RunUpgradeChoice
{
    public RunUpgradeDefinition Definition { get; }
    public RunUpgradeSpecialization? Specialization { get; }
    public bool IsExploration { get; }
    public bool IsSpecialization => Specialization is not null;
    public string Id => Specialization?.Id ?? Definition.Id;
    public IReadOnlyList<RunUpgradeAffinity> Affinities =>
        Specialization?.Affinities ?? Definition.Affinities;

    /// <summary>
    /// 建立候选值；特化必须属于传入定义，防止生成器产生无法应用的组合。
    /// </summary>
    public RunUpgradeChoice(
        RunUpgradeDefinition definition,
        RunUpgradeSpecialization? specialization = null,
        bool isExploration = false)
    {
        Definition = definition ?? throw new ArgumentNullException(nameof(definition));
        if (specialization is not null && !definition.Specializations.Contains(specialization))
        {
            throw new ArgumentException("Specialization does not belong to the upgrade.",
                nameof(specialization));
        }

        Specialization = specialization;
        IsExploration = isExploration;
    }

    /// <summary>
    /// 复制当前候选并替换探索标记，生成器可保持其他不可变元数据不变。
    /// </summary>
    public RunUpgradeChoice WithExploration(bool isExploration) =>
        new(Definition, Specialization, isExploration);

}
