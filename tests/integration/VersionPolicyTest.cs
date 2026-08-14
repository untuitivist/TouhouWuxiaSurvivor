using Godot;
using TouhouWuxiaSurvivor.Versioning;

namespace TouhouWuxiaSurvivor.Tests.Integration;

/// <summary>
/// 验证项目版本的唯一运行时来源、严格格式、当前值以及文档版本标题保持一致。
/// </summary>
public partial class VersionPolicyTest : Node
{
    private const string ExpectedVersion = "alpha-0.0.4";
    private const string ExpectedWindowsVersion = "0.0.4.0";

    /// <summary>
    /// 依次执行版本来源、格式样例和文档镜像检查；任一策略违约都以非零状态退出。
    /// </summary>
    public override void _Ready()
    {
        try
        {
            VerifyRuntimeSource();
            VerifyFormatPolicy();
            VerifyParsedFields();
            VerifyChangelogVersion();
            VerifyReadmeVersion();
            VerifyExportVersion();
            GD.Print("Version policy test passed.");
            GetTree().Quit();
        }
        catch (Exception exception)
        {
            GD.PushError(exception.ToString());
            GetTree().Quit(1);
        }
    }

    /// <summary>
    /// 确认版本设置存在、读取器直接反映原始设置，并锁定当前开发版本为 alpha-0.0.4。
    /// </summary>
    private static void VerifyRuntimeSource()
    {
        Require(ProjectSettings.HasSetting(GameVersion.ProjectSettingPath),
            $"Missing project setting: {GameVersion.ProjectSettingPath}.");
        string configured = ProjectSettings.GetSetting(GameVersion.ProjectSettingPath).AsString();
        Require(GameVersion.Current == configured,
            "GameVersion does not reflect the project version setting.");
        Require(configured == ExpectedVersion,
            $"Current project version must be {ExpectedVersion}, found {configured}.");
    }

    /// <summary>
    /// 用允许与拒绝样例覆盖阶段首段、数字段、前导零、大小写、旧格式和溢出边界。
    /// </summary>
    private static void VerifyFormatPolicy()
    {
        string[] valid =
        [
            "alpha-0.0.0",
            "beta-1.2.3",
            "rc-3.4.5",
            "stable-4.0.12",
        ];
        string?[] invalid =
        [
            null,
            "",
            "0.0.0-alpha",
            "alpha-0-0-2",
            "alpha-00.0.0",
            "dev-0.0.0",
            "gamma-0.0.2",
            "delta-0.0.0",
            "Alpha-0.0.0",
            "alpha-0.0.0.1",
            "alpha-2147483648.0.0",
            " alpha-0.0.0",
        ];
        Require(valid.All(GameVersion.IsValidFormat),
            "Version validator rejected an allowed format sample.");
        Require(invalid.All(value => !GameVersion.IsValidFormat(value)),
            "Version validator accepted a forbidden format sample.");
    }

    /// <summary>
    /// 将当前版本解析为类型化字段，确认四段语义、规范重组和 Windows 数字映射均来自同一对象。
    /// </summary>
    private static void VerifyParsedFields()
    {
        GameVersionDescriptor descriptor = GameVersion.Parse(ExpectedVersion);
        Require(descriptor.Stage == GameReleaseStage.Alpha &&
            descriptor.Major == 0 && descriptor.Release == 0 &&
            descriptor.Optimization == 4,
            "Version parser assigned a stage or numeric field to the wrong dimension.");
        Require(descriptor.ToString() == ExpectedVersion &&
            descriptor.ToWindowsNumericVersion() == ExpectedWindowsVersion,
            "Version descriptor did not produce canonical game and Windows versions.");
    }

    /// <summary>
    /// 读取版本日志的首个二级标题，并确认它精确等于项目设置中的当前版本。
    /// </summary>
    private static void VerifyChangelogVersion()
    {
        string changelog = Godot.FileAccess.GetFileAsString("res://CHANGELOG.md");
        string? latestTitle = changelog
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault(line => line.StartsWith("## ", StringComparison.Ordinal));
        Require(latestTitle == $"## {GameVersion.Current}",
            $"Latest changelog title does not match {GameVersion.Current}: {latestTitle}.");
        string[] migratedHistory =
            ["alpha-0.0.0", "alpha-0.0.1", "alpha-0.0.2", "alpha-0.0.3", "alpha-0.0.4"];
        Require(migratedHistory.All(version => changelog.Contains(
                $"## {version}", StringComparison.Ordinal)),
            "Changelog does not preserve the complete stage-first version history.");
        string[] legacyHistory = ["0.0.0-alpha", "0.0.0-beta", "0.0.0-gamma"];
        Require(legacyHistory.All(version => !changelog.Contains(
                $"## {version}", StringComparison.Ordinal)),
            "Changelog still exposes a legacy numeric-first version title.");
    }

    /// <summary>
    /// 确认 README 展示当前版本并明确标注唯一设置键，防止文档引入未受约束的版本来源。
    /// </summary>
    private static void VerifyReadmeVersion()
    {
        string readme = Godot.FileAccess.GetFileAsString("res://README.md");
        Require(readme.Contains($"当前版本：`{GameVersion.Current}`", StringComparison.Ordinal),
            "README does not display the configured project version.");
        Require(readme.Contains($"`{GameVersion.ProjectSettingPath}`", StringComparison.Ordinal),
            "README does not identify the unique runtime version setting.");
    }

    /// <summary>
    /// 确认导出文件名完整保留阶段首段，并把三个数字维度稳定映射到 Windows 四段元数据。
    /// </summary>
    private static void VerifyExportVersion()
    {
        string preset = Godot.FileAccess.GetFileAsString("res://export_presets.cfg");
        string version = GameVersion.Current;
        string numericVersion = GameVersion.CurrentDescriptor.ToWindowsNumericVersion();
        Require(preset.Contains(
                $"export_path=\"release/TouhouWuxiaSurvivor_{version}.exe\"",
                StringComparison.Ordinal),
            "Export filename does not mirror the configured game version.");
        Require(preset.Contains(
                $"export_path=\"release/diagnostics/TouhouWuxiaSurvivor_{version}" +
                "_windows-x86_64-debug.exe\"",
                StringComparison.Ordinal),
            "Diagnostic export filename does not mirror the configured game version.");
        Require(preset.Contains(
                $"application/file_version=\"{numericVersion}\"", StringComparison.Ordinal),
            "Windows file version does not mirror the game version numeric fields.");
        Require(preset.Contains(
                $"application/product_version=\"{numericVersion}\"", StringComparison.Ordinal),
            "Windows product version does not mirror the game version numeric fields.");
    }

    /// <summary>
    /// 将版本策略违约转换为包含具体原因的测试异常，供无头测试返回明确失败状态。
    /// </summary>
    /// <param name="condition">策略检查是否通过。</param>
    /// <param name="message">策略失败时写入异常的诊断文本。</param>
    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
