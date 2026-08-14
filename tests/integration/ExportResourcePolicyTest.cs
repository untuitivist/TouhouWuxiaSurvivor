using Godot;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace TouhouWuxiaSurvivor.Tests.Integration;

/// <summary>
/// 验证正式导出只携带当前原作相关资源，并持续隔离早期参考工程遗留素材。
/// </summary>
public partial class ExportResourcePolicyTest : Node
{
    private const string PresetPath = "res://export_presets.cfg";
    private const string LegacyPlayerScene = "res://src/actors/player/PlayerVisual.tscn";

    private static readonly string[] LegacyAssetPaths =
    [
        "assets/audio/music/cowboy_secret.wav",
        "assets/audio/sfx/enemies/death.wav",
        "assets/audio/sfx/enemies/explosion.wav",
        "assets/audio/sfx/enemies/hit.wav",
        "assets/audio/sfx/player/death.wav",
        "assets/audio/sfx/player/footstep.wav",
        "assets/audio/sfx/player/gunshot.wav",
        "assets/audio/sfx/player/pickup.wav",
        "assets/characters/first_player/floating_cannon.png",
        "assets/characters/first_player/player_full_sheet.png",
        "assets/characters/first_player/player_sheet.png",
        "assets/combat/enemies/enemy_sheet.png",
        "assets/combat/enemies/explosion_sheet.png",
        "assets/combat/pickups/pickup_sheet.png",
        "assets/combat/projectiles/item_sheet.png",
    ];

    private static readonly string[] LegacyResourcePrefixes =
    [
        "res://assets/audio/",
        "res://assets/characters/first_player/",
        "res://assets/combat/enemies/",
        "res://assets/combat/pickups/",
        "res://assets/combat/projectiles/",
    ];

    private static readonly string[] RootDiagnosticResidues = ["-e", "stdout"];

    private static readonly string[] TextResourceExtensions =
    [
        ".cs", ".gd", ".tscn", ".tres", ".cfg", ".json", ".godot",
    ];

    /// <summary>
    /// 依次检查排除清单、正式资源引用和原作资源保留策略，并以退出码反馈结果。
    /// </summary>
    public override void _Ready()
    {
        try
        {
            string[] exclusions = ReadExcludePatterns();
            VerifyLegacyAssetsExcluded(exclusions);
            VerifyRootDiagnosticResiduesExcluded(exclusions);
            VerifyFormalResourcesDoNotReferenceLegacyAssets();
            VerifyInternalOriginalAssetsRemainIncluded(exclusions);
            VerifyReleaseCompilationBoundary();
            GD.Print("Export resource policy test passed.");
            GetTree().Quit();
        }
        catch (Exception exception)
        {
            GD.PushError(exception.ToString());
            GetTree().Quit(1);
        }
    }

    /// <summary>
    /// 结构化解析项目文件，确保测试与素材构建器只服务开发环境而不进入正式托管程序集。
    /// </summary>
    private static void VerifyReleaseCompilationBoundary()
    {
        string xml = Godot.FileAccess.GetFileAsString(
            "res://TouhouWuxiaSurvivor.csproj");
        XDocument document = XDocument.Parse(xml);
        XElement[] compileItems = document.Descendants("Compile").ToArray();
        bool removesTestsInRelease = compileItems.Any(item =>
            ItemPath(item, "Remove") == "tests/**/*.cs" &&
            IsShippingCondition(item.Parent?.Attribute("Condition")?.Value, true));
        Require(removesTestsInRelease,
            "Release compilation must remove all integration and support test code.");

        XElement[] toolIncludes = compileItems.Where(item =>
            ItemPath(item, "Include").StartsWith(
                "tools/internal_assets/", StringComparison.Ordinal)).ToArray();
        Require(toolIncludes.Length > 0 && toolIncludes.All(item =>
                IsShippingCondition(item.Parent?.Attribute("Condition")?.Value, false)),
            "Internal asset builders must only compile outside Release builds.");
    }

    /// <summary>读取一个 Compile 项的路径属性，缺失时返回空串以便统一判定。</summary>
    private static string ItemPath(XElement item, string attribute) =>
        item.Attribute(attribute)?.Value ?? string.Empty;

    /// <summary>
    /// 同时识别普通 Release 与 Godot ExportRelease 条件，防止编辑器导出绕过发布边界。
    /// </summary>
    private static bool IsShippingCondition(string? condition, bool expectedShipping) =>
        condition is not null &&
        condition.Contains("$(Configuration)", StringComparison.Ordinal) &&
        condition.Contains(expectedShipping ? "== 'Release'" : "!= 'Release'",
            StringComparison.Ordinal) &&
        condition.Contains(expectedShipping ? "== 'ExportRelease'" : "!= 'ExportRelease'",
            StringComparison.Ordinal) &&
        condition.Contains(expectedShipping ? " Or " : " And ", StringComparison.Ordinal);

    /// <summary>
    /// 确认仍待用户决定是否删除的根目录诊断残留不会进入任何正式导出物。
    /// </summary>
    private static void VerifyRootDiagnosticResiduesExcluded(IReadOnlyList<string> exclusions)
    {
        foreach (string residue in RootDiagnosticResidues)
        {
            Require(exclusions.Any(pattern => MatchesExportPattern(residue, pattern)),
                $"Root diagnostic residue is not excluded from export: {residue}.");
        }
    }

    /// <summary>
    /// 确认十五个已按哈希核验的参考工程原件均命中至少一条导出排除规则。
    /// </summary>
    private static void VerifyLegacyAssetsExcluded(IReadOnlyList<string> exclusions)
    {
        foreach (string assetPath in LegacyAssetPaths)
        {
            Require(exclusions.Any(pattern => MatchesExportPattern(assetPath, pattern)),
                $"Legacy reference asset is not excluded from export: {assetPath}.");
        }
    }

    /// <summary>
    /// 扫描正式源码和资源文本，拒绝除已隔离旧场景外的任何旧素材路径引用。
    /// </summary>
    private static void VerifyFormalResourcesDoNotReferenceLegacyAssets()
    {
        foreach (string path in FindTextResources("res://src"))
        {
            if (path == LegacyPlayerScene)
            {
                continue;
            }

            VerifyTextResource(path);
        }

        VerifyTextResource("res://project.godot");
    }

    /// <summary>
    /// 读取一个正式文本资源，报告其中命中的旧示例素材命名空间及具体文件。
    /// </summary>
    private static void VerifyTextResource(string path)
    {
        string content = Godot.FileAccess.GetFileAsString(path);
        Require(!content.Contains(LegacyPlayerScene, StringComparison.Ordinal),
            $"Formal resource still references the isolated legacy player scene: {path}.");
        foreach (string prefix in LegacyResourcePrefixes)
        {
            Require(!content.Contains(prefix, StringComparison.Ordinal),
                $"Formal resource still references a legacy asset root: {path} -> {prefix}.");
        }
    }

    /// <summary>
    /// 确认全资源导出仍启用，且任何排除通配符都不会误伤共享原作素材目录。
    /// </summary>
    private static void VerifyInternalOriginalAssetsRemainIncluded(IReadOnlyList<string> exclusions)
    {
        var preset = new ConfigFile();
        Error error = preset.Load(PresetPath);
        Require(error == Error.Ok, $"Unable to load export preset: {error}.");
        Require(preset.GetValue("preset.0", "export_filter").AsString() == "all_resources",
            "Formal export must continue to include all non-excluded resources.");

        const string representative = "assets/internal_original/base/audio/shot.wav";
        Require(!exclusions.Any(pattern => MatchesExportPattern(representative, pattern)),
            "Export exclusions unexpectedly remove internal original assets.");
    }

    /// <summary>
    /// 从 Windows 正式预设读取逗号分隔的排除表达式并移除无意义空白项。
    /// </summary>
    private static string[] ReadExcludePatterns()
    {
        var preset = new ConfigFile();
        Error error = preset.Load(PresetPath);
        Require(error == Error.Ok, $"Unable to load export preset: {error}.");
        return preset.GetValue("preset.0", "exclude_filter").AsString()
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    /// <summary>
    /// 递归枚举正式目录中的文本型资源，返回按路径稳定排序的检查集合。
    /// </summary>
    private static IReadOnlyList<string> FindTextResources(string root)
    {
        var results = new List<string>();
        CollectTextResources(root, results);
        results.Sort(StringComparer.Ordinal);
        return results;
    }

    /// <summary>
    /// 深度遍历一个 Godot 资源目录，仅收集可能持有资源路径的文本扩展名。
    /// </summary>
    private static void CollectTextResources(string root, List<string> results)
    {
        DirAccess? openedDirectory = DirAccess.Open(root);
        Require(openedDirectory is not null,
            $"Unable to inspect formal resource directory: {root}.");
        using DirAccess directory = openedDirectory!;
        directory.ListDirBegin();
        string name = directory.GetNext();
        while (!string.IsNullOrEmpty(name))
        {
            string path = $"{root}/{name}";
            if (directory.CurrentIsDir())
            {
                CollectTextResources(path, results);
            }
            else if (TextResourceExtensions.Any(extension =>
                name.EndsWith(extension, StringComparison.OrdinalIgnoreCase)))
            {
                results.Add(path);
            }

            name = directory.GetNext();
        }

        directory.ListDirEnd();
    }

    /// <summary>
    /// 按 Godot 导出过滤器的星号和问号语义匹配资源路径，用于验证规则实际覆盖面。
    /// </summary>
    private static bool MatchesExportPattern(string resourcePath, string pattern)
    {
        string expression = "^" + Regex.Escape(pattern)
            .Replace("\\*", ".*", StringComparison.Ordinal)
            .Replace("\\?", ".", StringComparison.Ordinal) + "$";
        return Regex.IsMatch(resourcePath, expression,
            RegexOptions.CultureInvariant | RegexOptions.Singleline);
    }

    /// <summary>
    /// 将策略违约转换为带资源路径的测试异常，便于无头测试直接定位问题。
    /// </summary>
    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
