namespace TouhouWuxiaSurvivor.Gameplay.SpellCards.Runtime;

/// <summary>
/// 保存单张奥义的可变周期状态，并在构筑改变周期时按已完成比例平滑换算剩余时间。
/// </summary>
public sealed class SpellCardTimerState
{
    public float IntervalSeconds { get; private set; }
    public float RemainingSeconds { get; private set; }
    public bool IsReady => RemainingSeconds <= 0.0f;

    /// <summary>以完整首周期建立计时，确保刚悟得奥义不会绕过定时规则立即施展。</summary>
    public SpellCardTimerState(float intervalSeconds)
    {
        IntervalSeconds = RequirePositive(intervalSeconds);
        RemainingSeconds = IntervalSeconds;
    }

    /// <summary>按非负游戏时间推进计时；负数剩余值用于稳定比较多张同时超期的奥义。</summary>
    public void Advance(float elapsedSeconds) =>
        RemainingSeconds -= Math.Max(0.0f, elapsedSeconds);

    /// <summary>成功施展后从当前属性重新开始一个完整周期，失败施展不改写就绪状态。</summary>
    public void Restart(float intervalSeconds)
    {
        IntervalSeconds = RequirePositive(intervalSeconds);
        RemainingSeconds = IntervalSeconds;
    }

    /// <summary>施展因缺少目标失败时仅等待短起手周期，避免逐帧反复索敌且不重置完整周天。</summary>
    public void Retry(float retrySeconds)
    {
        if (RemainingSeconds <= 0.0f)
        {
            RemainingSeconds = RequirePositive(retrySeconds);
        }
    }

    /// <summary>
    /// 周期变化时保留当前完成比例；已经到期的奥义继续保持到期，不会因升级被推迟。
    /// </summary>
    public void Rescale(float intervalSeconds)
    {
        float nextInterval = RequirePositive(intervalSeconds);
        if (MathF.Abs(nextInterval - IntervalSeconds) <= 0.0001f)
        {
            return;
        }

        if (RemainingSeconds > 0.0f)
        {
            RemainingSeconds = RemainingSeconds / IntervalSeconds * nextInterval;
        }

        IntervalSeconds = nextInterval;
    }

    /// <summary>拒绝非有限或非正周期，阻止异常倍率制造永不触发或逐帧触发的奥义。</summary>
    private static float RequirePositive(float value) =>
        float.IsFinite(value) && value > 0.0f
            ? value
            : throw new ArgumentOutOfRangeException(nameof(value));
}
