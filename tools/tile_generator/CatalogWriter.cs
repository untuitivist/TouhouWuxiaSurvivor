using System.Text.Json;

namespace TouhouWuxiaSurvivor.Tools.TileGenerator;

/// <summary>
/// 协调 Tile 定义校验、逐张 PNG 输出、JSON 清单输出和总览图生成。
/// </summary>
internal sealed class CatalogWriter
{
    private const int PreviewColumns = 8;
    private const int PreviewScale = 4;
    private const int PreviewGap = 4;

    /// <summary>
    /// 在目标目录生成全部 Tile 资产及索引文件，并返回成功生成的 Tile 数量。
    /// </summary>
    public int Generate(string outputRoot, IReadOnlyList<TileSpec> specs)
    {
        ValidateSpecs(specs);
        Directory.CreateDirectory(outputRoot);

        TilePainter painter = new();
        List<PixelCanvas> canvases = new(specs.Count);

        foreach (TileSpec spec in specs)
        {
            PixelCanvas canvas = painter.Paint(spec);
            canvases.Add(canvas);

            string outputPath = Path.Combine(outputRoot, spec.Category, $"{spec.Id}.png");
            PngWriter.Write(canvas, outputPath);
        }

        WriteManifest(outputRoot, specs);
        WritePreview(outputRoot, canvases);
        return specs.Count;
    }

    /// <summary>
    /// 校验分类名和 Tile 名均为唯一的 lowercase_ascii_snake_case，防止资源路径不稳定。
    /// </summary>
    private static void ValidateSpecs(IReadOnlyList<TileSpec> specs)
    {
        HashSet<string> ids = new(StringComparer.Ordinal);

        foreach (TileSpec spec in specs)
        {
            if (!IsValidName(spec.Category) || !IsValidName(spec.Id))
            {
                throw new InvalidOperationException(
                    $"Tile names must use lowercase ASCII snake_case: {spec.Category}/{spec.Id}");
            }

            string fullId = $"{spec.Category}.{spec.Id}";
            if (!ids.Add(fullId))
            {
                throw new InvalidOperationException($"Duplicate tile id: {fullId}");
            }
        }
    }

    /// <summary>
    /// 判断名称是否只含小写 ASCII、数字和非首尾下划线。
    /// </summary>
    private static bool IsValidName(string value)
    {
        if (string.IsNullOrEmpty(value) || value[0] == '_' || value[^1] == '_')
        {
            return false;
        }

        foreach (char character in value)
        {
            bool valid = character is >= 'a' and <= 'z'
                || character is >= '0' and <= '9'
                || character == '_';
            if (!valid)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// 用 UTF-8 without BOM 写出机器可读的 Tile 路径、图案和版本清单。
    /// </summary>
    private static void WriteManifest(string outputRoot, IReadOnlyList<TileSpec> specs)
    {
        var manifest = new
        {
            version = 1,
            tile_size_pixels = TilePainter.TileSize,
            naming = "lowercase_ascii_snake_case",
            tiles = specs.Select(spec => new
            {
                id = $"{spec.Category}.{spec.Id}",
                category = spec.Category,
                path = $"{spec.Category}/{spec.Id}.png",
                pattern = spec.Pattern.ToString(),
            }),
        };

        string json = JsonSerializer.Serialize(
            manifest,
            new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(
            Path.Combine(outputRoot, "tile_manifest.json"),
            json + Environment.NewLine,
            new System.Text.UTF8Encoding(false));
    }

    /// <summary>
    /// 将所有 Tile 最近邻放大并排列为带间隔的总览 PNG，便于快速检查色板与图案。
    /// </summary>
    private static void WritePreview(string outputRoot, IReadOnlyList<PixelCanvas> canvases)
    {
        int tileDisplaySize = TilePainter.TileSize * PreviewScale;
        int rows = (int)Math.Ceiling(canvases.Count / (double)PreviewColumns);
        int width = (PreviewColumns * tileDisplaySize) + ((PreviewColumns - 1) * PreviewGap);
        int height = (rows * tileDisplaySize) + ((rows - 1) * PreviewGap);

        PixelCanvas preview = new(width, height);
        preview.Fill(new Rgba32(24, 26, 28));

        for (int index = 0; index < canvases.Count; index++)
        {
            int column = index % PreviewColumns;
            int row = index / PreviewColumns;
            int x = column * (tileDisplaySize + PreviewGap);
            int y = row * (tileDisplaySize + PreviewGap);
            preview.BlitNearest(canvases[index], x, y, PreviewScale);
        }

        PngWriter.Write(preview, Path.Combine(outputRoot, "tile_catalog.png"));
    }
}
