using Godot;
using TouhouWuxiaSurvivor.Gameplay.Pacing;

namespace TouhouWuxiaSurvivor.Ui.Hud;

/// <summary>
/// 绘制固定尺寸的五分钟目标分段进度带，以里程碑刻线表达阶段而不占用额外文字空间。
/// </summary>
public partial class RunPacingBar : Control
{
    private static readonly Color BackgroundColor = new("08100b");
    private static readonly Color TrackRim = new("6c5730");
    private static readonly Color FillColor = new("b2352e");
    private static readonly Color FinalColor = new("b99445");
    private static readonly Color EndlessColor = new("49865f");
    private static readonly Color MarkerColor = new("ded0a2");
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
        float width = Math.Max(1.0f, Size.X);
        Rect2 track = new(Vector2.Zero, new Vector2(width, 8.0f));
        Rect2 channel = new(new Vector2(2, 2), new Vector2(Math.Max(1.0f, width - 4), 4));
        DrawRect(track, new Color(0.01f, 0.02f, 0.014f, 0.92f));
        DrawRect(new Rect2(1, 1, Math.Max(1.0f, width - 2), 6), TrackRim);
        DrawRect(channel, BackgroundColor);
        Color fill = _snapshot.IsEndless
            ? EndlessColor
            : _snapshot.IsFinalEncounter ? FinalColor : FillColor;
        float filledWidth = (float)(channel.Size.X *
            Math.Clamp(_snapshot.TotalProgress, 0.0, 1.0));
        if (filledWidth > 0.0f)
        {
            DrawRect(new Rect2(channel.Position, new Vector2(filledWidth, channel.Size.Y)), fill);
            for (float x = channel.Position.X + 3; x < channel.Position.X + filledWidth; x += 7)
            {
                DrawRect(new Rect2(x, 3, 2, 1), fill.Lightened(0.26f));
                DrawRect(new Rect2(x + 2, 5, 2, 1), fill.Darkened(0.22f));
            }
        }

        foreach (double milestone in RunPacingTimeline.MilestoneSeconds)
        {
            float x = channel.Position.X + (float)(channel.Size.X * milestone /
                RunPacingTimeline.FinalEncounterSeconds);
            DrawLine(new Vector2(x, 1.0f), new Vector2(x, 7.0f), MarkerColor, 1.0f);
        }

        DrawRect(new Rect2(0, 0, 2, 2), TrackRim);
        DrawRect(new Rect2(width - 2, 6, 2, 2), TrackRim);
    }
}
