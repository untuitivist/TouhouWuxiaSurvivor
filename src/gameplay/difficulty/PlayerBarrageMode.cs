namespace TouhouWuxiaSurvivor.Gameplay.Difficulty;

/// <summary>
/// 区分自动武器的方向编排，使单发、交错扇形和旋转环形弹幕无需在射击节点内硬编码阶段。
/// </summary>
public enum PlayerBarrageMode
{
    TargetedSingle,
    AlternatingFan,
    RotatingRing,
}
