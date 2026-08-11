using Godot;
using System.Text.Json;
using MappingEntry = (string SourceId, string Category, string Name, string Asset, string Kind);

namespace TouhouWuxiaSurvivor.Tests.Integration;

/// <summary>
/// 逐作品验证内部原作纹理，并在普通渲染器中生成不裁切、最近邻的四列联系表。
/// </summary>
public partial class InternalAssetContactSheetTest : Node
{
    private const string CentralMappingPath = "res://assets/internal_original/preview_mappings.json";
    private const string PackMappingRoot = "res://assets/internal_original/mappings";
    private const string AssetRoot = "res://assets/internal_original/";
    private const int Columns = 4;
    private const int PageCapacity = 24;

    /// <summary>
    /// 读取所有映射、验证每张实际纹理，并仅在普通渲染器中逐来源构建和保存联系表。
    /// </summary>
    public override async void _Ready()
    {
        try
        {
            Dictionary<string, List<MappingEntry>> groups = ReadMappings();
            bool headless = DisplayServer.GetName() == "headless";
            foreach ((string sourceId, List<MappingEntry> entries) in
                groups.OrderBy(pair => pair.Key, StringComparer.Ordinal))
            {
                (MappingEntry Entry, Texture2D Texture)[] validated = entries.Select(
                    entry => (entry, ValidateAndLoad(entry))).ToArray();
                if (!headless)
                {
                    await CaptureSource(sourceId, validated);
                }
            }

            Require(groups.Count > 0, "Internal mappings did not define any source groups.");
            GD.Print($"Internal asset contact sheet test passed for {groups.Count} sources.");
            GetTree().Quit();
        }
        catch (Exception exception)
        {
            GD.PushError(exception.ToString());
            GetTree().Quit(1);
        }
    }

    /// <summary>
    /// 通过 Godot FileAccess 与 DirAccess 合并中央和逐作映射，并锁定中央拆分及单文件归属规则。
    /// </summary>
    private static Dictionary<string, List<MappingEntry>> ReadMappings()
    {
        var groups = new Dictionary<string, List<MappingEntry>>(StringComparer.Ordinal);
        var identities = new HashSet<string>(StringComparer.Ordinal);
        Require(Godot.FileAccess.FileExists(CentralMappingPath),
            $"Central internal mapping is missing: {CentralMappingPath}.");
        HashSet<string> centralSources = AddMappingFile(CentralMappingPath, groups, identities);
        Require(centralSources.SetEquals(["base", "th06_eosd"]),
            "Central mapping must split into exactly base and th06_eosd contact sheets.");

        using DirAccess? directory = DirAccess.Open(PackMappingRoot);
        Require(directory is not null, $"Pack mapping directory is missing: {PackMappingRoot}.");
        foreach (string fileName in directory!.GetFiles()
                     .Where(name => name.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                     .Order(StringComparer.Ordinal))
        {
            HashSet<string> sources = AddMappingFile(
                $"{PackMappingRoot}/{fileName}", groups, identities);
            Require(sources.Count == 1 &&
                    sources.Single() == Path.GetFileNameWithoutExtension(fileName),
                $"Pack mapping must own one matching sourceId: {fileName}.");
        }

        return groups;
    }

    /// <summary>
    /// 严格解析一份映射文件，把完整中文名和分类加入来源组，同时拒绝空字段与跨文件重复条目。
    /// </summary>
    private static HashSet<string> AddMappingFile(string path,
        Dictionary<string, List<MappingEntry>> groups, HashSet<string> identities)
    {
        using JsonDocument document = JsonDocument.Parse(Godot.FileAccess.GetFileAsString(path));
        var sources = new HashSet<string>(StringComparer.Ordinal);
        foreach (JsonElement item in document.RootElement.GetProperty("entries").EnumerateArray())
        {
            MappingEntry entry = (Required(item, "sourceId"), Required(item, "category"),
                Required(item, "name"), Required(item, "asset"), Required(item, "kind"));
            string identity = $"{entry.SourceId}\u001f{entry.Category}\u001f{entry.Name}";
            Require(identities.Add(identity), $"Duplicate internal mapping: {identity}.");
            if (!groups.TryGetValue(entry.SourceId, out List<MappingEntry>? entries))
            {
                entries = [];
                groups.Add(entry.SourceId, entries);
            }

            entries.Add(entry);
            sources.Add(entry.SourceId);
        }

        Require(sources.Count > 0, $"Internal mapping has no entries: {path}.");
        return sources;
    }

    /// <summary>
    /// 读取必需字符串并拒绝空白，以便损坏映射在资源加载前指出具体字段。
    /// </summary>
    private static string Required(JsonElement item, string property)
    {
        string? value = item.GetProperty(property).GetString();
        return !string.IsNullOrWhiteSpace(value)
            ? value
            : throw new InvalidDataException($"Internal mapping field is empty: {property}.");
    }

    /// <summary>
    /// 通过正式 Godot 资源加载器读取纹理，严格检查种类尺寸、可见像素，并深入验证动画四帧。
    /// </summary>
    private static Texture2D ValidateAndLoad(MappingEntry entry)
    {
        string path = AssetRoot + entry.Asset;
        Require(Godot.FileAccess.FileExists(path), $"Mapped PNG is missing: {path}.");
        Require(ResourceLoader.Exists(path, "Texture2D"), $"Texture was not imported: {path}.");
        Texture2D? texture = ResourceLoader.Load<Texture2D>(path);
        Require(texture is not null, $"Could not load mapped texture: {path}.");
        Image image = texture!.GetImage();
        Vector2I expected = ExpectedSize(entry.Kind);
        Require(!image.IsEmpty() && image.GetSize() == expected,
            $"{entry.Kind} has invalid size: {path}, actual {image.GetSize()}, expected {expected}.");
        Require(image.GetUsedRect().HasArea(), $"Mapped texture is fully transparent: {path}.");
        if (entry.Kind == "ActorStrip")
        {
            VerifyActorStrip(image, path);
        }

        return texture;
    }

    /// <summary>
    /// 返回构建器对每种内部纹理承诺的固定像素尺寸，未知种类直接视为清单错误。
    /// </summary>
    private static Vector2I ExpectedSize(string kind) => kind switch
    {
        "Scene" => new Vector2I(128, 80),
        "ActorStrip" => new Vector2I(192, 48),
        "Portrait" => new Vector2I(80, 80),
        "BulletAtlas" => new Vector2I(256, 256),
        "ItemAtlas" => new Vector2I(256, 64),
        _ => throw new InvalidDataException($"Unknown internal texture kind: {kind}."),
    };

    /// <summary>
    /// 将 192x48 动画条拆成四个 48x48 帧，要求每帧可见且至少存在一组像素不同的帧。
    /// </summary>
    private static void VerifyActorStrip(Image strip, string path)
    {
        var frameData = new List<byte[]>(4);
        for (int frameIndex = 0; frameIndex < 4; frameIndex++)
        {
            Image frame = strip.GetRegion(new Rect2I(frameIndex * 48, 0, 48, 48));
            Rect2I usedRect = frame.GetUsedRect();
            Require(usedRect.HasArea(),
                $"Actor strip frame {frameIndex} is transparent: {path}; " +
                $"format={strip.GetFormat()}, stripUsed={strip.GetUsedRect()}.");
            frame.Convert(Image.Format.Rgba8);
            frameData.Add(frame.GetData());
        }

        Require(frameData.Skip(1).Any(data => !data.SequenceEqual(frameData[0])),
            $"Actor strip has four identical frames: {path}.");
    }

    /// <summary>
    /// 将一个来源按最多二十四项分页；当前每作只生成一张，未来超量时不会让内容溢出视口。
    /// </summary>
    private async Task CaptureSource(string sourceId,
        IReadOnlyList<(MappingEntry Entry, Texture2D Texture)> entries)
    {
        int pageCount = Mathf.CeilToInt(entries.Count / (float)PageCapacity);
        for (int pageIndex = 0; pageIndex < pageCount; pageIndex++)
        {
            (MappingEntry Entry, Texture2D Texture)[] page = entries.Skip(
                pageIndex * PageCapacity).Take(PageCapacity).ToArray();
            Control sheet = CreateSheet(sourceId, page, pageIndex, pageCount);
            AddChild(sheet);
            sheet.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            string suffix = pageCount == 1 ? "" : $"-p{pageIndex + 1:00}";
            SaveScreenshot($"internal-contact-sheet-{sourceId}{suffix}.png");
            RemoveChild(sheet);
            sheet.Free();
        }
    }

    /// <summary>
    /// 在 640x360 设计视口内建立四列紧凑联系表，按行数计算单元高度并保持全部纹理等比可见。
    /// </summary>
    private static Control CreateSheet(string sourceId,
        IReadOnlyList<(MappingEntry Entry, Texture2D Texture)> entries,
        int pageIndex, int pageCount)
    {
        var root = new Control { TextureFilter = CanvasItem.TextureFilterEnum.Nearest };
        var background = new ColorRect { Color = new Color("0b120f") };
        root.AddChild(background);
        background.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        var title = new Label
        {
            Position = new Vector2(6, 3), Size = new Vector2(628, 25),
            Text = $"内部原作素材 · {sourceId} · {entries.Count}项" +
                (pageCount > 1 ? $" · {pageIndex + 1}/{pageCount}" : ""),
            VerticalAlignment = VerticalAlignment.Center,
        };
        title.AddThemeFontSizeOverride("font_size", 14);
        title.AddThemeColorOverride("font_color", new Color("e9e4cf"));
        root.AddChild(title);

        int rows = Mathf.CeilToInt(entries.Count / (float)Columns);
        float rowHeight = (324.0f - Math.Max(0, rows - 1) * 2.0f) / Math.Max(1, rows);
        var grid = new GridContainer
        {
            Columns = Columns, Position = new Vector2(4, 32), Size = new Vector2(632, 324),
        };
        grid.AddThemeConstantOverride("h_separation", 4);
        grid.AddThemeConstantOverride("v_separation", 2);
        root.AddChild(grid);
        foreach ((MappingEntry entry, Texture2D texture) in entries)
        {
            grid.AddChild(CreateCell(entry, texture, rowHeight));
        }

        return root;
    }

    /// <summary>
    /// 建立单个不裁切预览格：完整纹理居中缩放，中文分类与完整名称分别占一行。
    /// </summary>
    private static PanelContainer CreateCell(
        MappingEntry entry, Texture2D texture, float rowHeight)
    {
        var style = new StyleBoxFlat
        {
            BgColor = new Color("111c17"), BorderColor = new Color("3d5949"),
            BorderWidthLeft = 1, BorderWidthTop = 1,
            BorderWidthRight = 1, BorderWidthBottom = 1,
            ContentMarginLeft = 3, ContentMarginTop = 2,
            ContentMarginRight = 3, ContentMarginBottom = 2,
        };
        var panel = new PanelContainer { CustomMinimumSize = new Vector2(155, rowHeight) };
        panel.AddThemeStyleboxOverride("panel", style);
        var layout = new VBoxContainer();
        layout.AddThemeConstantOverride("separation", 0);
        panel.AddChild(layout);
        var preview = new TextureRect
        {
            Texture = texture, CustomMinimumSize = new Vector2(147, Math.Max(18, rowHeight - 22)),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
        };
        layout.AddChild(preview);
        layout.AddChild(CreateLabel(LocalizeCategory(entry.Category), 8, new Color("93b59d")));
        layout.AddChild(CreateLabel(entry.Name, entry.Name.Length > 12 ? 8 : 9,
            new Color("eee9d8")));
        return panel;
    }

    /// <summary>
    /// 创建不换行的居中文本行；字号由调用者控制，以保证较长中文名称仍完整落入单元格。
    /// </summary>
    private static Label CreateLabel(string text, int fontSize, Color color)
    {
        var label = new Label
        {
            Text = text, HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center, CustomMinimumSize = new Vector2(147, 9),
        };
        label.AddThemeFontSizeOverride("font_size", fontSize);
        label.AddThemeColorOverride("font_color", color);
        return label;
    }

    /// <summary>
    /// 将映射中的稳定英文分类转为联系表中文标签，同时拒绝未规划的新分类静默显示。
    /// </summary>
    private static string LocalizeCategory(string category) => category switch
    {
        "Biome" => "地区", "Structure" => "结构", "Enemy" => "敌人",
        "Character" => "角色", "SpellCard" => "符卡", "Pickup" => "道具",
        _ => throw new InvalidDataException($"Unknown internal category: {category}."),
    };

    /// <summary>
    /// 保存当前普通渲染器画面并输出绝对路径；调用点已确保布局和绘制稳定两帧。
    /// </summary>
    private void SaveScreenshot(string fileName)
    {
        Image image = GetViewport().GetTexture().GetImage();
        string path = ProjectSettings.GlobalizePath("user://" + fileName);
        Require(image.SavePng(path) == Error.Ok, $"Could not save contact sheet: {path}.");
        GD.Print($"Internal contact sheet: {path}");
    }

    /// <summary>
    /// 将资源、动画或布局契约失败转换为带具体上下文的异常，使 Godot 以状态码 1 退出。
    /// </summary>
    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
