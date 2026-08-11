namespace TouhouWuxiaSurvivor.Actors.Enemies;

/// <summary>
/// 标识纯数据敌人采用的移动决策类型，运行时系统据此批量执行而不创建独立节点。
/// </summary>
public enum EnemyAiKind
{
    /// <summary>持续缩短与玩家距离，适合数量多、职责简单的基础敌人。</summary>
    Chase,

    /// <summary>保持偏好距离并横向绕行，同时由弹幕档案负责定时射击。</summary>
    OrbitShooter,

    /// <summary>在追踪蓄势与高速突进之间循环，迫使玩家改变走位方向。</summary>
    Charger,

    /// <summary>角色 Boss 专用移动，围绕玩家游走并由血量阶段切换弹幕。</summary>
    BossPhased,
}
