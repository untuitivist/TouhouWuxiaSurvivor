using Godot;
using TouhouWuxiaSurvivor.Gameplay.Pacing;

namespace TouhouWuxiaSurvivor.Ui.Hud;

/// <summary>
/// 绘制固定尺寸的五分钟目标分段进度带，以里程碑刻线表达阶段而不占用额外文字空间。
/// </summary>
public partial class RunPacingBar : Control
{
    private static readonly Color BackgroundColor = new("172019");
    private static readonly Color FillColor = new("a33b36");
    private static readonly Color FinalColor = new("c9a64b");
    private static readonly Color EndlessColor = new("5f9d72");
    private static readonly Color MarkerColor = new("d7d5bd");
    private RunPacingSnapshot _snapshot;

    public double ProgressRatio => _snapshot.TotalProgress;
    public RunPhaseId PhaseId => _snapshot.PhaseId;

    /// <summary>声明该控件只负责绘制且不截获鼠标，避免覆盖层阻断底层玩法输入。</summary>
    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        CustomMinimumSize = new Vector2(146.0f, 8.0f);
    }

    /// <summary>接收不可变阶段快照并请求下一绘制帧更新填充与当前阶段颜色。</summary>
    public void SetSnapshot(RunPacingSnapshot snapshot)
    {
        _snapshot = snapshot;
        QueueRedraw();
    }

    /// <summary>
    /// 绘制底槽、当前进度和四个阶段边界；最终战与无尽分别使用金色和绿色满槽。
    /// </summary>
    public override void _Draw()
    {
        Rect2 track = new(Vector2.Zero, new Vector2(Math.Max(1.0f, Size.X), 8.0f));
        DrawRect(track, BackgroundColor);
        Color fill = _snapshot.IsEndless
            ? EndlessColor
            : _snapshot.IsFinalEncounter ? FinalColor : FillColor;
        float filledWidth = (float)(track.Size.X * Math.Clamp(_snapshot.TotalProgress, 0.0, 1.0));
        if (filledWidth > 0.0f)
        {
            DrawRect(new Rect2(track.Position, new Vector2(filledWidth, track.Size.Y)), fill);
        }

        foreach (double milestone in RunPacingTimeline.MilestoneSeconds)
        {
            float x = (float)(track.Size.X * milestone /
                RunPacingTimeline.FinalEncounterSeconds);
            DrawLine(new Vector2(x, 0.0f), new Vector2(x, track.Size.Y), MarkerColor, 1.0f);
        }

        DrawRect(track, new Color("697064"), false, 1.0f);
    }
}
