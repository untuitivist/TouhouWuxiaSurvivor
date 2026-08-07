namespace TouhouWuxiaSurvivor.Gameplay.Session;

/// <summary>
/// 区分导致本局失败的终局原因，使结算规则保持统一而界面仍能准确说明玩家为何离场。
/// </summary>
public enum RunEndReason
{
    /// <summary>
    /// 玩家生命归零或符力耗尽，被游戏规则判定为战败。
    /// </summary>
    Defeated,

    /// <summary>
    /// 玩家从暂停菜单主动结束尚未完成的探索，按失败进行结算。
    /// </summary>
    Abandoned,
}
