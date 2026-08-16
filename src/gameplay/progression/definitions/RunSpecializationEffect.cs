namespace TouhouWuxiaSurvivor.Gameplay.Progression.Definitions;

/// <summary>
/// 标识基础修行特化对现有运行倍率的等预算修正，使分支无需新增主动操作或专属战斗脚本。
/// </summary>
public enum RunSpecializationEffect
{
    Damage,
    FireRate,
    MoveSpeed,
    TargetRange,
    ProjectileSpeed,
    SpiritAttraction,
    BarrageProjectiles,
    ProjectilePierce,
    ConvergingBarrage,
    SpiritYield,
    ContinuousFireMomentum,
    StationaryFocus,
    MovementMomentum,
    SpiritFlowMomentum,
}
