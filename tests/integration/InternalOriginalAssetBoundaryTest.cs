using Godot;
using System.Security.Cryptography;
using System.Text.Json;
using TouhouWuxiaSurvivor.Tests.Support;
using TouhouWuxiaSurvivor.Ui.Compendium;

namespace TouhouWuxiaSurvivor.Tests.Integration;

/// <summary>
/// 验证内部原作素材的逐条覆盖、规范尺寸、来源追踪和内部发行声明边界。
/// </summary>
public partial class InternalOriginalAssetBoundaryTest : Node
{
    private const string MappingPath = "res://assets/internal_original/preview_mappings.json";
    private const string AssetRoot = "res://assets/internal_original/";
    private const string BuildManifestPath = "res://tools/internal_assets/build_manifest.json";

    /// <summary>
    /// 依次验证图鉴映射、生成纹理、来源哈希和内部包配置；任何失败都返回非零退出码。
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
            VerifyOriginalAudioFiles();
            VerifyReplacementManifest();
            VerifyInternalBuildInclusion();
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
    /// 确认每个已接入来源的全部图鉴条目都有唯一映射，且映射中不存在内容目录孤儿项。
    /// </summary>
    private static void VerifyMappingCoverage()
    {
        var catalog = new InternalPreviewCatalog();
        var mappings = InternalAssetManifestProbe.ReadMappings();
        IReadOnlySet<string> unavailable = InternalAssetManifestProbe.ReadUnavailableIdentities();
        HashSet<string> mappedSources = mappings
            .Where(item => item.Category != "Pickup")
            .Select(item => item.SourceId)
            .ToHashSet(StringComparer.Ordinal);
        mappedSources.UnionWith(unavailable.Select(identity => identity.Split('\u001f')[0]));
        CompendiumEntry[] expected = CompendiumCatalog.All
            .Where(entry => entry.Category != CompendiumCategory.Build &&
                mappedSources.Contains(entry.SourceId)).ToArray();
        int expectedMappedCount = expected.Count(entry => !unavailable.Contains(Identity(entry)));
        Require(catalog.Count == expectedMappedCount,
            $"Internal mapping count {catalog.Count} does not match " +
            $"{expectedMappedCount} available entries.");
        foreach (CompendiumEntry entry in expected)
        {
            bool isUnavailable = unavailable.Contains(Identity(entry));
            Require(catalog.Contains(entry) != isUnavailable,
                isUnavailable
                    ? $"Unavailable entry also has a visual mapping: {Identity(entry)}."
                    : $"Internal mapping is missing {Identity(entry)}.");
        }

        foreach (var mapping in mappings.Where(item => item.Category != "Pickup"))
        {
            Require(expected.Any(entry => entry.SourceId == mapping.SourceId &&
                    entry.Category.ToString() == mapping.Category && entry.Name == mapping.Name),
                $"Internal mapping has no compendium entry: " +
                $"{mapping.SourceId}/{mapping.Category}/{mapping.Name}.");
        }
    }

    /// <summary>
    /// 使用与视觉目录一致的记录分隔符构造图鉴身份，供可用映射与缺失声明做互斥校验。
    /// </summary>
    private static string Identity(CompendiumEntry entry) =>
        $"{entry.SourceId}\u001f{entry.Category}\u001f{entry.Name}";

    /// <summary>
    /// 按渲染类型检查所有唯一输出的尺寸和 RGBA8 格式，允许符卡、强化与多条目共享图集。
    /// </summary>
    private static void VerifyMappedAssets()
    {
        var visited = new HashSet<string>(StringComparer.Ordinal);
        foreach (var item in InternalAssetManifestProbe.ReadMappings())
        {
            string relative = item.Asset;
            if (!visited.Add(relative))
            {
                continue;
            }

            Vector2I expectedSize = item.Kind switch
            {
                "Scene" => new Vector2I(128, 80),
                "ActorStrip" => new Vector2I(192, 48),
                "Portrait" => new Vector2I(80, 80),
                "BulletAtlas" => new Vector2I(256, 256),
                "ItemAtlas" => new Vector2I(256, 64),
                _ => throw new InvalidDataException($"Unknown internal preview kind: {item.Kind}."),
            };
            Image image = Image.LoadFromFile(ProjectSettings.GlobalizePath(AssetRoot + relative));
            Require(!image.IsEmpty() && image.GetSize() == expectedSize,
                $"Internal asset has invalid size: {relative}, actual {image.GetSize()}.");
            Require(image.GetFormat() == Image.Format.Rgba8,
                $"Internal asset is not RGBA8: {relative}.");
        }

        Require(visited.Count > 0, "Internal mappings did not reference any generated assets.");
    }

    /// <summary>
    /// 确认生成器逐条记录所有清单外部输入的 SHA-256，且没有遗漏、陈旧或孤立来源。
    /// </summary>
    private static void VerifySourceHashes()
    {
        IReadOnlySet<string> expected = InternalAssetManifestProbe.ReadBuildSources();
        string[] lines = Godot.FileAccess.GetFileAsString(AssetRoot + "source_files.sha256")
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        HashSet<string> actual = lines.Select(line =>
            line.Length > 66 ? line[66..] : throw new InvalidDataException(
                $"Malformed source hash line: {line}.")).ToHashSet(StringComparer.Ordinal);
        Require(actual.SetEquals(expected),
            $"Source hash coverage differs: expected {expected.Count}, actual {actual.Count}.");
    }

    /// <summary>
    /// 确认正式世界需要的八类 TH16 原作 WAV 均已生成且包含 RIFF/WAVE 文件头，而不是空占位文件。
    /// </summary>
    private static void VerifyOriginalAudioFiles()
    {
        string[] names =
        [
            "shot", "footstep", "pickup", "enemy_hit",
            "enemy_death", "explosion", "player_hurt", "player_death",
        ];
        foreach (string name in names)
        {
            string path = ProjectSettings.GlobalizePath(AssetRoot + $"base/audio/{name}.wav");
            byte[] header = File.ReadAllBytes(path).Take(12).ToArray();
            Require(header.Length == 12 && System.Text.Encoding.ASCII.GetString(header, 0, 4) == "RIFF" &&
                System.Text.Encoding.ASCII.GetString(header, 8, 4) == "WAVE",
                $"Original audio is missing or not a WAV file: {name}.");
        }

        string bgmPath = ProjectSettings.GlobalizePath(AssetRoot + "base/audio/bgm_reimu.ogg");
        byte[] bgmHeader = File.ReadAllBytes(bgmPath).Take(4).ToArray();
        Require(bgmHeader.Length == 4 && System.Text.Encoding.ASCII.GetString(bgmHeader) == "OggS",
            "Original Reimu BGM is missing or not an OGG stream.");
    }

    /// <summary>
    /// 确认声明同时包含内部使用边界、本体跨作品代用说明与内部包携带约定。
    /// </summary>
    private static void VerifyReplacementManifest()
    {
        string manifest = Godot.FileAccess.GetFileAsString(AssetRoot + "MANIFEST.md");
        Require(manifest.Contains("非公开内部开发", StringComparison.Ordinal) &&
            manifest.Contains("跨作品视觉代用", StringComparison.Ordinal) &&
            manifest.Contains("不代表原著归属", StringComparison.Ordinal) &&
            manifest.Contains("内部测试包", StringComparison.Ordinal) &&
            manifest.Contains("可携带", StringComparison.Ordinal),
            "Internal asset manifest lost usage, attribution, or replacement boundaries.");
    }

    /// <summary>
    /// 读取真实导出预设，确认当前内部 Windows 测试包会携带共享原作素材而不是产生文字回退。
    /// </summary>
    private static void VerifyInternalBuildInclusion()
    {
        string presets = Godot.FileAccess.GetFileAsString("res://export_presets.cfg");
        Require(!presets.Contains("assets/internal_original/*", StringComparison.Ordinal) &&
            presets.Contains("export_filter=\"all_resources\"", StringComparison.Ordinal),
            "Internal Windows build no longer includes the shared original-asset tree.");
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
