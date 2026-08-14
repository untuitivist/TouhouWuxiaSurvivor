namespace TouhouWuxiaSurvivor.Gameplay.Pacing;

/// <summary>
/// 保存一个有限阶段的起止时间、显示名称和切换提示，使玩法与界面共享同一份策划数据。
/// </summary>
public sealed class RunPhaseDefinition
{
    public RunPhaseId Id { get; }
    public string DisplayName { get; }
    public string CueText { get; }
    public double StartSeconds { get; }
    public double EndSeconds { get; }

    /// <summary>
    /// 建立经过完整校验的阶段；非法时间或空文案直接失败，避免时间轴静默产生重叠区间。
    /// </summary>
    public RunPhaseDefinition(
        RunPhaseId id,
        string displayName,
        string cueText,
        double startSeconds,
        double endSeconds)
    {
        if (string.IsNullOrWhiteSpace(displayName) || string.IsNullOrWhiteSpace(cueText))
        {
            throw new ArgumentException("Run phase text cannot be empty.");
        }

        if (!double.IsFinite(startSeconds) || !double.IsFinite(endSeconds) ||
            startSeconds < 0.0 || endSeconds <= startSeconds)
        {
            throw new ArgumentOutOfRangeException(nameof(endSeconds),
                "Run phase boundaries must form a finite positive interval.");
        }

        Id = id;
        DisplayName = displayName;
        CueText = cueText;
        StartSeconds = startSeconds;
        EndSeconds = endSeconds;
    }
}
