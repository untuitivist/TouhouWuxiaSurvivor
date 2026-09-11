using Godot;
using Rebirth.Core;

namespace Rebirth.Presentation;

public partial class DebugOverlay : Control
{
    public GameCanvas? Canvas;
    public Font BodyFont = null!;
    public RunState? Run;
    public string ScreenName = "title";
    public VideoPreferences Video = new();
    public int RefreshCount { get; private set; }
    public string Summary => string.Join("\n", left.Concat(right));
    private string[] left = [];
    private string[] right = [];
    private readonly float[] frameTimes = new float[120];
    private int frameIndex;
    private int frameCount;
    private double refreshDelay;

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        TextureFilter = TextureFilterEnum.Nearest;
        Size = new(1280, 720);
        Visible = false;
    }

    public void Toggle()
    {
        Visible = !Visible;
        if (Visible) { refreshDelay = 0; frameIndex = 0; frameCount = 0; RefreshSnapshot(); QueueRedraw(); }
    }

    public override void _Process(double delta)
    {
        if (!Visible) return;
        frameTimes[frameIndex] = (float)delta * 1000;
        frameIndex = (frameIndex + 1) % frameTimes.Length;
        frameCount = Math.Min(frameCount + 1, frameTimes.Length);
        refreshDelay += delta;
        if (refreshDelay >= 0.25) { refreshDelay = 0; RefreshSnapshot(); }
        QueueRedraw();
    }

    private void RefreshSnapshot()
    {
        RefreshCount++;
        var version = ProjectSettings.GetSetting("application/config/version").AsString();
        var location = Run == null ? GameText.Get("XY: -- (尚未开始)") : GameText.Format($"XY: {Run.PlayerPosition.X:0.00} / {Run.PlayerPosition.Y:0.00}   (2D 世界单位)");
        var hostile = Run?.Projectiles.Count(projectile => projectile.Hostile) ?? 0;
        var regular = Run?.Enemies.Count(enemy => enemy.Kind != EnemyKind.Boss) ?? 0;
        var bossCount = Run?.Enemies.Count(enemy => enemy.Kind == EnemyKind.Boss) ?? 0;
        var monitor = DisplayServer.GetName() == "headless" ? -1 : DisplayServer.WindowGetCurrentScreen();
        left =
        [
            $"NIGHT JOURNAL  {version} / Godot {Engine.GetVersionInfo()["string"]}",
            GameText.Format($"FPS {Engine.GetFramesPerSecond():0}   主循环 {Performance.GetMonitor(Performance.Monitor.TimeProcess) * 1000:0.00} ms   物理 {Performance.GetMonitor(Performance.Monitor.TimePhysicsProcess) * 1000:0.00} ms"),
            GameText.Format($"场景 {ScreenName} / {(Run == null ? GameText.Get("菜单") : Run.Phase.ToString())}   模拟目标 {Engine.PhysicsTicksPerSecond} Hz"),
            location,
            Run == null ? "Seed: --   Tick: --" : $"Seed: {Run.Seed}   Tick: {Run.Ticks}   Time: {Run.Time:0.00}s",
            Run == null ? GameText.Get("区域: --") : GameText.Format($"区域: 博丽夜境   边界 ±{RunState.ArenaHalfWidth:0} / ±{RunState.ArenaHalfHeight:0}"),
            GameText.Format($"妖怪 {regular} / Boss {bossCount}   实体上限 {RunState.EnemyLimit}"),
            GameText.Format($"玩家弹 {Math.Max(0, (Run?.Projectiles.Count ?? 0) + (Run?.Stars.Count ?? 0) - hostile)} / 敌弹 {hostile}   掉落 {Run?.Pickups.Count ?? 0}"),
            Run == null ? GameText.Get("角色: --") : GameText.Format($"角色 {Run.Hero}  HP {Run.Health:0.0}/{Run.MaxHealth:0}   速度 {Run.PlayerVelocity.Length():0.0}"),
            Run == null ? GameText.Get("战况: --") : GameText.Format($"退治 {Run.Kills}   擦弹 {Run.Grazes}   古印 {Run.PurifiedSeals}/3   待选 {Run.PendingChoices}"),
            GameText.Format($"[{GameControls.Hint(GameControls.Debug)}] 隐藏诊断 · 只读，不暂停游戏")
        ];
        right =
        [
            GameText.Format($"显示驱动: {DisplayServer.GetName()} / {RenderingServer.GetCurrentRenderingMethod()}"),
            $"GPU: {RenderingServer.GetVideoAdapterName()}",
            GameText.Format($"物理窗口: {DisplayServer.WindowGetSize()}   逻辑: 1280 × 720"),
            GameText.Format($"显示器: {(monitor < 0 ? "--" : monitor.ToString())}   模式: {DisplayServer.WindowGetMode()}"),
            GameText.Format($"VSync: {DisplayServer.WindowGetVsyncMode()}   FPS 上限: {(Video.MaxFps == 0 ? GameText.Get("无限制") : Video.MaxFps.ToString())}"),
            $"Draw calls: {Performance.GetMonitor(Performance.Monitor.RenderTotalDrawCallsInFrame):0}   Objects: {Performance.GetMonitor(Performance.Monitor.ObjectCount):0}",
            GameText.Format($"Godot 静态内存: {(OS.IsDebugBuild() ? $"{Performance.GetMonitor(Performance.Monitor.MemoryStatic) / 1048576:0.0} MiB" : GameText.Get("N/A（发行引擎不提供）"))}"),
            GameText.Format($".NET 托管堆: {GC.GetTotalMemory(false) / 1048576.0:0.0} MiB"),
            $"Batch: {Canvas?.VisibleBatchInstances ?? 0}   Upload CPU: {Canvas?.BatchBuildMilliseconds ?? 0:0.00} ms   Target: {GetViewport().GetTexture().GetSize()}",
            GameText.Get("内存值不是进程总占用；主循环耗时不是 GPU 耗时。")
        ];
    }

    public override void _Draw()
    {
        if (BodyFont == null) return;
        DrawColumn(left, 12, 616);
        DrawColumn(right, 652, 616);
        var graph = new Rect2(12, 282, 360, 92);
        DrawRect(graph, new Color(0.05f, 0.05f, 0.055f, 0.9f));
        DrawString(BodyFont, new(20, 299), GameText.Get("真实帧间隔 / 最近 120 帧 · 顶线 50 ms"), fontSize: 13, modulate: Palette.Paper);
        var baseline = 360f;
        DrawLine(new(20, baseline - 17), new(364, baseline - 17), Palette.Alpha(Palette.Gold, 0.7f), 1);
        for (var index = 0; index < frameCount; index++)
        {
            var sample = frameTimes[(frameIndex - frameCount + index + frameTimes.Length) % frameTimes.Length];
            var height = Math.Min(50, sample);
            DrawRect(new(20 + index * 2.85f, baseline - height, 2, height), sample > 33.34f ? Palette.Red : Palette.Jade);
        }
    }

    private void DrawColumn(string[] lines, int horizontal, int width)
    {
        for (var index = 0; index < lines.Length; index++)
        {
            var text = lines[index];
            var size = 14;
            while (size > 10 && BodyFont.GetStringSize(text, fontSize: size).X > width - 10) size--;
            var measured = Math.Min(width, BodyFont.GetStringSize(text, fontSize: size).X + 10);
            DrawRect(new(horizontal, 10 + index * 23, measured, 22), new Color(0.05f, 0.05f, 0.055f, 0.88f));
            DrawString(BodyFont, new(horizontal + 4, 27 + index * 23), text, width: width - 8, fontSize: size, modulate: Palette.Paper);
        }
    }
}
