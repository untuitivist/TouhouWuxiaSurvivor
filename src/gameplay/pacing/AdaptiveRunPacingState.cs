namespace TouhouWuxiaSurvivor.Gameplay.Pacing;

/// <summary>
/// 以滚动击破率和敌群存量驱动有限阶段；最短展示与最长兜底共同避免爆发误判和弱构筑死锁。
/// </summary>
public sealed class AdaptiveRunPacingState
{
    private const double KillRateSmoothingSeconds = 4.0;
    private int _phaseIndex;
    private double _phaseEnteredSeconds;
    private double _lastElapsedSeconds;
    private int _lastDefeatedEnemies;
    private double _smoothedKillsPerSecond;
    private double _dominanceHeldSeconds;
    private double _latestDominanceScore;
    private double _secondsSinceDefeat = double.PositiveInfinity;

    public RunPhaseId PhaseId => _phaseIndex < RunPacingTimeline.AdaptiveRules.Count
        ? RunPacingTimeline.AdaptiveRules[_phaseIndex].PhaseId
        : RunPhaseId.FinalEncounter;
    public bool IsFinalEncounter => PhaseId == RunPhaseId.FinalEncounter;

    /// <summary>
    /// 消费单调战斗时钟和累计遥测；一次大跨度调用会按各阶段最长时限补齐，避免暂停或测试跳时丢阶段。
    /// </summary>
    public bool Advance(double elapsedSeconds, RunCombatTelemetry telemetry)
    {
        double elapsed = Math.Max(_lastElapsedSeconds, NormalizeElapsed(elapsedSeconds));
        RunCombatTelemetry sample = telemetry.Normalize();
        double delta = Math.Max(0.0, elapsed - _lastElapsedSeconds);
        UpdateKillRate(delta, sample.DefeatedEnemies);
        _lastElapsedSeconds = Math.Max(_lastElapsedSeconds, elapsed);
        _lastDefeatedEnemies = Math.Max(_lastDefeatedEnemies, sample.DefeatedEnemies);
        if (IsFinalEncounter)
        {
            return false;
        }

        bool changed = false;
        while (_phaseIndex < RunPacingTimeline.AdaptiveRules.Count)
        {
            RunPhaseRule rule = RunPacingTimeline.AdaptiveRules[_phaseIndex];
            double phaseElapsed = Math.Max(0.0, elapsed - _phaseEnteredSeconds);
            double dominance = CalculateDominance(rule, sample);
            _latestDominanceScore = Math.Clamp(dominance, 0.0, 1.0);
            if (phaseElapsed >= rule.MinimumDurationSeconds && dominance >= 1.0)
            {
                _dominanceHeldSeconds += Math.Min(delta, 1.0);
            }
            else
            {
                _dominanceHeldSeconds = Math.Max(0.0, _dominanceHeldSeconds - delta * 0.5);
            }

            bool dominated = phaseElapsed >= rule.MinimumDurationSeconds &&
                _dominanceHeldSeconds >= rule.RequiredDominanceSeconds;
            bool timedOut = phaseElapsed >= rule.MaximumDurationSeconds;
            if (!dominated && !timedOut)
            {
                break;
            }

            _phaseIndex++;
            changed = true;
            _dominanceHeldSeconds = 0.0;
            _phaseEnteredSeconds = dominated
                ? elapsed
                : _phaseEnteredSeconds + rule.MaximumDurationSeconds;
            if (_phaseIndex >= RunPacingTimeline.AdaptiveRules.Count)
            {
                break;
            }
        }

        return changed;
    }

    /// <summary>把当前阶段状态投影成 HUD 与玩法共同消费的不可变快照。</summary>
    public RunPacingSnapshot CreateSnapshot(double elapsedSeconds, bool isEndless = false)
    {
        double elapsed = Math.Max(_lastElapsedSeconds, NormalizeElapsed(elapsedSeconds));
        if (isEndless)
        {
            return RunPacingTimeline.CreateTerminalSnapshot(
                RunPhaseId.Endless, "无尽游历", "敌群与角色Boss将持续增强", elapsed, true);
        }

        if (IsFinalEncounter)
        {
            return RunPacingTimeline.CreateTerminalSnapshot(
                RunPhaseId.FinalEncounter, "异变核心", "击破角色Boss以平定异变", elapsed, false);
        }

        RunPhaseRule rule = RunPacingTimeline.AdaptiveRules[_phaseIndex];
        RunPhaseDefinition authored = RunPacingTimeline.StructuredPhases[_phaseIndex];
        double phaseElapsed = Math.Max(0.0, elapsed - _phaseEnteredSeconds);
        double timeProgress = Math.Clamp(phaseElapsed / rule.MinimumDurationSeconds, 0.0, 1.0);
        double timeoutProgress = Math.Clamp(
            (phaseElapsed - rule.MinimumDurationSeconds) /
            Math.Max(0.001, rule.MaximumDurationSeconds - rule.MinimumDurationSeconds), 0.0, 1.0);
        double holdProgress = Math.Clamp(
            _dominanceHeldSeconds / rule.RequiredDominanceSeconds, 0.0, 1.0);
        double dominanceProgress = phaseElapsed < rule.MinimumDurationSeconds
            ? 0.0
            : _latestDominanceScore * 0.70 + holdProgress * 0.30;
        double phaseProgress = phaseElapsed < rule.MinimumDurationSeconds
            ? timeProgress * 0.60
            : 0.60 + Math.Max(timeoutProgress, holdProgress) * 0.40;
        double difficultySeconds = authored.StartSeconds +
            (authored.EndSeconds - authored.StartSeconds) * phaseProgress;
        string nextName = _phaseIndex + 1 < RunPacingTimeline.StructuredPhases.Count
            ? RunPacingTimeline.StructuredPhases[_phaseIndex + 1].DisplayName
            : "异变核心";
        return new RunPacingSnapshot(
            rule.PhaseId, authored.DisplayName, authored.CueText, nextName,
            elapsed, _phaseEnteredSeconds, _phaseEnteredSeconds + rule.MaximumDurationSeconds,
            Math.Clamp(difficultySeconds / RunPacingTimeline.FinalEncounterSeconds, 0.0, 1.0),
            phaseProgress,
            phaseElapsed < rule.MinimumDurationSeconds
                ? rule.MinimumDurationSeconds - phaseElapsed
                : Math.Max(0.0, rule.MaximumDurationSeconds - phaseElapsed),
            false, false, difficultySeconds,
            Math.Clamp(dominanceProgress, 0.0, 1.0),
            phaseElapsed >= rule.MinimumDurationSeconds);
    }

    /// <summary>用指数平滑吸收累计击破变化，避免一轮爆发被误认为持续割草能力。</summary>
    private void UpdateKillRate(double delta, int defeatedEnemies)
    {
        if (delta <= 0.0)
        {
            return;
        }

        int defeatedDelta = Math.Max(0, defeatedEnemies - _lastDefeatedEnemies);
        _secondsSinceDefeat = defeatedDelta > 0
            ? 0.0
            : Math.Min(double.MaxValue, _secondsSinceDefeat + delta);
        double instantaneous = defeatedDelta / delta;
        double blend = 1.0 - Math.Exp(-delta / KillRateSmoothingSeconds);
        _smoothedKillsPerSecond += (instantaneous - _smoothedKillsPerSecond) * blend;
    }

    /// <summary>要求击破效率与低积压同时达标；两者中较弱的一项决定当前压制分数。</summary>
    private double CalculateDominance(RunPhaseRule rule, RunCombatTelemetry telemetry)
    {
        double requiredKills = Math.Max(
            rule.RequiredKillsPerSecond, telemetry.ScheduledSpawnsPerSecond);
        bool battlefieldCleared = telemetry.AliveEnemies <= 2;
        double killScore = battlefieldCleared
            ? 1.0
            : _secondsSinceDefeat <= 1.5
                ? _smoothedKillsPerSecond / requiredKills
                : 0.0;
        double aliveRatio = telemetry.AliveEnemies / (double)telemetry.AliveLimit;
        double crowdScore = aliveRatio <= rule.MaximumAliveRatio
            ? 1.0 + (rule.MaximumAliveRatio - aliveRatio)
            : rule.MaximumAliveRatio / Math.Max(rule.MaximumAliveRatio, aliveRatio);
        return Math.Min(killScore, crowdScore);
    }

    /// <summary>将非法或倒退时钟整理为单调有限秒数。</summary>
    private static double NormalizeElapsed(double elapsedSeconds)
    {
        if (!double.IsFinite(elapsedSeconds))
        {
            return elapsedSeconds > 0.0 ? double.MaxValue : 0.0;
        }

        return Math.Max(0.0, elapsedSeconds);
    }
}
