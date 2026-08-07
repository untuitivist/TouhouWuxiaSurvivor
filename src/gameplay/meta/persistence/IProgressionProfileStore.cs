namespace TouhouWuxiaSurvivor.Gameplay.Meta.Persistence;

/// <summary>
/// 抽象局外档案的加载和原子保存，使运行时使用磁盘而测试可以使用内存。
/// </summary>
public interface IProgressionProfileStore
{
    /// <summary>
    /// 加载并修复一份独立档案，缺失或损坏数据应返回可用默认值。
    /// </summary>
    ProgressionProfileData Load();

    /// <summary>
    /// 尝试完整保存候选档案，只有返回成功后管理器才可替换当前内存状态。
    /// </summary>
    bool TrySave(ProgressionProfileData profile);
}
