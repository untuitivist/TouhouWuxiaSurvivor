namespace TouhouWuxiaSurvivor.Diagnostics.Performance;

/// <summary>
/// 保存一秒窗口内的帧节奏、Godot 监视器和局内聚合负载，用于判断 CPU、GPU 或实体规模瓶颈。
/// </summary>
public sealed class PerformanceDiagnosticsSample
{
    public string Type { get; init; } = "sample";
    public string TimestampUtc { get; init; } = string.Empty;
    public long SampleIndex { get; init; }
    public double SessionSeconds { get; init; }
    public double ObservedFps { get; init; }
    public double GodotFps { get; init; }
    public double AverageFrameMilliseconds { get; init; }
    public double MaximumFrameMilliseconds { get; init; }
    public double ProcessMilliseconds { get; init; }
    public double PhysicsMilliseconds { get; init; }
    public double RenderCpuMilliseconds { get; init; }
    public double RenderGpuMilliseconds { get; init; }
    public double RenderSetupCpuMilliseconds { get; init; }
    public int HitchOver33Milliseconds { get; init; }
    public int HitchOver50Milliseconds { get; init; }
    public int HitchOver100Milliseconds { get; init; }
    public long ObjectCount { get; init; }
    public long NodeCount { get; init; }
    public long OrphanNodeCount { get; init; }
    public long ResourceCount { get; init; }
    public long DrawCalls { get; init; }
    public long RenderedObjects { get; init; }
    public long RenderedPrimitives { get; init; }
    public double VideoMemoryMegabytes { get; init; }
    public double TextureMemoryMegabytes { get; init; }
    public double BufferMemoryMegabytes { get; init; }
    public long Physics2DActiveObjects { get; init; }
    public long Physics2DCollisionPairs { get; init; }
    public long CanvasPipelineCompilations { get; init; }
    public double ManagedHeapMegabytes { get; init; }
    public int Gen0Collections { get; init; }
    public int Gen1Collections { get; init; }
    public int Gen2Collections { get; init; }
    public double ProcessWorkingSetMegabytes { get; init; }
    public double ProcessPrivateMemoryMegabytes { get; init; }
    public double ProcessTotalCpuSeconds { get; init; }
    public int RuntimeMaxFps { get; init; }
    public string RuntimeVsyncMode { get; init; } = string.Empty;
    public bool TreePaused { get; init; }
    public string ActiveModal { get; init; } = string.Empty;
    public string SceneName { get; init; } = string.Empty;
    public string ActiveContent { get; init; } = string.Empty;
    public string CharacterId { get; init; } = string.Empty;
    public string CharacterName { get; init; } = string.Empty;
    public double RunSeconds { get; init; }
    public float PlayerX { get; init; }
    public float PlayerY { get; init; }
    public int ActiveChunks { get; init; }
    public int PendingChunks { get; init; }
    public int AliveEnemies { get; init; }
    public int EnemyPoolCount { get; init; }
    public int AliveBosses { get; init; }
    public int DefeatedEnemies { get; init; }
    public int PlayerProjectiles { get; init; }
    public int EnemyProjectiles { get; init; }
    public int TotalProjectiles { get; init; }
    public int ProjectileCapacity { get; init; }
    public long PotentialPlayerCollisionChecks { get; init; }
    public long ActualPlayerCollisionChecks { get; init; }
    public int Pickups { get; init; }
    public int Spirits { get; init; }
    public int Level { get; init; }
    public int MappedVisuals { get; init; }
    public int FallbackVisuals { get; init; }
}
