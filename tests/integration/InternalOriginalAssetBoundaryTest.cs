using Godot;
using System.Security.Cryptography;
using System.Text.Json;
using TouhouWuxiaSurvivor.Ui.Compendium;

namespace TouhouWuxiaSurvivor.Tests.Integration;

/// <summary>
/// 验证内部原作素材的逐条覆盖、规范尺寸、来源追踪和公开导出隔离边界。
/// </summary>
public partial class InternalOriginalAssetBoundaryTest : Node
{
    private const string MappingPath = "res://assets/internal_original/preview_mappings.json";
    private const string AssetRoot = "res://assets/internal_original/";
    private const string BuildManifestPath = "res://tools/internal_assets/build_manifest.json";

    /// <summary>
    /// 依次验证图鉴映射、生成纹理、来源哈希和导出排除；任何失败都返回非零退出码。
    /// </summary>
    public override void _Ready()
    {
        try
        {
            VerifyMappingCoverage();
            VerifyMappedAssets();
            VerifyBaseEnemyVisuals();
            VerifyBaseEnemySources();
            VerifySourceHashes();
            VerifyReplacementManifest();
            VerifyPublicExportExclusion();
            GD.Print("Internal original asset boundary test passed.");
            GetTree().Quit();
        }
        catch (Exception exception)
        {
            GD.PushError(exception.ToString());
            GetTree().Quit(1);
        }
    }

    /// <summary>
    /// 锁定九类本体敌人的中文名到输出文件映射，并确认每条四帧动画非空且包含真实变化。
    /// </summary>
    private static void VerifyBaseEnemyVisuals()
    {
        var expected = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["野妖精"] = "base/actors/wild_fairy.png",
            ["毛玉"] = "base/actors/kedama.png",
            ["妖虫"] = "base/actors/youkai_insect.png",
            ["阴阳玉"] = "base/actors/yin_yang_orb.png",
            ["森林精怪"] = "base/actors/forest_spirit.png",
            ["山精"] = "base/actors/mountain_spirit.png",
            ["流窜妖怪"] = "base/actors/village_outlaw.png",
            ["夜行妖怪"] = "base/actors/wandering_youkai.png",
            ["大妖怪"] = "base/actors/great_youkai.png",
        };
        using JsonDocument document = JsonDocument.Parse(
            Godot.FileAccess.GetFileAsString(MappingPath));
        Dictionary<string, string> actual = document.RootElement.GetProperty("entries")
            .EnumerateArray()
            .Where(item => item.GetProperty("sourceId").GetString() == "base" &&
                item.GetProperty("category").GetString() == "Enemy")
            .ToDictionary(item => item.GetProperty("name").GetString()!,
                item => item.GetProperty("asset").GetString()!, StringComparer.Ordinal);
        Require(actual.Count == expected.Count,
            $"Expected {expected.Count} base enemy mappings, found {actual.Count}.");
        foreach ((string name, string relative) in expected)
        {
            Require(actual.GetValueOrDefault(name) == relative,
                $"Base enemy mapping is incorrect: {name} -> {actual.GetValueOrDefault(name)}.");
            VerifyAnimatedStrip(name, relative);
        }
    }

    /// <summary>
    /// 逐格检查 192×48 动画条的四个 48×48 帧都有可见像素，并至少存在两个不同像素状态。
    /// </summary>
    private static void VerifyAnimatedStrip(string name, string relative)
    {
        Image strip = Image.LoadFromFile(ProjectSettings.GlobalizePath(AssetRoot + relative));
        var frameHashes = new HashSet<string>(StringComparer.Ordinal);
        for (int frame = 0; frame < 4; frame++)
        {
            Image image = strip.GetRegion(new Rect2I(frame * 48, 0, 48, 48));
            Require(image.GetUsedRect().HasArea(), $"Base enemy frame is empty: {name}/{frame}.");
            image.Convert(Image.Format.Rgba8);
            frameHashes.Add(Convert.ToHexString(SHA256.HashData(image.GetData())));
        }

        Require(frameHashes.Count >= 2, $"Base enemy animation has no frame variation: {name}.");
    }

    /// <summary>
    /// 确认九个本体演员只从已审查的通用 enemy 图集构建，禁止重新接入 stgenm 舞台人物图。
    /// </summary>
    private static void VerifyBaseEnemySources()
    {
        using JsonDocument document = JsonDocument.Parse(
            Godot.FileAccess.GetFileAsString(BuildManifestPath));
        int baseActorCount = 0;
        VerifyBaseEnemySources(document.RootElement.GetProperty("gridStrips"), ref baseActorCount);
        VerifyBaseEnemySources(document.RootElement.GetProperty("staticStrips"), ref baseActorCount);
        Require(baseActorCount == 9,
            $"Expected 9 generic base enemy source definitions, found {baseActorCount}.");
    }

    /// <summary>
    /// 检查一种构建定义集合中的本体演员来源，并累计实际定义数供调用者验证完整覆盖。
    /// </summary>
    private static void VerifyBaseEnemySources(JsonElement definitions, ref int baseActorCount)
    {
        foreach (JsonElement definition in definitions.EnumerateArray())
        {
            string output = definition.GetProperty("output").GetString()!;
            if (!output.StartsWith("base/actors/", StringComparison.Ordinal))
            {
                continue;
            }

            baseActorCount++;
            string source = definition.GetProperty("source").GetString()!;
            Require(source.Contains("/ANM/ANM/enemy/", StringComparison.Ordinal) &&
                !source.Contains("/stgenm/", StringComparison.Ordinal),
                $"Base enemy uses an unreviewed stage-character atlas: {output} <- {source}.");
        }
    }

    /// <summary>
    /// 确认本体与红魔乡的 39 个图鉴条目都存在唯一映射，且没有清单孤儿项。
    /// </summary>
    private static void VerifyMappingCoverage()
    {
        var catalog = new InternalPreviewCatalog();
        CompendiumEntry[] expected = CompendiumCatalog.All.Where(entry =>
            entry.SourceId is CompendiumCatalog.BaseSourceId or "th06_eosd").ToArray();
        Require(expected.Length == 39, $"Expected 39 base/TH06 entries, found {expected.Length}.");
        Require(catalog.Count == expected.Length,
            $"Internal mapping count {catalog.Count} does not match {expected.Length} entries.");
        CompendiumEntry? missing = expected.FirstOrDefault(entry => !catalog.Contains(entry));
        Require(missing is null,
            $"Internal mapping is missing {missing?.SourceId}/{missing?.Category}/{missing?.Name}.");
    }

    /// <summary>
    /// 按渲染类型检查 38 个唯一输出的尺寸和 RGBA8 格式，允许两张符卡共享同一弹幕图集。
    /// </summary>
    private static void VerifyMappedAssets()
    {
        using JsonDocument document = JsonDocument.Parse(Godot.FileAccess.GetFileAsString(MappingPath));
        var visited = new HashSet<string>(StringComparer.Ordinal);
        foreach (JsonElement item in document.RootElement.GetProperty("entries").EnumerateArray())
        {
            string relative = item.GetProperty("asset").GetString()!;
            if (!visited.Add(relative))
            {
                continue;
            }

            string kind = item.GetProperty("kind").GetString()!;
            Vector2I expectedSize = kind switch
            {
                "Scene" => new Vector2I(128, 80),
                "ActorStrip" => new Vector2I(192, 48),
                "Portrait" => new Vector2I(80, 80),
                "BulletAtlas" => new Vector2I(256, 256),
                _ => throw new InvalidDataException($"Unknown internal preview kind: {kind}."),
            };
            Image image = Image.LoadFromFile(ProjectSettings.GlobalizePath(AssetRoot + relative));
            Require(!image.IsEmpty() && image.GetSize() == expectedSize,
                $"Internal asset has invalid size: {relative}, actual {image.GetSize()}.");
            Require(image.GetFormat() == Image.Format.Rgba8,
                $"Internal asset is not RGBA8: {relative}.");
        }

        Require(visited.Count == 38,
            $"Expected 38 unique mapped assets, found {visited.Count}.");
    }

    /// <summary>
    /// 确认生成器记录全部 42 个外部输入的 SHA-256，便于素材更新后判断是否需要重新生成。
    /// </summary>
    private static void VerifySourceHashes()
    {
        string hashes = Godot.FileAccess.GetFileAsString(AssetRoot + "source_files.sha256");
        int count = hashes.Split('\n', StringSplitOptions.RemoveEmptyEntries).Length;
        Require(count == 42, $"Expected 42 source hashes, found {count}.");
    }

    /// <summary>
    /// 确认声明同时包含内部使用边界、本体跨作品代用说明与公开前替换要求。
    /// </summary>
    private static void VerifyReplacementManifest()
    {
        string manifest = Godot.FileAccess.GetFileAsString(AssetRoot + "MANIFEST.md");
        Require(manifest.Contains("非公开内部开发", StringComparison.Ordinal) &&
            manifest.Contains("跨作品视觉代用", StringComparison.Ordinal) &&
            manifest.Contains("不代表原著归属", StringComparison.Ordinal) &&
            manifest.Contains("正式公开版本发布前", StringComparison.Ordinal),
            "Internal asset manifest lost usage, attribution, or replacement boundaries.");
    }

    /// <summary>
    /// 读取真实导出预设，确认公开 Windows 包排除整个内部资源树而非当前文件列表。
    /// </summary>
    private static void VerifyPublicExportExclusion()
    {
        string presets = Godot.FileAccess.GetFileAsString("res://export_presets.cfg");
        Require(presets.Contains("assets/internal_original/*", StringComparison.Ordinal),
            "Windows Release no longer excludes the internal original-asset tree.");
    }

    /// <summary>
    /// 将资源边界违约转换为包含条目或路径说明的测试异常。
    /// </summary>
    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
