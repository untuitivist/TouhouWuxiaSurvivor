using TouhouWuxiaSurvivor.Gameplay.Meta.Persistence;

namespace TouhouWuxiaSurvivor.Tests.Support;

/// <summary>
/// 为局外成长测试提供不接触用户目录的内存档案，并可模拟保存失败。
/// </summary>
public sealed class MemoryProgressionProfileStore : IProgressionProfileStore
{
    private ProgressionProfileData _profile;
    public bool FailSaves { get; set; }
    public int SaveCount { get; private set; }

    /// <summary>
    /// 以给定档案深复制初始化，避免测试修改调用方仍持有的数据对象。
    /// </summary>
    public MemoryProgressionProfileStore(ProgressionProfileData? initial = null)
    {
        _profile = (initial ?? ProgressionProfileData.CreateDefault()).Clone();
        _profile.Repair();
    }

    /// <summary>
    /// 返回当前内存档案的深复制，模拟真实磁盘加载不会共享对象引用。
    /// </summary>
    public ProgressionProfileData Load() => _profile.Clone();

    /// <summary>
    /// 在未启用失败模拟时保存深复制并累计次数，否则保持旧状态并返回失败。
    /// </summary>
    public bool TrySave(ProgressionProfileData profile)
    {
        if (FailSaves)
        {
            return false;
        }

        _profile = profile.Clone();
        SaveCount++;
        return true;
    }
}
