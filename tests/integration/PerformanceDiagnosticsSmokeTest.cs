using System.Text.Json;
using Godot;
using TouhouWuxiaSurvivor.Diagnostics.Performance;

namespace TouhouWuxiaSurvivor.Tests.Integration;

/// <summary>
/// 验证诊断生命周期、聚合计算和 JSONL 字段契约，不访问真实用户存档或创建持久文件。
/// </summary>
public partial class PerformanceDiagnosticsSmokeTest : Node
{
    /// <summary>
    /// 依次检查普通测试环境禁用、Autoload 接线、样本计算和序列化格式后退出。
    /// </summary>
    public override void _Ready()
    {
        try
        {
            VerifyLifecycle();
            PerformanceDiagnosticsSample sample = CreateSample();
            VerifySample(sample);
            VerifyJsonLine(sample);
            VerifyBufferedUtf8Writer(sample);
            GD.Print("Performance diagnostics smoke test passed.");
            GetTree().Quit();
        }
        catch (Exception exception)
        {
            GD.PushError(exception.ToString());
            GetTree().Quit(1);
        }
    }

    /// <summary>
    /// 确认普通编辑器测试不会意外写日志，同时项目已登记唯一常驻诊断场景。
    /// </summary>
    private static void VerifyLifecycle()
    {
        string[] arguments = OS.GetCmdlineUserArgs();
        bool requested = arguments.Contains("--diagnostics", StringComparer.Ordinal);
        Require(PerformanceDiagnosticsHost.IsActive == requested,
            "Diagnostics lifecycle does not match the explicit user argument.");
        string autoload = ProjectSettings.GetSetting(
            "autoload/PerformanceDiagnostics").AsString();
        Require(autoload ==
            "*res://src/diagnostics/performance/PerformanceDiagnosticsHost.tscn",
            "Performance diagnostics autoload is missing or points to another scene.");

        string? output = arguments.FirstOrDefault(argument =>
            argument.StartsWith("--diagnostic-output=", StringComparison.Ordinal));
        if (!requested || output is null) return;
        string expected = Path.GetFullPath(output["--diagnostic-output=".Length..]);
        string actual = Path.GetFullPath(Path.GetDirectoryName(
            PerformanceDiagnosticsHost.CurrentLogPath) ?? string.Empty);
        Require(string.Equals(expected, actual, StringComparison.OrdinalIgnoreCase),
            "Explicit diagnostic output directory was ignored.");
    }

    /// <summary>
    /// 创建带已知局内负载的一秒样本，使派生玩家弹数和碰撞候选量可被精确断言。
    /// </summary>
    private PerformanceDiagnosticsSample CreateSample()
    {
        var runtime = new PerformanceDiagnosticsRuntimeSnapshot(
            "WorldDemo", "幻想乡本体", "character_base_00", "博丽灵梦",
            120.0, 16.0f, -8.0f, 25, 3, 40, 44, 1, 99,
            200, 50, 2000, 6, 120, 12, 41, 0);
        Rid viewportRid = GetViewport().GetViewportRid();
        RenderingServer.ViewportSetMeasureRenderTime(viewportRid, true);
        return PerformanceDiagnosticsSampleFactory.Capture(
            1, 1.0, 60, 1.0, 1.0, 0.12,
            2, 1, 1, viewportRid, runtime);
    }

    /// <summary>
    /// 验证玩家与敌方弹幕拆分、理论碰撞规模、卡顿计数和世界字段没有在转换中丢失。
    /// </summary>
    private static void VerifySample(PerformanceDiagnosticsSample sample)
    {
        Require(Math.Abs(sample.ObservedFps - 60.0) < 0.001,
            "Observed FPS does not use the wall-clock frame window.");
        Require(sample.PlayerProjectiles == 150 && sample.EnemyProjectiles == 50 &&
            sample.TotalProjectiles == 200,
            "Projectile factions were not split from the aggregate pool.");
        Require(sample.EnemyPoolCount == 44 &&
            sample.PotentialPlayerCollisionChecks == 6600,
            "Potential O(P*E) collision pressure is incorrect.");
        Require(sample.HitchOver33Milliseconds == 2 &&
            sample.HitchOver50Milliseconds == 1 &&
            sample.HitchOver100Milliseconds == 1,
            "Frame hitch bands were not preserved.");
        Require(sample.ActiveChunks == 25 && sample.PendingChunks == 3 &&
            sample.AliveEnemies == 40 && sample.AliveBosses == 1,
            "World aggregate fields were not preserved.");
    }

    /// <summary>
    /// 确认单条记录是不含换行的合法 JSON，且关键字段使用稳定 camelCase 名称。
    /// </summary>
    private static void VerifyJsonLine(PerformanceDiagnosticsSample sample)
    {
        string json = PerformanceDiagnosticsSessionWriter.SerializeRecord(sample);
        Require(!json.Contains('\n') && !json.Contains('\r'),
            "A JSONL record contains an embedded line break.");
        using JsonDocument document = JsonDocument.Parse(json);
        JsonElement root = document.RootElement;
        Require(root.GetProperty("type").GetString() == "sample" &&
            root.GetProperty("observedFps").GetDouble() == 60.0 &&
            root.GetProperty("potentialPlayerCollisionChecks").GetInt64() == 6600,
            "Diagnostic JSON field names or values changed unexpectedly.");
    }

    /// <summary>
    /// 经内存流执行正式缓冲写入和释放，验证输出无 BOM、是一行 JSON 且退出时确实刷新。
    /// </summary>
    private static void VerifyBufferedUtf8Writer(PerformanceDiagnosticsSample sample)
    {
        using var stream = new MemoryStream();
        using (var writer = PerformanceDiagnosticsSessionWriter.CreateForTesting(stream))
        {
            writer.Write(sample);
        }

        byte[] bytes = stream.ToArray();
        Require(bytes.Length > 1 && !(bytes[0] == 0xEF && bytes[1] == 0xBB),
            "Diagnostic writer emitted a UTF-8 BOM.");
        string text = System.Text.Encoding.UTF8.GetString(bytes);
        Require(text.Count(character => character == '\n') == 1,
            "Buffered writer did not flush exactly one JSONL record on dispose.");
        using JsonDocument document = JsonDocument.Parse(text);
        Require(document.RootElement.GetProperty("type").GetString() == "sample",
            "Buffered writer changed the record payload.");
    }

    /// <summary>
    /// 将任一诊断契约失败转换为带明确原因的测试异常。
    /// </summary>
    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
