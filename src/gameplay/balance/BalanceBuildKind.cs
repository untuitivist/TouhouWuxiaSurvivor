namespace TouhouWuxiaSurvivor.Gameplay.Balance;

/// <summary>
/// 标识确定性策划模拟使用的四条合法构筑路线；路线只改变选择顺序，不获得目录外加成。
/// </summary>
public enum BalanceBuildKind
{
    Baseline,
    Assault,
    Rapid,
    Utility,
}
