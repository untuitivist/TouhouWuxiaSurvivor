using Godot;
using System.Diagnostics;

namespace TouhouWuxiaSurvivor.Diagnostics.Performance;

/// <summary>
/// 把一秒墙钟帧窗口、Godot 内置监视器和世界聚合快照转换为稳定的日志记录。
/// </summary>
public static class PerformanceDiagnosticsSampleFactory
{
    private const double BytesPerMegabyte = 1024.0 * 1024.0;

    /// <summary>
    /// 创建一次性能样本；所有监视器只查询一次，避免诊断开销随实体数量增长。
    /// </summary>
    public static PerformanceDiagnosticsSample Capture(
        long sampleIndex,
        double sessionSeconds,
        int frameCount,
        double windowSeconds,
        double totalFrameSeconds,
        double maximumFrameSeconds,
        int hitchOver33Milliseconds,
        int hitchOver50Milliseconds,
        int hitchOver100Milliseconds,
        Rid viewportRid,
        PerformanceDiagnosticsRuntimeSnapshot runtime)
    {
        double safeWindow = Math.Max(windowSeconds, 0.000001);
        int playerProjectiles = Math.Max(0, runtime.Projectiles - runtime.EnemyProjectiles);
        using Process process = Process.GetCurrentProcess();
        return new PerformanceDiagnosticsSample
        {
            TimestampUtc = DateTimeOffset.UtcNow.ToString("O"),
            SampleIndex = sampleIndex,
            SessionSeconds = sessionSeconds,
            ObservedFps = frameCount / safeWindow,
            GodotFps = Monitor(Godot.Performance.Monitor.TimeFps),
            AverageFrameMilliseconds = frameCount == 0
                ? 0.0 : totalFrameSeconds * 1000.0 / frameCount,
            MaximumFrameMilliseconds = maximumFrameSeconds * 1000.0,
            ProcessMilliseconds = Monitor(Godot.Performance.Monitor.TimeProcess) * 1000.0,
            PhysicsMilliseconds = Monitor(Godot.Performance.Monitor.TimePhysicsProcess) * 1000.0,
            RenderCpuMilliseconds = Finite(
                RenderingServer.ViewportGetMeasuredRenderTimeCpu(viewportRid)),
            RenderGpuMilliseconds = Finite(
                RenderingServer.ViewportGetMeasuredRenderTimeGpu(viewportRid)),
            RenderSetupCpuMilliseconds = Finite(RenderingServer.GetFrameSetupTimeCpu()),
            HitchOver33Milliseconds = hitchOver33Milliseconds,
            HitchOver50Milliseconds = hitchOver50Milliseconds,
            HitchOver100Milliseconds = hitchOver100Milliseconds,
            ObjectCount = Quantity(Godot.Performance.Monitor.ObjectCount),
            NodeCount = Quantity(Godot.Performance.Monitor.ObjectNodeCount),
            OrphanNodeCount = Quantity(Godot.Performance.Monitor.ObjectOrphanNodeCount),
            ResourceCount = Quantity(Godot.Performance.Monitor.ObjectResourceCount),
            DrawCalls = Quantity(Godot.Performance.Monitor.RenderTotalDrawCallsInFrame),
            RenderedObjects = Quantity(Godot.Performance.Monitor.RenderTotalObjectsInFrame),
            RenderedPrimitives = Quantity(Godot.Performance.Monitor.RenderTotalPrimitivesInFrame),
            VideoMemoryMegabytes = Monitor(Godot.Performance.Monitor.RenderVideoMemUsed) / BytesPerMegabyte,
            TextureMemoryMegabytes = Monitor(Godot.Performance.Monitor.RenderTextureMemUsed) / BytesPerMegabyte,
            BufferMemoryMegabytes = Monitor(Godot.Performance.Monitor.RenderBufferMemUsed) / BytesPerMegabyte,
            Physics2DActiveObjects = Quantity(Godot.Performance.Monitor.Physics2DActiveObjects),
            Physics2DCollisionPairs = Quantity(Godot.Performance.Monitor.Physics2DCollisionPairs),
            CanvasPipelineCompilations = Quantity(Godot.Performance.Monitor.PipelineCompilationsCanvas),
            ManagedHeapMegabytes = GC.GetTotalMemory(false) / BytesPerMegabyte,
            Gen0Collections = GC.CollectionCount(0),
            Gen1Collections = GC.CollectionCount(1),
            Gen2Collections = GC.CollectionCount(2),
            ProcessWorkingSetMegabytes = process.WorkingSet64 / BytesPerMegabyte,
            ProcessPrivateMemoryMegabytes = process.PrivateMemorySize64 / BytesPerMegabyte,
            ProcessTotalCpuSeconds = process.TotalProcessorTime.TotalSeconds,
            RuntimeMaxFps = Engine.MaxFps,
            RuntimeVsyncMode = DisplayServer.GetName() == "headless"
                ? "headless"
                : DisplayServer.WindowGetVsyncMode().ToString(),
            TreePaused = runtime.TreePaused,
            ActiveModal = runtime.ActiveModal,
            SceneName = runtime.SceneName,
            ActiveContent = runtime.ActiveContent,
            CharacterId = runtime.CharacterId,
            CharacterName = runtime.CharacterName,
            RunSeconds = runtime.RunSeconds,
            PlayerX = runtime.PlayerX,
            PlayerY = runtime.PlayerY,
            ActiveChunks = runtime.ActiveChunks,
            PendingChunks = runtime.PendingChunks,
            AliveEnemies = runtime.AliveEnemies,
            EnemyPoolCount = runtime.EnemyPoolCount,
            AliveBosses = runtime.AliveBosses,
            DefeatedEnemies = runtime.DefeatedEnemies,
            PlayerProjectiles = playerProjectiles,
            EnemyProjectiles = runtime.EnemyProjectiles,
            TotalProjectiles = runtime.Projectiles,
            ProjectileCapacity = runtime.ProjectileCapacity,
            PotentialPlayerCollisionChecks = (long)playerProjectiles * runtime.EnemyPoolCount,
            ActualPlayerCollisionChecks = runtime.PlayerCollisionChecks,
            Pickups = runtime.Pickups,
            Spirits = runtime.Spirits,
            Level = runtime.Level,
            MappedVisuals = runtime.MappedVisuals,
            FallbackVisuals = runtime.FallbackVisuals,
        };
    }

    /// <summary>
    /// 读取一个 Godot 监视器的双精度值，为主构造流程提供统一入口。
    /// </summary>
    private static double Monitor(Godot.Performance.Monitor monitor)
    {
        double value = Godot.Performance.GetMonitor(monitor);
        return double.IsFinite(value) ? value : 0.0;
    }

    /// <summary>
    /// 把渲染服务器在无头或设备重建期间可能返回的非有限值归零，保持 JSONL 始终可序列化。
    /// </summary>
    private static double Finite(double value) => double.IsFinite(value) ? value : 0.0;

    /// <summary>
    /// 把整数语义监视器四舍五入为长整数，避免 JSON 出现没有意义的小数部分。
    /// </summary>
    private static long Quantity(Godot.Performance.Monitor monitor) =>
        (long)Math.Round(Monitor(monitor));
}
