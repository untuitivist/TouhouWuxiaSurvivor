using System.Text.Json;
using Godot;
using static TouhouWuxiaSurvivor.Tools.InternalAssets.InternalImageTransformer;

namespace TouhouWuxiaSurvivor.Tools.InternalAssets;

/// <summary>
/// 读取声明式构建清单，把用户提供的异构原作图集规范化为内部图鉴使用的场景、动画条、立绘和弹幕图集。
/// </summary>
public partial class InternalPreviewAssetBuilder : Node
{
    private const string ManifestPath = "res://tools/internal_assets/build_manifest.json";
    private const string OutputRoot = "res://assets/internal_original/";
    private readonly HashSet<string> _usedSources = new(StringComparer.Ordinal);

    /// <summary>
    /// 解析 `--source-root`，依次执行全部构建类型并写来源哈希；任何错误都让工具场景返回非零退出码。
    /// </summary>
    public override void _Ready()
    {
        try
        {
            WriteProgress("started", true);
            GD.Print("Internal asset builder: started.");
            string sourceRoot = ReadSourceRoot();
            using JsonDocument document = JsonDocument.Parse(
                Godot.FileAccess.GetFileAsString(ManifestPath));
            JsonElement root = document.RootElement;
            WriteProgress("manifest loaded");
            GD.Print("Internal asset builder: manifest loaded.");
            BuildScenes(sourceRoot, root.GetProperty("scenes"));
            WriteProgress("scenes complete");
            GD.Print("Internal asset builder: scenes complete.");
            BuildGridStrips(sourceRoot, root.GetProperty("gridStrips"));
            WriteProgress("grid strips complete");
            GD.Print("Internal asset builder: grid strips complete.");
            BuildStaticStrips(sourceRoot, root.GetProperty("staticStrips"));
            WriteProgress("static strips complete");
            GD.Print("Internal asset builder: static strips complete.");
            BuildPortraits(sourceRoot, root.GetProperty("portraits"));
            WriteProgress("portraits complete");
            GD.Print("Internal asset builder: portraits complete.");
            BuildCopies(sourceRoot, root.GetProperty("copies"));
            WriteProgress("copies complete");
            InternalSourceHashWriter.Write(sourceRoot, _usedSources,
                ProjectSettings.GlobalizePath(OutputRoot + "source_files.sha256"));
            GD.Print($"Internal preview assets built from {_usedSources.Count} source files.");
            GetTree().Quit();
        }
        catch (Exception exception)
        {
            GD.PushError(exception.ToString());
            GetTree().Quit(1);
        }
    }

    /// <summary>
    /// 将少量阶段标记写入 Godot 用户目录，绕过 console 包装层缓冲以定位内部工具阻塞点。
    /// </summary>
    private static void WriteProgress(string message, bool reset = false)
    {
        string path = ProjectSettings.GlobalizePath("user://internal-asset-builder-progress.txt");
        string line = $"{DateTime.Now:O} {message}{System.Environment.NewLine}";
        if (reset)
        {
            File.WriteAllText(path, line, new System.Text.UTF8Encoding(false));
            return;
        }

        File.AppendAllText(path, line, new System.Text.UTF8Encoding(false));
    }

    /// <summary>
    /// 从 Godot 用户参数读取外部素材根目录，并拒绝缺失或不存在的路径，避免把空输出误判为成功。
    /// </summary>
    private static string ReadSourceRoot()
    {
        string[] arguments = OS.GetCmdlineUserArgs();
        for (int index = 0; index + 1 < arguments.Length; index++)
        {
            if (arguments[index] == "--source-root" && Directory.Exists(arguments[index + 1]))
            {
                return Path.GetFullPath(arguments[index + 1]);
            }
        }

        throw new InvalidOperationException("Missing valid --source-root for internal assets.");
    }

    /// <summary>
    /// 把背景或透明贴图片按可选裁切铺在指定底色上，再以最近邻裁成统一 128x80 场景图。
    /// </summary>
    private void BuildScenes(string sourceRoot, JsonElement definitions)
    {
        foreach (JsonElement definition in definitions.EnumerateArray())
        {
            Image image = LoadImage(sourceRoot, definition);
            if (definition.TryGetProperty("crop", out JsonElement crop))
            {
                image = image.GetRegion(ReadRect(crop));
            }

            Color baseColor = ReadColor(definition.GetProperty("baseColor"));
            Image backdrop = Image.CreateEmpty(
                image.GetWidth(), image.GetHeight(), false, Image.Format.Rgba8);
            backdrop.Fill(baseColor);
            backdrop.BlendRect(image,
                new Rect2I(0, 0, image.GetWidth(), image.GetHeight()), Vector2I.Zero);
            Save(Fit(backdrop, new Vector2I(128, 80)), definition.GetProperty("output"));
        }
    }

    /// <summary>
    /// 从规则网格连续裁四帧，逐帧去透明边并居中到 48x48，最终输出 192x48 横向动画条。
    /// </summary>
    private void BuildGridStrips(string sourceRoot, JsonElement definitions)
    {
        foreach (JsonElement definition in definitions.EnumerateArray())
        {
            Image atlas = LoadImage(sourceRoot, definition);
            int startX = definition.GetProperty("startX").GetInt32();
            int startY = definition.GetProperty("startY").GetInt32();
            int frameWidth = definition.GetProperty("frameWidth").GetInt32();
            int frameHeight = definition.GetProperty("frameHeight").GetInt32();
            Image strip = CreateTransparent(new Vector2I(192, 48));
            for (int frame = 0; frame < 4; frame++)
            {
                Image sprite = atlas.GetRegion(new Rect2I(
                    startX + frame * frameWidth, startY, frameWidth, frameHeight));
                PlaceSprite(strip, sprite, frame, frame % 2);
            }

            Save(strip, definition.GetProperty("output"));
        }
    }

    /// <summary>
    /// 裁取单个完整原作轮廓并制成轻微上下浮动的四帧条，用于没有规则动作网格的通用单位。
    /// </summary>
    private void BuildStaticStrips(string sourceRoot, JsonElement definitions)
    {
        foreach (JsonElement definition in definitions.EnumerateArray())
        {
            Image sprite = LoadImage(sourceRoot, definition);
            if (definition.TryGetProperty("crop", out JsonElement crop))
            {
                sprite = sprite.GetRegion(ReadRect(crop));
            }
            Image strip = CreateTransparent(new Vector2I(192, 48));
            int[] offsets = [2, 1, 0, 1];
            for (int frame = 0; frame < 4; frame++)
            {
                PlaceSprite(strip, sprite, frame, offsets[frame]);
            }

            Save(strip, definition.GetProperty("output"));
        }
    }

    /// <summary>
    /// 裁取原作双表情图的左半角色，去透明边后缩入 80x80 透明画布，供图鉴保留中文题名叠加。
    /// </summary>
    private void BuildPortraits(string sourceRoot, JsonElement definitions)
    {
        foreach (JsonElement definition in definitions.EnumerateArray())
        {
            Image source = LoadImage(sourceRoot, definition);
            Rect2I crop = definition.TryGetProperty("crop", out JsonElement cropElement)
                ? ReadRect(cropElement)
                : new Rect2I(0, 0, source.GetWidth() / 2, source.GetHeight());
            Image portrait = source.GetRegion(crop);
            portrait = FitWithin(CropOpaque(portrait), new Vector2I(72, 72));
            Image canvas = CreateTransparent(new Vector2I(80, 80));
            canvas.BlendRect(portrait,
                new Rect2I(0, 0, portrait.GetWidth(), portrait.GetHeight()),
                new Vector2I((80 - portrait.GetWidth()) / 2, 4));
            Save(canvas, definition.GetProperty("output"));
        }
    }

    /// <summary>
    /// 对只需合并独立 Alpha 的完整图集执行无损复制，保留弹幕图集的原始像素坐标。
    /// </summary>
    private void BuildCopies(string sourceRoot, JsonElement definitions)
    {
        foreach (JsonElement definition in definitions.EnumerateArray())
        {
            Save(LoadImage(sourceRoot, definition), definition.GetProperty("output"));
        }
    }

    /// <summary>
    /// 加载颜色图并按可选 `mask` 的红通道覆盖 Alpha，同时登记两者供最终 SHA-256 清单审计。
    /// </summary>
    private Image LoadImage(string sourceRoot, JsonElement definition)
    {
        string relative = definition.GetProperty("source").GetString()!;
        _usedSources.Add(relative);
        Image image = Image.LoadFromFile(ResolveSource(sourceRoot, relative));
        image.Convert(Image.Format.Rgba8);
        if (!definition.TryGetProperty("mask", out JsonElement maskElement))
        {
            return image;
        }

        string maskRelative = maskElement.GetString()!;
        _usedSources.Add(maskRelative);
        Image mask = Image.LoadFromFile(ResolveSource(sourceRoot, maskRelative));
        return MergeAlpha(image, mask);
    }

    /// <summary>
    /// 把清单中的四整数数组转换为 Godot 裁切矩形。
    /// </summary>
    private static Rect2I ReadRect(JsonElement element) => new(
        element[0].GetInt32(), element[1].GetInt32(),
        element[2].GetInt32(), element[3].GetInt32());

    /// <summary>
    /// 把清单中的四个 0-255 通道转换为不透明或半透明 Godot 颜色。
    /// </summary>
    private static Color ReadColor(JsonElement element) => Color.Color8(
        (byte)element[0].GetInt32(), (byte)element[1].GetInt32(),
        (byte)element[2].GetInt32(), (byte)element[3].GetInt32());

    /// <summary>
    /// 将统一使用正斜杠的清单相对路径安全解析到用户提供的素材根目录。
    /// </summary>
    private static string ResolveSource(string sourceRoot, string relative) =>
        Path.Combine(sourceRoot, relative.Replace('/', Path.DirectorySeparatorChar));

    /// <summary>
    /// 确保内部输出目录存在并以 PNG 保存；非 OK 结果会转为异常阻止生成假成功。
    /// </summary>
    private static void Save(Image image, JsonElement output) => Save(image, output.GetString()!);

    /// <summary>
    /// 按相对内部目录保存 PNG，并让调用者能复用于复制与规范化两种流程。
    /// </summary>
    private static void Save(Image image, string relativeOutput)
    {
        string path = ProjectSettings.GlobalizePath(OutputRoot + relativeOutput);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        Error error = image.SavePng(path);
        if (error != Error.Ok)
        {
            throw new IOException($"Could not save internal preview {path}: {error}.");
        }
    }
}
