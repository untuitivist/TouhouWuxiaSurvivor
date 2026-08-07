namespace TouhouWuxiaSurvivor.Tools.TileGenerator;

/// <summary>
/// 根据 TileSpec 在 16×16 画布上绘制可重复的简易像素点与线条图案。
/// </summary>
internal sealed class TilePainter
{
    public const int TileSize = 16;

    /// <summary>
    /// 先填充底色，再按图案类型调用对应绘制算法，返回完成的像素画布。
    /// </summary>
    public PixelCanvas Paint(TileSpec spec)
    {
        PixelCanvas canvas = new(TileSize, TileSize);
        canvas.Fill(spec.BaseColor);
        DeterministicRandom random = new(spec.Seed);

        switch (spec.Pattern)
        {
            case PatternKind.Speckles:
                PaintSpeckles(canvas, random, spec.AccentA, spec.AccentB, 12);
                break;
            case PatternKind.DenseSpeckles:
                PaintSpeckles(canvas, random, spec.AccentA, spec.AccentB, 28);
                break;
            case PatternKind.Pebbles:
                PaintPebbles(canvas, random, spec.AccentA, spec.AccentB);
                break;
            case PatternKind.Cracks:
                PaintCracks(canvas, random, spec.AccentA, spec.AccentB);
                break;
            case PatternKind.Ripples:
                PaintRipples(canvas, random, spec.AccentA, spec.AccentB);
                break;
            case PatternKind.Leaves:
                PaintLeaves(canvas, random, spec.AccentA, spec.AccentB);
                break;
            case PatternKind.Petals:
                PaintSpeckles(canvas, random, spec.AccentA, spec.AccentB, 8);
                PaintPetals(canvas, random, spec.AccentB);
                break;
            case PatternKind.Sparkles:
                PaintSpeckles(canvas, random, spec.AccentA, spec.AccentB, 8);
                PaintSparkles(canvas, random, spec.AccentB);
                break;
            case PatternKind.Stripes:
                PaintStripes(canvas, random, spec.AccentA, spec.AccentB);
                break;
            case PatternKind.Droplets:
                PaintSpeckles(canvas, random, spec.AccentA, spec.AccentB, 8);
                PaintDroplets(canvas, random, spec.AccentB);
                break;
            case PatternKind.Flowers:
                PaintSpeckles(canvas, random, spec.AccentA, spec.AccentB, 8);
                PaintFlowers(canvas, random, spec.AccentB);
                break;
            case PatternKind.WetStones:
                PaintPebbles(canvas, random, spec.AccentA, spec.AccentB);
                PaintDroplets(canvas, random, spec.AccentB);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(spec.Pattern));
        }

        return canvas;
    }

    /// <summary>
    /// 在随机坐标散布双色单像素斑点，count 控制纹理密度。
    /// </summary>
    private static void PaintSpeckles(
        PixelCanvas canvas,
        DeterministicRandom random,
        Rgba32 accentA,
        Rgba32 accentB,
        int count)
    {
        for (int index = 0; index < count; index++)
        {
            Rgba32 color = index % 4 == 0 ? accentB : accentA;
            canvas.SetPixel(random.Next(TileSize), random.Next(TileSize), color);
        }
    }

    /// <summary>
    /// 在稀疏斑点上叠加五组相邻明暗像素，形成小石子轮廓。
    /// </summary>
    private static void PaintPebbles(
        PixelCanvas canvas,
        DeterministicRandom random,
        Rgba32 accentA,
        Rgba32 accentB)
    {
        PaintSpeckles(canvas, random, accentA, accentB, 10);
        for (int index = 0; index < 5; index++)
        {
            int x = random.Next(TileSize - 1);
            int y = random.Next(TileSize);
            canvas.SetPixel(x, y, accentB);
            canvas.SetPixel(x + 1, y, accentA);
        }
    }

    /// <summary>
    /// 从随机起点绘制三条向下游走的四像素裂纹。
    /// </summary>
    private static void PaintCracks(
        PixelCanvas canvas,
        DeterministicRandom random,
        Rgba32 accentA,
        Rgba32 accentB)
    {
        PaintSpeckles(canvas, random, accentA, accentB, 7);
        for (int crack = 0; crack < 3; crack++)
        {
            int x = random.Next(TileSize);
            int y = random.Next(TileSize);
            for (int step = 0; step < 4; step++)
            {
                canvas.SetPixel(x, y, accentB);
                x = Math.Clamp(x + random.Next(3) - 1, 0, TileSize - 1);
                y = Math.Clamp(y + 1, 0, TileSize - 1);
            }
        }
    }

    /// <summary>
    /// 绘制五条长度与位置变化的水平短线，模拟水面涟漪。
    /// </summary>
    private static void PaintRipples(
        PixelCanvas canvas,
        DeterministicRandom random,
        Rgba32 accentA,
        Rgba32 accentB)
    {
        for (int wave = 0; wave < 5; wave++)
        {
            int x = random.Next(TileSize - 4);
            int y = 1 + random.Next(TileSize - 2);
            int length = 2 + random.Next(4);
            for (int offset = 0; offset < length; offset++)
            {
                canvas.SetPixel(x + offset, y, wave % 3 == 0 ? accentB : accentA);
            }
        }
    }

    /// <summary>
    /// 绘制十组对角双像素叶片，并按固定比例混用两种颜色。
    /// </summary>
    private static void PaintLeaves(
        PixelCanvas canvas,
        DeterministicRandom random,
        Rgba32 accentA,
        Rgba32 accentB)
    {
        for (int leaf = 0; leaf < 10; leaf++)
        {
            int x = random.Next(TileSize - 1);
            int y = random.Next(TileSize - 1);
            Rgba32 color = leaf % 3 == 0 ? accentB : accentA;
            canvas.SetPixel(x, y, color);
            canvas.SetPixel(x + 1, y + 1, color);
        }
    }

    /// <summary>
    /// 散布七组单像素或双像素花瓣，避免覆盖到画布右边界外。
    /// </summary>
    private static void PaintPetals(
        PixelCanvas canvas,
        DeterministicRandom random,
        Rgba32 color)
    {
        for (int petal = 0; petal < 7; petal++)
        {
            int x = random.Next(TileSize - 1);
            int y = random.Next(TileSize);
            canvas.SetPixel(x, y, color);
            if (petal % 3 == 0)
            {
                canvas.SetPixel(x + 1, y, color);
            }
        }
    }

    /// <summary>
    /// 绘制两个五像素十字高光，作为结界或魔力闪烁标记。
    /// </summary>
    private static void PaintSparkles(
        PixelCanvas canvas,
        DeterministicRandom random,
        Rgba32 color)
    {
        for (int sparkle = 0; sparkle < 2; sparkle++)
        {
            int x = 2 + random.Next(TileSize - 4);
            int y = 2 + random.Next(TileSize - 4);
            canvas.SetPixel(x, y, color);
            canvas.SetPixel(x - 1, y, color);
            canvas.SetPixel(x + 1, y, color);
            canvas.SetPixel(x, y - 1, color);
            canvas.SetPixel(x, y + 1, color);
        }
    }

    /// <summary>
    /// 在底层斑点上绘制四条三像素斜纹，用于表现竹林路径纹理。
    /// </summary>
    private static void PaintStripes(
        PixelCanvas canvas,
        DeterministicRandom random,
        Rgba32 accentA,
        Rgba32 accentB)
    {
        PaintSpeckles(canvas, random, accentA, accentB, 6);
        for (int stripe = 0; stripe < 4; stripe++)
        {
            int x = random.Next(TileSize - 3);
            int y = random.Next(TileSize - 3);
            for (int offset = 0; offset < 3; offset++)
            {
                canvas.SetPixel(x + offset, y + offset, accentB);
            }
        }
    }

    /// <summary>
    /// 绘制五组竖直双像素水滴，表现湿草或湿石表面反光。
    /// </summary>
    private static void PaintDroplets(
        PixelCanvas canvas,
        DeterministicRandom random,
        Rgba32 color)
    {
        for (int drop = 0; drop < 5; drop++)
        {
            int x = random.Next(TileSize);
            int y = random.Next(TileSize - 1);
            canvas.SetPixel(x, y, color);
            canvas.SetPixel(x, y + 1, color);
        }
    }

    /// <summary>
    /// 绘制三组三像素 L 形花朵，使山地草面在地图和场景中都可辨认。
    /// </summary>
    private static void PaintFlowers(
        PixelCanvas canvas,
        DeterministicRandom random,
        Rgba32 color)
    {
        for (int flower = 0; flower < 3; flower++)
        {
            int x = 1 + random.Next(TileSize - 2);
            int y = 1 + random.Next(TileSize - 2);
            canvas.SetPixel(x, y, color);
            canvas.SetPixel(x + 1, y, color);
            canvas.SetPixel(x, y + 1, color);
        }
    }
}
