namespace TouhouWuxiaSurvivor.Gameplay.Difficulty;

/// <summary>
/// 区分自动武器的方向编排，使单发、交错扇形和目标收束阵无需在射击节点内硬编码阶段。
/// </summary>
public enum PlayerBarrageMode
{
    TargetedSingle,
    AlternatingFan,
    ConvergingOrbit,
}
