using Godot;
using TouhouWuxiaSurvivor.Actors.Enemies;
using TouhouWuxiaSurvivor.Ecs.Combat.Bosses;
using TouhouWuxiaSurvivor.Ecs.Combat.Projectiles;

namespace TouhouWuxiaSurvivor.Ecs.Combat;

/// <summary>
/// 批量推进普通敌人射击与角色 Boss 三阶段弹幕，发射结果通过委托写入共享投射物池。
/// </summary>
public sealed class EnemyProjectileSystem
{
    public const int MaximumShotsPerVolley = 96;
    private IBossAttackResolver? _bossAttacks;

    /// <summary>注入内容层的 Boss 攻击解析器；空值恢复所有角色的通用三阶段弹幕。</summary>
    public void ConfigureBossAttacks(IBossAttackResolver? resolver) =>
        _bossAttacks = resolver;

    /// <summary>
    /// 为存活敌人递减射击冷却，并严格按怪物档案或 Boss 血量阶段生成固定属性敌弹。
    /// </summary>
    public void Step(
        EnemyPool enemies,
        Vector2 playerPosition,
        float delta,
        Func<EnemyProjectileSpawnRequest, bool> emitProjectile)
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
                enemy.SpellAnnouncementTime = Math.Max(
                    0.0f, enemy.SpellAnnouncementTime - delta);
                enemy.BossPhase = DetermineBossPhase(enemy.Health, enemy.Definition.MaxHealth);
            }

            if (enemy.FireCooldown <= 0.0f)
            {
                enemy.FireCooldown = enemy.Definition.IsBoss
                    ? EmitBossPattern(ref enemy, playerPosition, emitProjectile)
                    : EmitOrdinaryVolley(enemy, playerPosition, emitProjectile);
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
    /// 生成以玩家方向为中心的普通扇形；弹数、速度、伤害和间隔全部来自该种类档案。
    /// </summary>
    private static float EmitOrdinaryVolley(
        EnemyComponent enemy,
        Vector2 playerPosition,
        Func<EnemyProjectileSpawnRequest, bool> emitProjectile)
    {
        EnemyProjectileProfile profile = enemy.Definition.ProjectileProfile;
        int count = CapVolley(profile.ShotCount);
        float spread = Math.Max(0.0f, profile.SpreadDegrees);
        Vector2 aimed = enemy.Position.DirectionTo(playerPosition);
        EmitFan(enemy.Position, aimed, count, spread,
            profile.ProjectileSpeed, profile.Damage, 0, 0,
            emitProjectile);
        return profile.FireInterval;
    }

    /// <summary>
    /// 根据 Boss 当前阶段生成瞄准扇形、完整环形或正反交错旋转弹，并返回下一次发射间隔。
    /// </summary>
    private float EmitBossPattern(
        ref EnemyComponent enemy,
        Vector2 playerPosition,
        Func<EnemyProjectileSpawnRequest, bool> emitProjectile)
    {
        if (_bossAttacks is not null && enemy.Definition.CharacterId is string characterId &&
            _bossAttacks.TryResolve(characterId, enemy.BossPhase, out BossAttackPattern attack))
        {
            return BossSpellProjectileEmitter.Emit(
                ref enemy, playerPosition, attack, emitProjectile);
        }

        EnemyProjectileProfile profile = enemy.Definition.ProjectileProfile;
        float speed = profile.ProjectileSpeed;
        int damage = profile.Damage;
        float interval;
        switch (enemy.BossPhase)
        {
            case BossBulletPhase.Ring:
                EmitRing(enemy.Position, CapVolley(14L), enemy.PatternAngle,
                    speed * 0.88f, damage, 1, 0, emitProjectile);
                enemy.PatternAngle += 0.11f;
                interval = 0.92f;
                break;
            case BossBulletPhase.AlternatingSpiral:
                EmitSpiral(ref enemy, CapVolley(2L), speed * 1.08f,
                    damage, emitProjectile);
                interval = 0.20f;
                break;
            default:
                EmitFan(enemy.Position, enemy.Position.DirectionTo(playerPosition),
                    CapVolley(5L),
                    12.0f, speed, damage, 2, 0, emitProjectile);
                interval = 1.12f;
                break;
        }

        return interval;
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
        int visualStyle,
        Func<EnemyProjectileSpawnRequest, bool> emitProjectile)
    {
        float centerAngle = centerDirection.Angle();
        float spread = Mathf.DegToRad(spreadDegrees);
        float start = centerAngle - spread * (count - 1) * 0.5f;
        for (int shot = 0; shot < count; shot++)
        {
            float angle = count <= 1 ? centerAngle : start + spread * shot;
            emitProjectile(new EnemyProjectileSpawnRequest(
                position, Vector2.FromAngle(angle), speed, damage,
                visualVariant, visualStyle));
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
        int visualStyle,
        Func<EnemyProjectileSpawnRequest, bool> emitProjectile)
    {
        for (int shot = 0; shot < count; shot++)
        {
            float angle = phase + Mathf.Tau * shot / count;
            emitProjectile(new EnemyProjectileSpawnRequest(
                position, Vector2.FromAngle(angle), speed, damage,
                visualVariant, visualStyle));
        }
    }

    /// <summary>围绕累计相位发射交错旋转弹，每波翻转偏移方向并持续推进基础角度。</summary>
    private static void EmitSpiral(
        ref EnemyComponent enemy,
        int count,
        float speed,
        int damage,
        Func<EnemyProjectileSpawnRequest, bool> emitProjectile)
    {
        float spacing = Mathf.Tau / Math.Max(2, count);
        for (int shot = 0; shot < count; shot++)
        {
            float angle = enemy.PatternAngle + spacing * shot * enemy.PatternDirection;
            emitProjectile(new EnemyProjectileSpawnRequest(
                enemy.Position, Vector2.FromAngle(angle), speed, damage, 3));
        }

        enemy.PatternAngle += 0.21f;
        enemy.PatternDirection *= -1.0f;
    }

    /// <summary>把怪物档案中的单波弹数限制在固定性能预算内，避免错误内容生成过量实体。</summary>
    private static int CapVolley(long desired) =>
        (int)Math.Clamp(desired, 1L, MaximumShotsPerVolley);
}
