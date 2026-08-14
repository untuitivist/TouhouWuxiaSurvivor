using Godot;
using TouhouWuxiaSurvivor.Versioning;

namespace TouhouWuxiaSurvivor.Tests.Integration;

/// <summary>
/// 验证阶段无关的 Windows 诊断构建与正式版本、资源边界和日志收集工具保持一致。
/// </summary>
public partial class BetaDebugExportPolicyTest : Node
{
    private const string PresetPath = "res://export_presets.cfg";
    private const string DebugSection = "preset.1";
    private const string DebugOptions = "preset.1.options";
    private const string ExpectedVersion = "alpha-0.0.3";
    private const string ExpectedWindowsVersion = "0.0.3.0";
    private const string ExpectedArtifact =
        "release/diagnostics/TouhouWuxiaSurvivor_alpha-0.0.3_windows-x86_64-debug.exe";

    /// <summary>
    /// 读取导出与随包脚本契约；任一违约均输出明确原因并以非零状态退出。
    /// </summary>
    public override void _Ready()
    {
        try
        {
            var preset = LoadPreset();
            VerifyVersionIdentity(preset);
            VerifyPresetIdentity(preset);
            VerifyContentBoundary(preset);
            VerifyDebugCapabilities(preset);
            VerifyDiagnosticCompanions();
            GD.Print("Diagnostic export policy test passed.");
            GetTree().Quit();
        }
        catch (Exception exception)
        {
            GD.PushError(exception.ToString());
            GetTree().Quit(1);
        }
    }

    /// <summary>加载唯一导出预设文件，并在解析失败时保留 Godot 错误码。</summary>
    private static ConfigFile LoadPreset()
    {
        var preset = new ConfigFile();
        Error error = preset.Load(PresetPath);
        Require(error == Error.Ok, $"Unable to load export presets: {error}.");
        return preset;
    }

    /// <summary>确认诊断只是构建变体，并与游戏版本及 Windows 数字版本保持一致。</summary>
    private static void VerifyVersionIdentity(ConfigFile preset)
    {
        Require(GameVersion.Current == ExpectedVersion,
            $"Diagnostic variant changed game version: {GameVersion.Current}.");
        Require(Value(preset, DebugOptions, "application/file_version") ==
                ExpectedWindowsVersion &&
            Value(preset, DebugOptions, "application/product_version") ==
                ExpectedWindowsVersion,
            "Diagnostic Windows metadata does not map alpha-0.0.3 to 0.0.3.0.");
    }

    /// <summary>确认正式预设不启用诊断，而专用预设具有稳定身份和阶段无关路径。</summary>
    private static void VerifyPresetIdentity(ConfigFile preset)
    {
        Require(string.IsNullOrEmpty(Value(preset, "preset.0", "custom_features")),
            "Normal Windows Release must never activate performance diagnostics.");
        Require(Value(preset, DebugSection, "name") == "Windows Diagnostics",
            "Dedicated Windows diagnostic preset is missing.");
        Require(Value(preset, DebugSection, "platform") == "Windows Desktop",
            "Diagnostic preset must target Windows Desktop.");
        Require(Value(preset, DebugSection, "custom_features") == "diagnostics",
            "Diagnostic runtime feature tag is missing.");
        Require(Value(preset, DebugSection, "export_path") == ExpectedArtifact,
            "Diagnostic artifact path does not follow the build-variant contract.");
    }

    /// <summary>确认诊断版与正式版使用完全相同的资源包含及排除边界。</summary>
    private static void VerifyContentBoundary(ConfigFile preset)
    {
        Require(Value(preset, DebugSection, "export_filter") == "all_resources",
            "Diagnostic preset must include all permitted resources.");
        Require(Value(preset, DebugSection, "exclude_filter") ==
                Value(preset, "preset.0", "exclude_filter"),
            "Diagnostic and release presets must share the same exclusions.");
        Require(Value(preset, DebugSection, "include_filter") == "CHANGELOG.md" &&
                Value(preset, "preset.0", "include_filter") == "CHANGELOG.md",
            "Both exports must package the in-game changelog source.");
    }

    /// <summary>确认控制台、内嵌 PCK、架构和托管符号满足远端诊断要求。</summary>
    private static void VerifyDebugCapabilities(ConfigFile preset)
    {
        Require(preset.GetValue(DebugOptions, "debug/export_console_wrapper").AsInt64() == 2,
            "Console wrapper must also be enabled for the optimized diagnostic release export.");
        Require(preset.GetValue(DebugOptions, "binary_format/embed_pck").AsBool(),
            "Diagnostic PCK must remain embedded in the main executable.");
        Require(Value(preset, DebugOptions, "binary_format/architecture") == "x86_64",
            "Diagnostic artifact must use the requested x86_64 architecture.");
        Require(preset.GetValue(DebugOptions, "dotnet/include_debug_symbols").AsBool(),
            "Managed debug symbols must be included.");
        Require(preset.GetValue(DebugOptions, "dotnet/embed_build_outputs").AsBool(),
            "Managed build outputs must remain embedded.");
    }

    /// <summary>确认正式入口、双后端启动器、独立日志目录和中文指南形成完整闭环。</summary>
    private static void VerifyDiagnosticCompanions()
    {
        string builder = Read("res://tools/diagnostics/build_diagnostics.cmd");
        string core = Read("res://tools/diagnostics/run_diagnostics_core.cmd");
        string d3d12 = Read("res://tools/diagnostics/run_d3d12_diagnostics.cmd");
        string opengl = Read("res://tools/diagnostics/run_opengl_diagnostics.cmd");
        string guide = Read("res://docs/diagnostics.md");
        Require(builder.Contains("--export-release \"Windows Diagnostics\"", StringComparison.Ordinal),
            "Builder does not use the optimized dedicated diagnostic preset.");
        Require(builder.Contains("Refusing to overwrite", StringComparison.Ordinal),
            "Builder must refuse to overwrite an existing diagnostic package.");
        Require(builder.Contains("release\\diagnostics", StringComparison.Ordinal) &&
                !builder.Contains("beta-debug", StringComparison.Ordinal),
            "Builder does not use the stage-neutral diagnostics directory.");
        Require(core.Contains("session_%PROFILE%_%RANDOM%_", StringComparison.Ordinal),
            "Runtime collector does not isolate diagnostic sessions.");
        Require(core.Contains("--verbose --print-fps --log-file", StringComparison.Ordinal),
            "Runtime collector does not request persistent engine diagnostics.");
        Require(core.Contains("--diagnostic-output=%SESSION_DIR%", StringComparison.Ordinal) &&
                core.Contains("--diagnostic-label=%PROFILE%", StringComparison.Ordinal),
            "Structured runtime diagnostics are not written into the isolated session.");
        Require(d3d12.Contains(" d3d12", StringComparison.Ordinal) &&
                opengl.Contains(" opengl", StringComparison.Ordinal),
            "Both renderer comparison entry points are required.");
        Require(guide.Contains("回传内容", StringComparison.Ordinal) &&
                guide.Contains("OpenGL", StringComparison.Ordinal),
            "Chinese diagnostic guide is incomplete.");
    }

    /// <summary>读取必须存在的 UTF-8 文本资源，并拒绝缺失或空白的随包文件。</summary>
    private static string Read(string path)
    {
        Require(Godot.FileAccess.FileExists(path), $"Diagnostic companion is missing: {path}.");
        string content = Godot.FileAccess.GetFileAsString(path);
        Require(!string.IsNullOrWhiteSpace(content), $"Diagnostic companion is empty: {path}.");
        return content;
    }

    /// <summary>把配置值统一转换为字符串，避免重复的 Variant 转换掩盖策略意图。</summary>
    private static string Value(ConfigFile preset, string section, string key) =>
        preset.GetValue(section, key).AsString();

    /// <summary>将诊断策略违约转换为包含具体原因的测试异常。</summary>
    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
