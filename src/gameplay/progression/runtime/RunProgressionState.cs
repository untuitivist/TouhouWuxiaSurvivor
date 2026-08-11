namespace TouhouWuxiaSurvivor.Gameplay.Progression.Runtime;

/// <summary>
/// 管理本局等级、当前灵息、累计灵息和待处理升级次数，不直接依赖任何界面节点。
/// </summary>
public sealed class RunProgressionState
{
    public int Level { get; private set; } = 1;
    public long Experience { get; private set; }
    public long TotalExperience { get; private set; }
    public int PendingChoices { get; private set; }
    public int ExperienceToNext => RunLevelCurve.GetRequiredExperience(Level);

    public event Action? Changed;

    /// <summary>
    /// 接收正数灵息并连续处理可能跨越的多个等级，返回本次新增等级数量。
    /// </summary>
    public int AddExperience(int amount)
    {
        if (amount <= 0)
        {
            return 0;
        }

        Experience += amount;
        TotalExperience += amount;
        int levelsGained = 0;
        while (Experience >= ExperienceToNext)
        {
            Experience -= ExperienceToNext;
            Level++;
            PendingChoices++;
            levelsGained++;
        }

        Changed?.Invoke();
        return levelsGained;
    }

    /// <summary>
    /// 消耗一个已通过选择或自动跳过解决的升级次数，返回是否确实存在待处理项。
    /// </summary>
    public bool ResolveChoice()
    {
        if (PendingChoices <= 0)
        {
            return false;
        }

        PendingChoices--;
        Changed?.Invoke();
        return true;
    }
}
