namespace TouhouWuxiaSurvivor.Gameplay.Pacing;

/// <summary>
/// 以最近三十秒滑动窗口比较普通敌人的实际生成与击破；每个合格观察期最多提高一档。
/// </summary>
public sealed class AdaptiveRunPacingState
{
    private const double TransitionSeconds = 3.0;
    private readonly Queue<(double Time, int Spawned, int Defeated)> _samples = new();
    private int _phaseIndex;
    private double _phaseEnteredSeconds;
    private double _lastElapsedSeconds;
    private (double Time, int Spawned, int Defeated) _windowBaseline;
    private int _lastDefeatedEnemies;
    private int _lastSpawnedEnemies;
    private bool _initialized;

    public RunPhaseId PhaseId => _phaseIndex < RunPacingTimeline.AdaptiveRules.Count
        ? RunPacingTimeline.AdaptiveRules[_phaseIndex].PhaseId
        : RunPhaseId.FinalEncounter;
    public bool IsFinalEncounter => PhaseId == RunPhaseId.FinalEncounter;

    /// <summary>
    /// 消费单调战斗时钟和累计遥测；最近三十秒只有 K/S 达到九成且 S 大于零才提高一档。
    /// </summary>
    public bool Advance(double elapsedSeconds, RunCombatTelemetry telemetry)
    {
        double elapsed = Math.Max(_lastElapsedSeconds, NormalizeElapsed(elapsedSeconds));
        RunCombatTelemetry sample = telemetry.Normalize();
        _lastElapsedSeconds = elapsed;
        _lastSpawnedEnemies = Math.Max(_lastSpawnedEnemies, sample.SpawnedEnemies);
        _lastDefeatedEnemies = Math.Max(_lastDefeatedEnemies, sample.DefeatedEnemies);
        if (!_initialized)
        {
            _initialized = true;
            _phaseEnteredSeconds = elapsed;
            _windowBaseline = (elapsed, 0, 0);
            return false;
        }

        RecordSample(elapsed);

        if (IsFinalEncounter)
        {
            return false;
        }

        if (elapsed - _phaseEnteredSeconds < RunPacingTimeline.EvaluationWindowSeconds ||
            elapsed - _windowBaseline.Time < RunPacingTimeline.EvaluationWindowSeconds)
        {
            return false;
        }

        (int spawned, int defeated) = GetWindowCounts();
        bool advances = RunPacingTimeline.MeetsClearRequirement(spawned, defeated);
        if (!advances)
        {
            return false;
        }

        _phaseIndex++;
        _phaseEnteredSeconds = elapsed;
        return true;
    }

    /// <summary>把当前阶段状态投影成 HUD 与玩法共同消费的不可变快照。</summary>
    public RunPacingSnapshot CreateSnapshot(double elapsedSeconds, bool isEndless = false)
    {
        double elapsed = Math.Max(_lastElapsedSeconds, NormalizeElapsed(elapsedSeconds));
        (int spawned, int defeated) = GetWindowCounts();
        if (isEndless)
        {
            return RunPacingTimeline.CreateTerminalSnapshot(
                RunPhaseId.Endless, "无尽游历", "敌群数量将持续增强", elapsed, true) with
            {
                WindowSpawned = spawned,
                WindowDefeated = defeated,
            };
        }

        if (IsFinalEncounter)
        {
            return RunPacingTimeline.CreateTerminalSnapshot(
                RunPhaseId.FinalEncounter, "异变核心", "击破角色Boss以平定异变", elapsed, false) with
            {
                WindowSpawned = spawned,
                WindowDefeated = defeated,
            };
        }

        RunPhaseDefinition authored = RunPacingTimeline.StructuredPhases[_phaseIndex];
        double phaseElapsed = Math.Max(0.0, elapsed - _phaseEnteredSeconds);
        double phaseProgress = Math.Clamp(
            phaseElapsed / RunPacingTimeline.EvaluationWindowSeconds, 0.0, 1.0);
        int requiredDefeats = RunPacingTimeline.GetRequiredDefeats(spawned);
        double dominanceProgress = requiredDefeats <= 0
            ? 0.0
            : Math.Clamp(defeated / (double)requiredDefeats, 0.0, 1.0);
        double transition = _phaseIndex == 0
            ? 1.0
            : Math.Clamp((elapsed - _phaseEnteredSeconds) / TransitionSeconds, 0.0, 1.0);
        double difficultySeconds = Math.Max(0, _phaseIndex - 1) *
            RunPacingTimeline.EvaluationWindowSeconds +
            transition * (_phaseIndex == 0 ? 0.0 : RunPacingTimeline.EvaluationWindowSeconds);
        string nextName = _phaseIndex + 1 < RunPacingTimeline.StructuredPhases.Count
            ? RunPacingTimeline.StructuredPhases[_phaseIndex + 1].DisplayName
            : "异变核心";
        return new RunPacingSnapshot(
            authored.Id, authored.DisplayName, authored.CueText, nextName,
            elapsed, _phaseEnteredSeconds,
            _phaseEnteredSeconds + RunPacingTimeline.EvaluationWindowSeconds,
            _phaseIndex / (double)RunPacingTimeline.AdaptiveRules.Count,
            phaseProgress,
            Math.Max(0.0, RunPacingTimeline.EvaluationWindowSeconds - phaseElapsed),
            false, false, difficultySeconds,
            dominanceProgress,
            RunPacingTimeline.MeetsClearRequirement(spawned, defeated),
            _phaseIndex, spawned, defeated);
    }

    /// <summary>
    /// 记录最新累计遥测，并把三十秒边界之前最后一个样本保存为差分基线；失败后不清空历史。
    /// </summary>
    private void RecordSample(double elapsed)
    {
        _samples.Enqueue((elapsed, _lastSpawnedEnemies, _lastDefeatedEnemies));
        double cutoff = elapsed - RunPacingTimeline.EvaluationWindowSeconds;
        while (_samples.Count > 0 && _samples.Peek().Time <= cutoff)
        {
            _windowBaseline = _samples.Dequeue();
        }
    }

    /// <summary>从仍在滑动窗口内的累计遥测基线计算当前生成数与击破数。</summary>
    private (int Spawned, int Defeated) GetWindowCounts() =>
        (Math.Max(0, _lastSpawnedEnemies - _windowBaseline.Spawned),
            Math.Max(0, _lastDefeatedEnemies - _windowBaseline.Defeated));

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
