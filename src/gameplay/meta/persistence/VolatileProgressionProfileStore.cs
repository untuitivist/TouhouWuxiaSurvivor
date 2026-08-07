namespace TouhouWuxiaSurvivor.Gameplay.Meta.Persistence;

/// <summary>
/// 在进程内保存档案副本，供不应写入用户目录的演示场景和集成测试使用。
/// </summary>
public sealed class VolatileProgressionProfileStore : IProgressionProfileStore
{
    private ProgressionProfileData _profile;

    /// <summary>
    /// 使用默认档或给定档案的深复制初始化，避免与调用方共享可变集合。
    /// </summary>
    public VolatileProgressionProfileStore(ProgressionProfileData? initial = null)
    {
        _profile = (initial ?? ProgressionProfileData.CreateDefault()).Clone();
        _profile.Repair();
    }

    /// <summary>
    /// 返回当前内存档案的深复制，保持与磁盘存储相同的对象隔离语义。
    /// </summary>
    public ProgressionProfileData Load() => _profile.Clone();

    /// <summary>
    /// 用候选档案的深复制替换内存状态，并始终返回成功。
    /// </summary>
    public bool TrySave(ProgressionProfileData profile)
    {
        _profile = profile.Clone();
        return true;
    }
}
