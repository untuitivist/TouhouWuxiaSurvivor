using Godot;
using TouhouWuxiaSurvivor.Ecs.Combat.Projectiles;

namespace TouhouWuxiaSurvivor.Ecs.Combat.Bosses;

/// <summary>
/// 把符卡攻击档案转换为批量敌弹请求；只负责空间演出，不查询内容清单或写入实体池。
/// </summary>
public static class BossSpellProjectileEmitter
{
    /// <summary>按既有三段血量强度生成符卡弹幕，并返回下一波等待时间。</summary>
    public static float Emit(
        ref EnemyComponent enemy,
        Vector2 playerPosition,
        BossAttackPattern attack,
        int visualSourceId,
        Func<EnemyProjectileSpawnRequest, bool> emitProjectile)
    {
        (float countScale, float intervalScale) = enemy.BossPhase switch
        {
            BossBulletPhase.Ring => (1.25f, 0.86f),
            BossBulletPhase.AlternatingSpiral => (1.50f, 0.72f),
            _ => (1.0f, 1.0f),
        };
        int count = (int)Math.Clamp(
            Math.Ceiling(attack.ShotCount * countScale),
            1.0,
            EnemyProjectileSystem.MaximumShotsPerVolley);
        UpdatePresentation(ref enemy, attack.DisplayName);
        EmitPattern(ref enemy, playerPosition, attack, count,
            visualSourceId, emitProjectile);
        enemy.PatternAngle += 0.13f * enemy.PatternDirection;
        if (enemy.BossPhase == BossBulletPhase.AlternatingSpiral)
        {
            enemy.PatternDirection *= -1.0f;
        }
        return Math.Max(0.16f, attack.FireInterval * intervalScale);
    }

    /// <summary>按纯几何枚举分派弹幕，不让单个角色拥有隐藏的特殊数值分支。</summary>
    private static void EmitPattern(
        ref EnemyComponent enemy,
        Vector2 playerPosition,
        BossAttackPattern attack,
        int count,
        int visualSourceId,
        Func<EnemyProjectileSpawnRequest, bool> emitProjectile)
    {
        if (BossOriginalPatternEmitter.TryEmit(ref enemy, playerPosition,
                attack, count, visualSourceId, emitProjectile))
        {
            return;
        }

        Vector2 aimed = enemy.Position.DirectionTo(playerPosition);
        switch (attack.PatternKind)
        {
            case BossProjectilePatternKind.Orbit:
                EmitOrbit(enemy.Position, playerPosition, count, enemy.PatternAngle,
                    attack, visualSourceId, emitProjectile);
                break;
            case BossProjectilePatternKind.Ring:
                EmitRing(enemy.Position, count, enemy.PatternAngle,
                    attack, visualSourceId, emitProjectile);
                break;
            case BossProjectilePatternKind.Backstab:
                EmitCrossfire(enemy.Position, playerPosition, aimed, count,
                    attack, visualSourceId, emitProjectile);
                break;
            case BossProjectilePatternKind.Line:
                EmitFan(enemy.Position, aimed, count, attack.SpreadDegrees * 0.18f,
                    8, attack, visualSourceId, emitProjectile);
                break;
            default:
                EmitFan(enemy.Position, aimed, count, attack.SpreadDegrees,
                    12, attack, visualSourceId, emitProjectile);
                break;
        }
    }

    /// <summary>仅在阶段切换到另一张符卡时重新显示名称，避免每波射击造成文字闪烁。</summary>
    private static void UpdatePresentation(ref EnemyComponent enemy, string displayName)
    {
        if (string.Equals(enemy.ActiveSpellName, displayName, StringComparison.Ordinal)) return;
        enemy.ActiveSpellName = displayName;
        enemy.SpellAnnouncementTime = 2.2f;
    }

    /// <summary>从 Boss 本体向玩家方向展开扇面；直线型通过较小张角复用相同预算。</summary>
    private static void EmitFan(
        Vector2 position,
        Vector2 aimed,
        int count,
        float spreadDegrees,
        int variant,
        BossAttackPattern attack,
        int visualSourceId,
        Func<EnemyProjectileSpawnRequest, bool> emitProjectile)
    {
        float center = aimed.Angle();
        float spread = Mathf.DegToRad(spreadDegrees);
        float start = center - spread * (count - 1) * 0.5f;
        for (int shot = 0; shot < count; shot++)
        {
            float angle = count <= 1 ? center : start + spread * shot;
            Emit(position, Vector2.FromAngle(angle), shot + variant,
                attack, visualSourceId, emitProjectile);
        }
    }

    /// <summary>从 Boss 中心向整圆外放弹幕，逐阶段积累的相位防止出现永久安全直线。</summary>
    private static void EmitRing(
        Vector2 position,
        int count,
        float phase,
        BossAttackPattern attack,
        int visualSourceId,
        Func<EnemyProjectileSpawnRequest, bool> emitProjectile)
    {
        for (int shot = 0; shot < count; shot++)
        {
            Emit(position, Vector2.FromAngle(phase + Mathf.Tau * shot / count),
                shot + 4, attack, visualSourceId, emitProjectile);
        }
    }

    /// <summary>从 Boss 周身等距起手并向玩家内收，表达环绕类符卡。</summary>
    private static void EmitOrbit(
        Vector2 center,
        Vector2 playerPosition,
        int count,
        float phase,
        BossAttackPattern attack,
        int visualSourceId,
        Func<EnemyProjectileSpawnRequest, bool> emitProjectile)
    {
        for (int shot = 0; shot < count; shot++)
        {
            Vector2 spawn = center + Vector2.FromAngle(
                phase + Mathf.Tau * shot / count) * attack.SpawnDistance;
            Emit(spawn, spawn.DirectionTo(playerPosition), shot,
                attack, visualSourceId, emitProjectile);
        }
    }

    /// <summary>从 Boss 两侧交错起手并瞄准玩家，表达时停、隙间或夹击类符卡。</summary>
    private static void EmitCrossfire(
        Vector2 center,
        Vector2 playerPosition,
        Vector2 aimed,
        int count,
        BossAttackPattern attack,
        int visualSourceId,
        Func<EnemyProjectileSpawnRequest, bool> emitProjectile)
    {
        Vector2 side = aimed.IsZeroApprox() ? Vector2.Up : aimed.Orthogonal();
        for (int shot = 0; shot < count; shot++)
        {
            float offset = ((shot & 1) == 0 ? -1.0f : 1.0f) * attack.SpawnDistance;
            Vector2 spawn = center + side * offset;
            Emit(spawn, spawn.DirectionTo(playerPosition), shot + 16,
                attack, visualSourceId, emitProjectile);
        }
    }

    /// <summary>把一枚符卡弹的公共数值写入请求，所有几何只决定起点、方向和变体。</summary>
    private static void Emit(
        Vector2 position,
        Vector2 direction,
        int variant,
        BossAttackPattern attack,
        int visualSourceId,
        Func<EnemyProjectileSpawnRequest, bool> emitProjectile) =>
        emitProjectile(new EnemyProjectileSpawnRequest(
            position,
            direction,
            attack.ProjectileSpeed,
            attack.Damage,
            variant,
            attack.VisualStyleId,
            visualSourceId));
}
