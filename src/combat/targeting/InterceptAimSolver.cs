using Godot;

namespace TouhouWuxiaSurvivor.Combat.Targeting;

/// <summary>
/// 用目标匀速运动、弹丸实际速度与出生偏移解析最早拦截方向；无可靠交点时安全回退到直瞄。
/// </summary>
public static class InterceptAimSolver
{
    private const double EquationEpsilon = 0.000001;
    private const double MinimumFlightSeconds = 0.0001;

    /// <summary>
    /// 返回可直接交给武器的归一化方向；非法输入、目标过近或寿命内无法追上时返回目标当前方向。
    /// </summary>
    public static Vector2 ResolveDirection(
        Vector2 origin,
        TargetMotion target,
        float projectileSpeed,
        float spawnDistance,
        float maximumFlightSeconds)
    {
        return TrySolve(origin, target, projectileSpeed, spawnDistance,
            maximumFlightSeconds, out Vector2 direction, out _)
            ? direction
            : ResolveDirectDirection(origin, target);
    }

    /// <summary>
    /// 解二次拦截方程并返回最早正时间；出生偏移按最终射向计入，因此不会把枪口前移误当成额外飞行时间。
    /// </summary>
    public static bool TrySolve(
        Vector2 origin,
        TargetMotion target,
        float projectileSpeed,
        float spawnDistance,
        float maximumFlightSeconds,
        out Vector2 direction,
        out float flightSeconds)
    {
        direction = ResolveDirectDirection(origin, target);
        flightSeconds = 0.0f;
        if (!IsFinite(origin) || !IsFinite(target.Position) || !IsFinite(target.Velocity) ||
            !float.IsFinite(projectileSpeed) || projectileSpeed <= 0.0f ||
            !float.IsFinite(spawnDistance) || !float.IsFinite(maximumFlightSeconds) ||
            maximumFlightSeconds <= 0.0f)
        {
            return false;
        }

        Vector2 relative = target.Position - origin;
        double distance = relative.Length();
        double launchOffset = Math.Max(0.0, spawnDistance);
        if (distance <= launchOffset + EquationEpsilon)
        {
            return false;
        }

        double speed = projectileSpeed;
        double a = target.Velocity.LengthSquared() - speed * speed;
        double b = 2.0 * (relative.Dot(target.Velocity) - launchOffset * speed);
        double c = relative.LengthSquared() - launchOffset * launchOffset;
        if (!TryFindEarliestTime(a, b, c, maximumFlightSeconds, out double time))
        {
            return false;
        }

        Vector2 interceptOffset = relative + target.Velocity * (float)time;
        if (!IsFinite(interceptOffset) || interceptOffset.IsZeroApprox())
        {
            return false;
        }

        direction = interceptOffset.Normalized();
        flightSeconds = (float)time;
        return true;
    }

    /// <summary>
    /// 分别处理退化线性方程与常规二次方程，只接受弹丸寿命内最早的有限正根。
    /// </summary>
    private static bool TryFindEarliestTime(
        double a,
        double b,
        double c,
        double maximumFlightSeconds,
        out double time)
    {
        time = 0.0;
        if (Math.Abs(a) <= EquationEpsilon)
        {
            if (Math.Abs(b) <= EquationEpsilon)
            {
                return false;
            }

            double linear = -c / b;
            return TryAcceptTime(linear, maximumFlightSeconds, ref time);
        }

        double discriminant = b * b - 4.0 * a * c;
        if (!double.IsFinite(discriminant) || discriminant < 0.0)
        {
            return false;
        }

        double root = Math.Sqrt(Math.Max(0.0, discriminant));
        double denominator = 2.0 * a;
        TryAcceptTime((-b - root) / denominator, maximumFlightSeconds, ref time);
        TryAcceptTime((-b + root) / denominator, maximumFlightSeconds, ref time);
        return time > 0.0;
    }

    /// <summary>
    /// 将一个候选根并入当前最早时间，排除负根、非有限根与超过弹丸寿命的伪解。
    /// </summary>
    private static bool TryAcceptTime(double candidate, double maximum, ref double accepted)
    {
        if (!double.IsFinite(candidate) || candidate < MinimumFlightSeconds ||
            candidate > maximum + EquationEpsilon)
        {
            return false;
        }

        if (accepted <= 0.0 || candidate < accepted)
        {
            accepted = candidate;
        }

        return true;
    }

    /// <summary>
    /// 生成失败路径使用的稳定直瞄方向；目标重合时优先沿其速度发射，静止重合则统一向右。
    /// </summary>
    private static Vector2 ResolveDirectDirection(Vector2 origin, TargetMotion target)
    {
        Vector2 direct = target.Position - origin;
        if (IsFinite(direct) && !direct.IsZeroApprox())
        {
            return direct.Normalized();
        }

        return IsFinite(target.Velocity) && !target.Velocity.IsZeroApprox()
            ? target.Velocity.Normalized()
            : Vector2.Right;
    }

    /// <summary>确认二维向量的两个分量都可参与确定性拦截计算。</summary>
    private static bool IsFinite(Vector2 value) =>
        float.IsFinite(value.X) && float.IsFinite(value.Y);
}
