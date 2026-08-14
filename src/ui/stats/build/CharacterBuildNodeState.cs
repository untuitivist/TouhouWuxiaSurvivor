namespace TouhouWuxiaSurvivor.Ui.Stats.Build;

/// <summary>
/// 标识构筑节点相对当前运行状态的可交互阶段，界面不得根据文案反推锁定逻辑。
/// </summary>
public enum CharacterBuildNodeState
{
    Available,
    Learned,
    Mastered,
    LockedPrerequisite,
    LockedExclusion,
    LockedContent,
    LockedLevel,
}
