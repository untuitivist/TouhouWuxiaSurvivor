using Godot;
using TouhouWuxiaSurvivor.Ecs.Combat.Projectiles;
using TouhouWuxiaSurvivor.Gameplay.SpellCards.Contracts;
using TouhouWuxiaSurvivor.Gameplay.SpellCards.Definitions;

namespace TouhouWuxiaSurvivor.Gameplay.SpellCards.Patterns;

/// <summary>
/// 把声明式原作演出转换为玩家弹丸的复合弹型与运动档案，不参与伤害、目标数或触发预算。
/// </summary>
public static class SpellCardPatternRuntimePolicy
{
    /// <summary>按弹丸序号解析主辅弹型，确保复合演出仍只读取当前内容包自己的图集。</summary>
    public static SpellBulletStyleKind ResolveStyle(
        SpellCardDefinition card,
        int projectileIndex) => card.Pattern.ResolveStyle(
            card.BulletStyleKind, projectileIndex);

    /// <summary>用本次实际飞行时间换算冻结、停顿和旋流阶段，拒绝独立写死秒数。</summary>
    public static ProjectileMotionProfile CreateMotion(
        SpellCardDefinition card,
        ResolvedSpellCardCombat combat,
        int projectileIndex,
        Vector2 target)
    {
        float duration = Math.Max(0.1f, combat.TravelDurationSeconds);
        float transition = duration * card.Pattern.PhaseRatio;
        float hold = duration * card.Pattern.HoldRatio;
        float sign = (projectileIndex & 1) == 0 ? 1.0f : -1.0f;
        return card.Pattern.Kind switch
        {
            SpellCardPatternKind.FreezeRelease => new ProjectileMotionProfile(
                ProjectileMotionKind.FreezeResume, transition, hold,
                TurnAngle: (((projectileIndex * 37) % 9) - 4) * 0.06f),
            SpellCardPatternKind.TimeStopRedirect => new ProjectileMotionProfile(
                ProjectileMotionKind.RedirectOnce, transition, hold,
                RedirectTarget: target),
            SpellCardPatternKind.RotatingStream or SpellCardPatternKind.SweepingBeam =>
                new ProjectileMotionProfile(
                    ProjectileMotionKind.CurvedStream,
                    Math.Max(transition, duration * 0.25f),
                    AngularVelocity: Mathf.Tau * card.Pattern.TurnRateScale /
                        duration * sign),
            _ => default,
        };
    }

    /// <summary>只为弧列和多波星屑追加小幅起手曲率，不改变几何策略规划的目标数量。</summary>
    public static float ResolveCurvature(
        SpellCardDefinition card,
        int projectileIndex,
        float geometryCurvature)
    {
        if (card.Pattern.Kind is not (SpellCardPatternKind.AimedArc or
            SpellCardPatternKind.StardustFan)) return geometryCurvature;
        int wave = projectileIndex % card.Pattern.WaveCount;
        float centered = wave - (card.Pattern.WaveCount - 1) * 0.5f;
        return geometryCurvature + centered * 0.055f;
    }
}
