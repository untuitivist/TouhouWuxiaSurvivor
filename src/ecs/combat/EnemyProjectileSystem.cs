using Godot;
using TouhouWuxiaSurvivor.Actors.Enemies;
using TouhouWuxiaSurvivor.Ecs.Combat.Projectiles;

namespace TouhouWuxiaSurvivor.Ecs.Combat;

/// <summary>
/// 批量推进普通敌人射击与角色 Boss 三阶段弹幕，发射结果通过委托写入共享投射物池。
/// </summary>
public sealed class EnemyProjectileSystem
{
    public const int MaximumShotsPerVolley = 96;
    /// <summary>
    /// 为存活敌人递减射击冷却，并按普通档案或 Boss 血量阶段生成带无尽强度缩放的敌弹。
    /// </summary>
    public void Step(
        EnemyPool enemies,
        Vector2 playerPosition,
        float delta,
        double elapsedSeconds,
        Func<Vector2, Vector2, float, int, int, bool> emitProjectile)
    {
        for (int index = 0; index < enemies.Count; index++)
        {
            EnemyComponent enemy = enemies.Get(index);
            if (!enemy.Alive || !enemy.Definition.ProjectileProfile.Enabled)
            {
                continue;
            }

            enemy.FireCooldown -= delta;
            if (enemy.Definition.IsBoss)
            {
                enemy.BossPhase = DetermineBossPhase(enemy.Health, enemy.Definition.MaxHealth);
            }

            if (enemy.FireCooldown <= 0.0f)
            {
                enemy.FireCooldown = enemy.Definition.IsBoss
                    ? EmitBossPattern(ref enemy, playerPosition, elapsedSeconds, emitProjectile)
                    : EmitOrdinaryVolley(enemy, playerPosition, elapsedSeconds, emitProjectile);
            }

            enemies.Set(index, enemy);
        }
    }

    /// <summary>
    /// 按剩余生命比例选择三个互斥阶段；边界归入更危险的下一阶段，避免一帧来回抖动。
    /// </summary>
    public static BossBulletPhase DetermineBossPhase(int health, int maximumHealth)
    {
        float ratio = maximumHealth <= 0 ? 0.0f : health / (float)maximumHealth;
        if (ratio > 0.66f)
        {
            return BossBulletPhase.AimedFan;
        }

        return ratio > 0.33f ? BossBulletPhase.Ring : BossBulletPhase.AlternatingSpiral;
    }

    /// <summary>
    /// 生成以玩家方向为中心的普通扇形；后期逐渐增加弹数、弹速和伤害，将单发敌人演化为弹幕敌人。
    /// </summary>
    private static float EmitOrdinaryVolley(
        EnemyComponent enemy,
        Vector2 playerPosition,
        double elapsedSeconds,
        Func<Vector2, Vector2, float, int, int, bool> emitProjectile)
    {
        EnemyProjectileProfile profile = enemy.Definition.ProjectileProfile;
        int waveBonus = CombatIntensityCurve.GetWaveBonus(elapsedSeconds);
        int count = CapVolley((long)profile.ShotCount + waveBonus);
        float spread = profile.SpreadDegrees > 0.0f
            ? profile.SpreadDegrees
            : Math.Min(24.0f, waveBonus * 3.0f);
        Vector2 aimed = enemy.Position.DirectionTo(playerPosition);
        EmitFan(enemy.Position, aimed, count, spread,
            profile.ProjectileSpeed * CombatIntensityCurve.GetBulletSpeedMultiplier(elapsedSeconds),
            profile.Damage + CombatIntensityCurve.GetDamageBonus(elapsedSeconds), 0,
            emitProjectile);
        return profile.FireInterval * CombatIntensityCurve.GetFireIntervalMultiplier(elapsedSeconds);
    }

    /// <summary>
    /// 根据 Boss 当前阶段生成瞄准扇形、完整环形或正反交错旋转弹，并返回下一次发射间隔。
    /// </summary>
    private static float EmitBossPattern(
        ref EnemyComponent enemy,
        Vector2 playerPosition,
        double elapsedSeconds,
        Func<Vector2, Vector2, float, int, int, bool> emitProjectile)
    {
        EnemyProjectileProfile profile = enemy.Definition.ProjectileProfile;
        int waveBonus = CombatIntensityCurve.GetWaveBonus(elapsedSeconds);
        float speed = profile.ProjectileSpeed *
            CombatIntensityCurve.GetBulletSpeedMultiplier(elapsedSeconds);
        int damage = profile.Damage + CombatIntensityCurve.GetDamageBonus(elapsedSeconds);
        float interval;
        switch (enemy.BossPhase)
        {
            case BossBulletPhase.Ring:
                EmitRing(enemy.Position, CapVolley(14L + waveBonus * 2L), enemy.PatternAngle,
                    speed * 0.88f, damage, 1, emitProjectile);
                enemy.PatternAngle += 0.11f;
                interval = 0.92f;
                break;
            case BossBulletPhase.AlternatingSpiral:
                EmitSpiral(ref enemy, CapVolley(2L + waveBonus), speed * 1.08f,
                    damage, emitProjectile);
                interval = 0.20f;
                break;
            default:
                EmitFan(enemy.Position, enemy.Position.DirectionTo(playerPosition),
                    CapVolley(5L + waveBonus * 2L),
                    12.0f, speed, damage, 2, emitProjectile);
                interval = 1.12f;
                break;
        }

        return interval * CombatIntensityCurve.GetFireIntervalMultiplier(elapsedSeconds);
    }

    /// <summary>围绕中心方向均匀展开奇偶兼容的扇形，所有角度以度数档案转换为弧度。</summary>
    private static void EmitFan(
        Vector2 position,
        Vector2 centerDirection,
        int count,
        float spreadDegrees,
        float speed,
        int damage,
        int visualVariant,
        Func<Vector2, Vector2, float, int, int, bool> emitProjectile)
    {
        float centerAngle = centerDirection.Angle();
        float spread = Mathf.DegToRad(spreadDegrees);
        float start = centerAngle - spread * (count - 1) * 0.5f;
        for (int shot = 0; shot < count; shot++)
        {
            float angle = count <= 1 ? centerAngle : start + spread * shot;
            emitProjectile(position, Vector2.FromAngle(angle), speed, damage, visualVariant);
        }
    }

    /// <summary>按整圆等角度发射一波环形弹，起始相位逐波偏移以避免形成永久安全直线。</summary>
    private static void EmitRing(
        Vector2 position,
        int count,
        float phase,
        float speed,
        int damage,
        int visualVariant,
        Func<Vector2, Vector2, float, int, int, bool> emitProjectile)
    {
        for (int shot = 0; shot < count; shot++)
        {
            float angle = phase + Mathf.Tau * shot / count;
            emitProjectile(position, Vector2.FromAngle(angle), speed, damage, visualVariant);
        }
    }

    /// <summary>围绕累计相位发射交错旋转弹，每波翻转偏移方向并持续推进基础角度。</summary>
    private static void EmitSpiral(
        ref EnemyComponent enemy,
        int count,
        float speed,
        int damage,
        Func<Vector2, Vector2, float, int, int, bool> emitProjectile)
    {
        float spacing = Mathf.Tau / Math.Max(2, count);
        for (int shot = 0; shot < count; shot++)
        {
            float angle = enemy.PatternAngle + spacing * shot * enemy.PatternDirection;
            emitProjectile(enemy.Position, Vector2.FromAngle(angle), speed, damage, 3);
        }

        enemy.PatternAngle += 0.21f;
        enemy.PatternDirection *= -1.0f;
    }

    /// <summary>把任意长局的期望单波弹数限制在固定发射预算内，余下强度交给共享伤害与弹速倍率。</summary>
    private static int CapVolley(long desired) =>
        (int)Math.Clamp(desired, 1L, MaximumShotsPerVolley);
}
