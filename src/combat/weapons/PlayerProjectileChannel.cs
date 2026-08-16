namespace TouhouWuxiaSurvivor.Combat.Weapons;

/// <summary>
/// 区分预判自瞄弹与定向弹幕；两者共享基础数值，只分别成长数量、阵形和视觉。
/// </summary>
public enum PlayerProjectileChannel
{
    PredictiveAim = 0,
    Barrage = 1,
}
