namespace TouhouWuxiaSurvivor.Ecs.Combat;

/// <summary>
/// 标识角色 Boss 当前血量阶段对应的弹幕职责，供系统、渲染和测试读取同一状态。
/// </summary>
public enum BossBulletPhase
{
    /// <summary>高血量阶段，以面向玩家的扇形弹建立基础走位压力。</summary>
    AimedFan,

    /// <summary>中血量阶段，以完整环形波迫使玩家寻找弹幕间隙。</summary>
    Ring,

    /// <summary>低血量阶段，以正反交错旋转弹形成持续弹幕。</summary>
    AlternatingSpiral,
}
