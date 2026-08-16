namespace TouhouWuxiaSurvivor.Gameplay.Difficulty;

/// <summary>
/// 区分自动武器的方向编排，使预判单发、定向扇面和两翼收束无需写进射击节点。
/// </summary>
public enum PlayerBarrageMode
{
    TargetedSingle,
    AlternatingFan,
    ConvergingFormation,
}
