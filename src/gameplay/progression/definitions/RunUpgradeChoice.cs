namespace TouhouWuxiaSurvivor.Gameplay.Progression.Definitions;

/// <summary>
/// 表示三选一中的一次普通升重或特化选择，并携带仅用于解释候选来源的探索标记。
/// </summary>
public sealed class RunUpgradeChoice
{
    public RunUpgradeDefinition Definition { get; }
    public RunUpgradeSpecialization? Specialization { get; }
    public RunUpgradeOfferRole OfferRole { get; }
    public bool IsExploration => OfferRole == RunUpgradeOfferRole.Exploration;
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
        : this(
            definition,
            specialization,
            isExploration
                ? RunUpgradeOfferRole.Exploration
                : RunUpgradeOfferRole.Opportunity)
    {
    }

    /// <summary>
    /// 建立带明确构筑职责的候选；仅由生成器复制已有合法候选时调用。
    /// </summary>
    private RunUpgradeChoice(
        RunUpgradeDefinition definition,
        RunUpgradeSpecialization? specialization,
        RunUpgradeOfferRole offerRole)
    {
        Definition = definition ?? throw new ArgumentNullException(nameof(definition));
        if (specialization is not null && !definition.Specializations.Contains(specialization))
        {
            throw new ArgumentException("Specialization does not belong to the upgrade.",
                nameof(specialization));
        }

        Specialization = specialization;
        OfferRole = offerRole;
    }

    /// <summary>
    /// 复制当前候选并替换探索标记，生成器可保持其他不可变元数据不变。
    /// </summary>
    public RunUpgradeChoice WithExploration(bool isExploration) =>
        WithRole(isExploration
            ? RunUpgradeOfferRole.Exploration
            : RunUpgradeOfferRole.Opportunity);

    /// <summary>
    /// 复制当前候选并附加本轮展示职责，定义、特化与亲和信息保持不变。
    /// </summary>
    public RunUpgradeChoice WithRole(RunUpgradeOfferRole offerRole) =>
        new(Definition, Specialization, offerRole);

}
