namespace TouhouWuxiaSurvivor.Tools.TileGenerator;

/// <summary>
/// 提供无外部图像库依赖的 RGBA8 像素画布，用于确定性生成小型 Tile PNG。
/// </summary>
internal sealed class PixelCanvas
{
    private readonly byte[] _pixels;

    /// <summary>
    /// 创建正尺寸画布并分配连续的行优先 RGBA8 缓冲区。
    /// </summary>
    public PixelCanvas(int width, int height)
    {
        if (width <= 0 || height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width));
        }

        Width = width;
        Height = height;
        _pixels = new byte[width * height * 4];
    }

    public int Width { get; }

    public int Height { get; }

    public ReadOnlySpan<byte> Pixels => _pixels;

    /// <summary>
    /// 将画布内所有像素覆盖为同一颜色。
    /// </summary>
    public void Fill(Rgba32 color)
    {
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                SetPixel(x, y, color);
            }
        }
    }

    /// <summary>
    /// 写入指定像素；坐标越界时忽略，便于图案算法处理边缘。
    /// </summary>
    public void SetPixel(int x, int y, Rgba32 color)
    {
        if (x < 0 || y < 0 || x >= Width || y >= Height)
        {
            return;
        }

        int offset = ((y * Width) + x) * 4;
        _pixels[offset] = color.R;
        _pixels[offset + 1] = color.G;
        _pixels[offset + 2] = color.B;
        _pixels[offset + 3] = color.A;
    }

    /// <summary>
    /// 读取指定有效坐标的 RGBA 值；调用者负责保证坐标范围。
    /// </summary>
    public Rgba32 GetPixel(int x, int y)
    {
        int offset = ((y * Width) + x) * 4;
        return new Rgba32(
            _pixels[offset],
            _pixels[offset + 1],
            _pixels[offset + 2],
            _pixels[offset + 3]);
    }

    /// <summary>
    /// 以整数最近邻倍率复制源画布，不引入插值颜色，保持像素画硬边缘。
    /// </summary>
    public void BlitNearest(PixelCanvas source, int targetX, int targetY, int scale)
    {
        if (scale <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(scale));
        }

        for (int sourceY = 0; sourceY < source.Height; sourceY++)
        {
            for (int sourceX = 0; sourceX < source.Width; sourceX++)
            {
                Rgba32 color = source.GetPixel(sourceX, sourceY);
                for (int offsetY = 0; offsetY < scale; offsetY++)
                {
                    for (int offsetX = 0; offsetX < scale; offsetX++)
                    {
                        SetPixel(
                            targetX + (sourceX * scale) + offsetX,
                            targetY + (sourceY * scale) + offsetY,
                            color);
                    }
                }
            }
        }
    }
}
