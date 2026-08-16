namespace TouhouWuxiaSurvivor.Gameplay.Pacing;

/// <summary>
/// 保存指定时刻的阶段、局内总进度与下一里程碑，供HUD和测试读取而不反向依赖时间轴实现。
/// </summary>
public readonly record struct RunPacingSnapshot(
    RunPhaseId PhaseId,
    string PhaseName,
    string CueText,
    string NextPhaseName,
    double ElapsedSeconds,
    double PhaseStartSeconds,
    double PhaseEndSeconds,
    double TotalProgress,
    double PhaseProgress,
    double SecondsToNextPhase,
    bool IsFinalEncounter,
    bool IsEndless,
    double DifficultySeconds,
    double DominanceProgress,
    bool CanAdvanceByDominance,
    int PressureGear = 0,
    int WindowSpawned = 0,
    int WindowDefeated = 0);
