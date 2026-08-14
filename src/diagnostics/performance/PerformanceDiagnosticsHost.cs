using Godot;
using TouhouWuxiaSurvivor.Settings;

namespace TouhouWuxiaSurvivor.Diagnostics.Performance;

/// <summary>
/// 在诊断构建中跨场景维护低频性能会话，正常发行与编辑器测试不会创建日志或采样。
/// </summary>
public partial class PerformanceDiagnosticsHost : Node
{
    private const ulong SampleIntervalMicroseconds = 1_000_000;
    private static PerformanceDiagnosticsHost? _instance;
    private PerformanceDiagnosticsSessionWriter? _writer;
    private Node? _worldOwner;
    private Func<PerformanceDiagnosticsRuntimeSnapshot>? _worldSnapshotProvider;
    private ulong _sessionStartTicks;
    private ulong _lastSampleTicks;
    private ulong _lastFrameTicks;
    private int _frameCount;
    private double _totalFrameSeconds;
    private double _maximumFrameSeconds;
    private int _hitchOver33Milliseconds;
    private int _hitchOver50Milliseconds;
    private int _hitchOver100Milliseconds;
    private long _sampleIndex;
    private bool _enabled;
    private bool _renderTimingEnabled;
    private Rid _viewportRid;

    public static bool IsActive => _instance?._enabled == true;
    public static string CurrentLogPath => _instance?._writer?.FilePath ?? string.Empty;

    /// <summary>
    /// 在带 diagnostics 特性的导出包或显式诊断参数下创建唯一日志并写入环境头。
    /// </summary>
    public override void _Ready()
    {
        _instance = this;
        ProcessMode = ProcessModeEnum.Always;
        string[] userArguments = OS.GetCmdlineUserArgs();
        _enabled = OS.HasFeature("diagnostics") ||
            userArguments.Contains("--diagnostics", StringComparer.Ordinal);
        SetProcess(_enabled);
        if (!_enabled) return;

        try
        {
            GameSettingsService.Initialize();
            string label = ReadArgument(userArguments, "--diagnostic-label=", "default");
            string outputDirectory = ReadArgument(userArguments, "--diagnostic-output=",
                Path.Combine(OS.GetUserDataDir(), "diagnostics"));
            _writer = PerformanceDiagnosticsSessionWriter.Create(outputDirectory, label);
            GD.Print($"Performance diagnostics: {_writer.FilePath}");
            _writer.Write(PerformanceDiagnosticsEnvironmentFactory.Capture(label));
            _writer.Write(PerformanceDiagnosticsDriverProbe.CreateSkippedRecord());
            _writer.Flush();
            _viewportRid = GetViewport().GetViewportRid();
            RenderingServer.ViewportSetMeasureRenderTime(_viewportRid, true);
            _renderTimingEnabled = true;
            _sessionStartTicks = Time.GetTicksUsec();
            _lastSampleTicks = _sessionStartTicks;
            _lastFrameTicks = _sessionStartTicks;
        }
        catch (Exception exception)
        {
            DisableDiagnostics($"性能诊断初始化失败，游戏将继续运行: {exception.Message}");
        }
    }

    /// <summary>
    /// 记录墙钟帧间隔，并每秒写一次聚合样本；暂停状态下仍运行以捕获渲染或窗口问题。
    /// </summary>
    public override void _Process(double delta)
    {
        if (!_enabled || _writer is null) return;
        try { SampleFrame(); }
        catch (Exception exception)
        {
            DisableDiagnostics($"性能诊断采样失败，本次会话已停止: {exception.Message}");
        }
    }

    /// <summary>
    /// 绑定当前正式世界的聚合快照函数；未启用诊断时立即返回且不保留委托。
    /// </summary>
    public static void AttachWorld(
        Node owner,
        Func<PerformanceDiagnosticsRuntimeSnapshot> snapshotProvider)
    {
        if (_instance is not { _enabled: true }) return;
        _instance._worldOwner = owner;
        _instance._worldSnapshotProvider = snapshotProvider;
    }

    /// <summary>
    /// 仅解除匹配世界的快照委托，防止旧场景退出晚于新场景加载时清空新绑定。
    /// </summary>
    public static void DetachWorld(Node owner)
    {
        if (_instance?._worldOwner != owner) return;
        _instance._worldOwner = null;
        _instance._worldSnapshotProvider = null;
    }

    /// <summary>
    /// 释放会话文件并清空静态入口，确保正常退出前最后几秒样本写入磁盘。
    /// </summary>
    public override void _ExitTree()
    {
        DisableRenderTimingSafely();
        DisposeWriterSafely();
        if (_instance == this) _instance = null;
    }

    /// <summary>
    /// 聚合一帧的墙钟时间，并在满一秒时捕获系统、渲染与正式世界快照。
    /// </summary>
    private void SampleFrame()
    {
        ulong now = Time.GetTicksUsec();
        double frameSeconds = (now - _lastFrameTicks) / 1_000_000.0;
        _lastFrameTicks = now;
        _frameCount++;
        _totalFrameSeconds += frameSeconds;
        _maximumFrameSeconds = Math.Max(_maximumFrameSeconds, frameSeconds);
        if (frameSeconds >= 0.033) _hitchOver33Milliseconds++;
        if (frameSeconds >= 0.050) _hitchOver50Milliseconds++;
        if (frameSeconds >= 0.100) _hitchOver100Milliseconds++;
        ulong elapsedMicroseconds = now - _lastSampleTicks;
        if (elapsedMicroseconds < SampleIntervalMicroseconds) return;

        double windowSeconds = elapsedMicroseconds / 1_000_000.0;
        PerformanceDiagnosticsRuntimeSnapshot runtime = CaptureWorldSnapshot();
        var sample = PerformanceDiagnosticsSampleFactory.Capture(
            ++_sampleIndex,
            (now - _sessionStartTicks) / 1_000_000.0,
            _frameCount,
            windowSeconds,
            _totalFrameSeconds,
            _maximumFrameSeconds,
            _hitchOver33Milliseconds,
            _hitchOver50Milliseconds,
            _hitchOver100Milliseconds,
            _viewportRid,
            runtime);
        if (!TryWrite(sample)) return;
        _lastSampleTicks = now;
        _frameCount = 0;
        _totalFrameSeconds = 0.0;
        _maximumFrameSeconds = 0.0;
        _hitchOver33Milliseconds = 0;
        _hitchOver50Milliseconds = 0;
        _hitchOver100Milliseconds = 0;
    }

    /// <summary>
    /// 安全调用世界快照；场景不存在或提供者抛错时回退为当前场景的零负载记录。
    /// </summary>
    private PerformanceDiagnosticsRuntimeSnapshot CaptureWorldSnapshot()
    {
        if (_worldOwner is not null && GodotObject.IsInstanceValid(_worldOwner) &&
            _worldSnapshotProvider is not null)
        {
            try { return _worldSnapshotProvider(); }
            catch (Exception exception) { GD.PushWarning($"诊断世界快照失败: {exception.Message}"); }
        }

        _worldOwner = null;
        _worldSnapshotProvider = null;
        return PerformanceDiagnosticsRuntimeSnapshot.Empty(
            GetTree().CurrentScene?.Name ?? "scene-transition");
    }

    /// <summary>
    /// 隔离序列化与磁盘错误；失败后关闭本次诊断，而不是让辅助功能中断正式游戏。
    /// </summary>
    private bool TryWrite<T>(T record)
    {
        try
        {
            _writer?.Write(record);
            return _writer is not null;
        }
        catch (Exception exception)
        {
            DisableDiagnostics($"性能诊断写入失败，本次采样已停止: {exception.Message}");
            return false;
        }
    }

    /// <summary>
    /// 原子停用本次诊断并尽力刷新已有数据，任何清理失败都不得传播到游戏循环。
    /// </summary>
    private void DisableDiagnostics(string warning)
    {
        _enabled = false;
        SetProcess(false);
        DisableRenderTimingSafely();
        DisposeWriterSafely();
        GD.PushWarning(warning);
    }

    /// <summary>
    /// 尽力关闭 viewport 帧时测量；独立状态位保证诊断停用后不会把测量开销留到整局结束。
    /// </summary>
    private void DisableRenderTimingSafely()
    {
        if (!_renderTimingEnabled) return;
        try
        {
            if (_viewportRid.IsValid)
                RenderingServer.ViewportSetMeasureRenderTime(_viewportRid, false);
        }
        catch (Exception exception)
        {
            GD.PushWarning($"性能诊断渲染计时关闭失败: {exception.Message}");
        }
        finally { _renderTimingEnabled = false; }
    }

    /// <summary>
    /// 尽力释放日志写入器；磁盘故障只终止诊断，不改变游戏退出码或场景生命周期。
    /// </summary>
    private void DisposeWriterSafely()
    {
        try { _writer?.Dispose(); }
        catch (Exception exception) { GD.PushWarning($"性能诊断日志关闭失败: {exception.Message}"); }
        finally { _writer = null; }
    }

    /// <summary>
    /// 从用户参数读取指定值；未提供时返回回退值，输出路径与 A/B 标签共用同一解析规则。
    /// </summary>
    private static string ReadArgument(
        IEnumerable<string> arguments,
        string prefix,
        string fallback)
    {
        string? value = arguments.FirstOrDefault(argument =>
            argument.StartsWith(prefix, StringComparison.Ordinal));
        return value is null || value.Length == prefix.Length
            ? fallback
            : value[prefix.Length..];
    }
}
