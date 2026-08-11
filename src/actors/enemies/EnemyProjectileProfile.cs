namespace TouhouWuxiaSurvivor.Actors.Enemies;

/// <summary>
/// 保存普通敌人的基础射击节奏与扇形参数；Boss 会在此基础上由阶段系统生成更复杂弹幕。
/// </summary>
public sealed class EnemyProjectileProfile
{
    public static EnemyProjectileProfile None { get; } = new(0.0f, 0.0f, 0, 0, 0.0f);
    public static EnemyProjectileProfile Aimed { get; } = new(2.1f, 88.0f, 1, 1, 0.0f);
    public static EnemyProjectileProfile Fan { get; } = new(2.6f, 82.0f, 1, 3, 18.0f);
    public static EnemyProjectileProfile Boss { get; } = new(1.15f, 96.0f, 2, 5, 12.0f);

    public float FireInterval { get; }
    public float ProjectileSpeed { get; }
    public int Damage { get; }
    public int ShotCount { get; }
    public float SpreadDegrees { get; }
    public bool Enabled => FireInterval > 0.0f && ProjectileSpeed > 0.0f && Damage > 0 && ShotCount > 0;

    /// <summary>
    /// 构造不可变射击档案；零间隔代表完全禁用，其余数值被限制到可计算范围。
    /// </summary>
    public EnemyProjectileProfile(
        float fireInterval,
        float projectileSpeed,
        int damage,
        int shotCount,
        float spreadDegrees)
    {
        FireInterval = Math.Max(0.0f, fireInterval);
        ProjectileSpeed = Math.Max(0.0f, projectileSpeed);
        Damage = Math.Max(0, damage);
        ShotCount = Math.Max(0, shotCount);
        SpreadDegrees = Math.Max(0.0f, spreadDegrees);
    }
}
