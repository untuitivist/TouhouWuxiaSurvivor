using Godot;
using TouhouWuxiaSurvivor.Ecs.Combat.Projectiles;

namespace TouhouWuxiaSurvivor.Ecs.Combat.Bosses;

/// <summary>
/// 把已校对原作时序转成纯 ECS 敌弹请求；通用语法不读取作品、角色或符卡名称。
/// </summary>
public static class BossOriginalPatternEmitter
{
    /// <summary>执行显式原作语法并返回 true；旧几何返回 false 交由兼容发射器处理。</summary>
    public static bool TryEmit(
        ref EnemyComponent enemy,
        Vector2 playerPosition,
        BossAttackPattern attack,
        int count,
        int visualSourceId,
        Func<EnemyProjectileSpawnRequest, bool> emit)
    {
        Vector2 aimed = SafeDirection(enemy.Position, playerPosition);
        switch (attack.PatternKind)
        {
            case BossProjectilePatternKind.HomingOrbit:
                EmitOrbit(enemy.Position, playerPosition, enemy.PatternAngle,
                    attack, count, visualSourceId, emit);
                return true;
            case BossProjectilePatternKind.SealPulse:
                EmitRing(enemy.Position, enemy.PatternAngle, attack, count,
                    visualSourceId, emit);
                return true;
            case BossProjectilePatternKind.StraightBeam:
                EmitFan(enemy.Position, aimed, attack.SpreadDegrees * 0.08f,
                    attack, count, visualSourceId, emit);
                return true;
            case BossProjectilePatternKind.StardustFan:
                EmitFan(enemy.Position, aimed, attack.SpreadDegrees,
                    attack, count, visualSourceId, emit);
                return true;
            case BossProjectilePatternKind.AimedArc:
                EmitAimedArcs(enemy.Position, aimed, attack.SpreadDegrees,
                    attack, count, visualSourceId, emit);
                return true;
            case BossProjectilePatternKind.FreezeRelease:
                EmitFreeze(enemy.Position, aimed, enemy.PatternAngle, attack, count,
                    visualSourceId, emit);
                return true;
            case BossProjectilePatternKind.RotatingStream:
                EmitStream(enemy.Position, enemy.PatternAngle, attack, count,
                    visualSourceId, emit);
                return true;
            case BossProjectilePatternKind.ElementalCycle:
                EmitElements(enemy.Position, aimed, enemy.PatternAngle,
                    attack, count, visualSourceId, emit);
                return true;
            case BossProjectilePatternKind.TimeStopRedirect:
                EmitRedirect(enemy.Position, playerPosition, enemy.PatternAngle,
                    attack, count, visualSourceId, emit);
                return true;
            case BossProjectilePatternKind.AimedTrail:
                EmitTrail(enemy.Position, aimed, attack, count, visualSourceId, emit);
                return true;
            case BossProjectilePatternKind.SweepingBeam:
                EmitSweep(enemy.Position, aimed, enemy.PatternAngle,
                    attack, count, visualSourceId, emit);
                return true;
            default:
                return false;
        }
    }

    /// <summary>围绕施术者布置弹源后内收瞄准，表达梦想封印一类多玉追踪起手。</summary>
    private static void EmitOrbit(Vector2 center, Vector2 target, float phase,
        BossAttackPattern attack, int count, int source, Func<EnemyProjectileSpawnRequest, bool> emit)
    {
        for (int index = 0; index < count; index++)
        {
            Vector2 spawn = center + Vector2.FromAngle(phase + Mathf.Tau * index / count)
                * attack.SpawnDistance;
            Emit(spawn, SafeDirection(spawn, target), index, attack, source, emit);
        }
    }

    /// <summary>生成完整圆周脉冲；波次只改变颜色和相位，不突破同一发射数量预算。</summary>
    private static void EmitRing(Vector2 center, float phase, BossAttackPattern attack,
        int count, int source, Func<EnemyProjectileSpawnRequest, bool> emit)
    {
        for (int index = 0; index < count; index++)
            Emit(center, Vector2.FromAngle(phase + Mathf.Tau * index / count),
                index, attack, source, emit);
    }

    /// <summary>按多列弧或星屑波分组展开瞄准扇面，组间微移形成可读空隙。</summary>
    private static void EmitFan(Vector2 center, Vector2 aimed, float spreadDegrees,
        BossAttackPattern attack, int count, int source, Func<EnemyProjectileSpawnRequest, bool> emit)
    {
        float spread = Mathf.DegToRad(spreadDegrees);
        for (int index = 0; index < count; index++)
        {
            float normalized = count <= 1 ? 0.0f : index / (float)(count - 1) - 0.5f;
            int wave = index % attack.WaveCount;
            float waveOffset = (wave - (attack.WaveCount - 1) * 0.5f) * 0.035f;
            Emit(center, aimed.Rotated(normalized * spread + waveOffset),
                index, attack, source, emit);
        }
    }

    /// <summary>把每列首弹精确瞄准玩家，其余圆弹向两侧展开成错位弧列。</summary>
    private static void EmitAimedArcs(Vector2 center, Vector2 aimed, float spreadDegrees,
        BossAttackPattern attack, int count, int source,
        Func<EnemyProjectileSpawnRequest, bool> emit)
    {
        int waves = Math.Min(attack.WaveCount, count);
        float spread = Mathf.DegToRad(spreadDegrees);
        Vector2 side = aimed.IsZeroApprox() ? Vector2.Right : aimed.Orthogonal();
        for (int index = 0; index < count; index++)
        {
            int wave = index % waves;
            int slot = index / waves;
            int tier = (slot + 1) / 2;
            float sign = (slot & 1) == 0 ? 1.0f : -1.0f;
            float angle = slot == 0 ? 0.0f : sign * tier * spread / 4.0f;
            float lane = wave - (waves - 1) * 0.5f;
            Emit(center + side * lane * 4.0f, aimed.Rotated(angle), index,
                attack, source, emit);
        }
    }

    /// <summary>彩弹先外放、冻结再错向恢复；辅弹保持蓝色瞄准列并穿过冻结阵。</summary>
    private static void EmitFreeze(Vector2 center, Vector2 aimed, float phase,
        BossAttackPattern attack, int count, int source,
        Func<EnemyProjectileSpawnRequest, bool> emit)
    {
        for (int index = 0; index < count; index++)
        {
            bool aimedAccent = attack.VisualBulletStyleIds.Count > 1 &&
                index % attack.VisualBulletStyleIds.Count != 0;
            if (aimedAccent)
            {
                float offset = ((index / attack.VisualBulletStyleIds.Count) % 3 - 1) * 0.08f;
                Emit(center, aimed.Rotated(offset), index, attack, source, emit);
                continue;
            }
            float turn = (((index * 37) % 11) - 5) * 0.08f;
            var motion = new ProjectileMotionProfile(
                ProjectileMotionKind.FreezeResume,
                attack.FireInterval * attack.PhaseRatio,
                attack.FireInterval * attack.HoldRatio,
                TurnAngle: turn);
            Emit(center, Vector2.FromAngle(phase + Mathf.Tau * index / count),
                index, attack, source, emit, motion);
        }
    }

    /// <summary>让多束米弹在起步阶段持续弯曲，随后保持切线方向离开中心。</summary>
    private static void EmitStream(Vector2 center, float phase, BossAttackPattern attack,
        int count, int source, Func<EnemyProjectileSpawnRequest, bool> emit)
    {
        float duration = Math.Max(0.1f, attack.FireInterval * attack.PhaseRatio);
        float angular = Mathf.Tau * attack.TurnRateScale / Math.Max(0.25f, attack.FireInterval);
        for (int index = 0; index < count; index++)
        {
            float sign = (index & 1) == 0 ? 1.0f : -1.0f;
            var motion = new ProjectileMotionProfile(
                ProjectileMotionKind.CurvedStream, duration,
                AngularVelocity: angular * sign);
            Emit(center, Vector2.FromAngle(phase + Mathf.Tau * index / count),
                index, attack, source, emit, motion);
        }
    }

    /// <summary>在五个弹式间轮换瞄准、环射、停转与漂移，所有分支共享本波总数。</summary>
    private static void EmitElements(Vector2 center, Vector2 aimed, float phase,
        BossAttackPattern attack, int count, int source, Func<EnemyProjectileSpawnRequest, bool> emit)
    {
        for (int index = 0; index < count; index++)
        {
            int element = index % attack.WaveCount;
            Vector2 direction = element switch
            {
                0 => aimed.Rotated((index / attack.WaveCount - 1) * 0.08f),
                3 => Vector2.Down.Rotated(-0.55f),
                _ => Vector2.FromAngle(phase + Mathf.Tau * index / count),
            };
            ProjectileMotionProfile motion = element == 2
                ? new ProjectileMotionProfile(ProjectileMotionKind.FreezeResume,
                    attack.FireInterval * attack.PhaseRatio, attack.FireInterval * 0.18f,
                    TurnAngle: 0.35f)
                : default;
            Emit(center, direction, index, attack, source, emit, motion);
        }
    }

    /// <summary>从圆形刀阵先外放并停顿，停止结束时整批重新瞄准玩家位置。</summary>
    private static void EmitRedirect(Vector2 center, Vector2 target, float phase,
        BossAttackPattern attack, int count, int source, Func<EnemyProjectileSpawnRequest, bool> emit)
    {
        for (int index = 0; index < count; index++)
        {
            Vector2 direction = Vector2.FromAngle(phase + Mathf.Tau * index / count);
            Vector2 spawn = center + direction * attack.SpawnDistance * 0.35f;
            var motion = new ProjectileMotionProfile(
                ProjectileMotionKind.RedirectOnce,
                attack.FireInterval * attack.PhaseRatio,
                attack.FireInterval * attack.HoldRatio,
                RedirectTarget: target);
            Emit(spawn, direction, index, attack, source, emit, motion);
        }
    }

    /// <summary>让主弹和辅弹沿同一瞄准扇面错开起点，形成大弹曳出小弹的可读轨迹。</summary>
    private static void EmitTrail(Vector2 center, Vector2 aimed, BossAttackPattern attack,
        int count, int source, Func<EnemyProjectileSpawnRequest, bool> emit)
    {
        float spread = Mathf.DegToRad(attack.SpreadDegrees);
        for (int index = 0; index < count; index++)
        {
            float normalized = count <= 1 ? 0.0f : index / (float)(count - 1) - 0.5f;
            Vector2 direction = aimed.Rotated(normalized * spread);
            int trail = index % attack.WaveCount;
            Emit(center - direction * trail * 7.0f, direction, index,
                attack, source, emit);
        }
    }

    /// <summary>以窄光束组沿累计相位横扫，辅弹沿相邻方向形成原作可见的残留弹列。</summary>
    private static void EmitSweep(Vector2 center, Vector2 aimed, float phase,
        BossAttackPattern attack, int count, int source, Func<EnemyProjectileSpawnRequest, bool> emit)
    {
        float spread = Mathf.DegToRad(Math.Max(8.0f, attack.SpreadDegrees));
        for (int index = 0; index < count; index++)
        {
            float normalized = count <= 1 ? 0.0f : index / (float)(count - 1) - 0.5f;
            float sweep = phase * attack.TurnRateScale + normalized * spread;
            Emit(center, aimed.Rotated(sweep), index, attack, source, emit);
        }
    }

    /// <summary>把一枚原作演出弹的公共数值、复合弹型与运动档案写入统一请求。</summary>
    private static void Emit(Vector2 position, Vector2 direction, int index,
        BossAttackPattern attack, int source, Func<EnemyProjectileSpawnRequest, bool> emit,
        ProjectileMotionProfile motion = default) => emit(new EnemyProjectileSpawnRequest(
            position, direction, attack.ProjectileSpeed, attack.Damage, index,
            attack.VisualStyleId, source, attack.ResolveVisualBulletStyleId(index), motion));

    /// <summary>返回稳定非零瞄准向量，目标与弹源重合时沿屏幕下方离开。</summary>
    private static Vector2 SafeDirection(Vector2 origin, Vector2 target)
    {
        Vector2 direction = origin.DirectionTo(target);
        return direction.IsZeroApprox() ? Vector2.Down : direction;
    }
}
