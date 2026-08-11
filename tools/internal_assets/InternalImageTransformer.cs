using Godot;

namespace TouhouWuxiaSurvivor.Tools.InternalAssets;

/// <summary>
/// 提供内部原作素材构建所需的纯图像变换，不负责清单解析、路径解析或文件写入。
/// </summary>
internal static class InternalImageTransformer
{
    /// <summary>
    /// 通过 RGBA8/L8 字节缓冲一次性写入 Alpha，避免逐像素跨 Godot C# 绑定造成数量级性能损失。
    /// </summary>
    internal static Image MergeAlpha(Image image, Image mask)
    {
        if (image.GetSize() != mask.GetSize())
        {
            throw new InvalidDataException(
                $"Color and alpha dimensions differ: {image.GetSize()} / {mask.GetSize()}.");
        }

        image.Convert(Image.Format.Rgba8);
        mask.Convert(Image.Format.L8);
        byte[] rgba = image.GetData();
        byte[] alpha = mask.GetData();
        for (int pixel = 0; pixel < alpha.Length; pixel++)
        {
            rgba[pixel * 4 + 3] = alpha[pixel];
        }

        return Image.CreateFromData(
            image.GetWidth(), image.GetHeight(), false, Image.Format.Rgba8, rgba);
    }

    /// <summary>
    /// 去掉全透明外边，等比缩入 44x44 后放入指定动画帧；空图仍保持透明以暴露错误裁切。
    /// </summary>
    internal static void PlaceSprite(Image strip, Image source, int frame, int verticalOffset)
    {
        Image sprite = FitWithin(CropOpaque(source), new Vector2I(44, 44));
        Vector2I destination = new(
            frame * 48 + (48 - sprite.GetWidth()) / 2,
            (48 - sprite.GetHeight()) / 2 + verticalOffset);
        strip.BlendRect(sprite,
            new Rect2I(0, 0, sprite.GetWidth(), sprite.GetHeight()), destination);
    }

    /// <summary>
    /// 使用 Godot 原生可见区域计算返回最小外接图；全透明输入原样返回，便于测试识别无效帧。
    /// </summary>
    internal static Image CropOpaque(Image image)
    {
        image.Convert(Image.Format.Rgba8);
        Rect2I usedRect = image.GetUsedRect();
        return usedRect.HasArea() ? image.GetRegion(usedRect) : image;
    }

    /// <summary>
    /// 等比缩放到给定边界内且不放大原图，以保持原作像素边缘和合理纹理尺寸。
    /// </summary>
    internal static Image FitWithin(Image image, Vector2I bounds)
    {
        float scale = Math.Min(
            bounds.X / (float)Math.Max(1, image.GetWidth()),
            bounds.Y / (float)Math.Max(1, image.GetHeight()));
        scale = Math.Min(1.0f, scale);
        image.Resize(
            Math.Max(1, Mathf.RoundToInt(image.GetWidth() * scale)),
            Math.Max(1, Mathf.RoundToInt(image.GetHeight() * scale)),
            Image.Interpolation.Nearest);
        return image;
    }

    /// <summary>
    /// 以覆盖模式等比缩放并居中裁成目标尺寸，适用于不应改变宽高比的场景底图。
    /// </summary>
    internal static Image Fit(Image image, Vector2I size)
    {
        float scale = Math.Max(size.X / (float)image.GetWidth(), size.Y / (float)image.GetHeight());
        image.Resize(Mathf.CeilToInt(image.GetWidth() * scale),
            Mathf.CeilToInt(image.GetHeight() * scale), Image.Interpolation.Nearest);
        return image.GetRegion(new Rect2I(
            (image.GetWidth() - size.X) / 2, (image.GetHeight() - size.Y) / 2,
            size.X, size.Y));
    }

    /// <summary>
    /// 建立指定尺寸的透明 RGBA8 图像，供动画条和立绘画布共享统一像素格式。
    /// </summary>
    internal static Image CreateTransparent(Vector2I size)
    {
        Image image = Image.CreateEmpty(size.X, size.Y, false, Image.Format.Rgba8);
        image.Fill(Colors.Transparent);
        return image;
    }

    /// <summary>
    /// 仅把从图像边缘经透明或近白像素可达的近白区域设为透明，保留人物内部眼白与衣服高光。
    /// </summary>
    internal static Image ClearEdgeConnectedNearWhite(Image image, byte distanceFromWhite)
    {
        image.Convert(Image.Format.Rgba8);
        int width = image.GetWidth();
        int height = image.GetHeight();
        byte[] pixels = image.GetData();
        var visited = new bool[width * height];
        var pending = new Queue<int>();
        for (int x = 0; x < width; x++)
        {
            TryEnqueueClearable(pixels, visited, pending, width, height, x, 0, distanceFromWhite);
            TryEnqueueClearable(pixels, visited, pending, width, height, x, height - 1, distanceFromWhite);
        }
        for (int y = 1; y < height - 1; y++)
        {
            TryEnqueueClearable(pixels, visited, pending, width, height, 0, y, distanceFromWhite);
            TryEnqueueClearable(pixels, visited, pending, width, height, width - 1, y, distanceFromWhite);
        }

        while (pending.Count > 0)
        {
            int index = pending.Dequeue();
            int offset = index * 4;
            if (pixels[offset + 3] > 0)
            {
                pixels[offset + 3] = 0;
            }

            int x = index % width;
            int y = index / width;
            TryEnqueueClearable(pixels, visited, pending, width, height, x - 1, y, distanceFromWhite);
            TryEnqueueClearable(pixels, visited, pending, width, height, x + 1, y, distanceFromWhite);
            TryEnqueueClearable(pixels, visited, pending, width, height, x, y - 1, distanceFromWhite);
            TryEnqueueClearable(pixels, visited, pending, width, height, x, y + 1, distanceFromWhite);
        }

        return Image.CreateFromData(width, height, false, Image.Format.Rgba8, pixels);
    }

    /// <summary>
    /// 将边界内尚未访问且透明或各 RGB 通道距白色不超过阈值的像素加入连通区域队列。
    /// </summary>
    private static void TryEnqueueClearable(
        byte[] pixels,
        bool[] visited,
        Queue<int> pending,
        int width,
        int height,
        int x,
        int y,
        byte distanceFromWhite)
    {
        if (x < 0 || x >= width || y < 0 || y >= height)
        {
            return;
        }

        int index = y * width + x;
        int offset = index * 4;
        bool clearable = pixels[offset + 3] == 0 ||
            pixels[offset] >= 255 - distanceFromWhite &&
            pixels[offset + 1] >= 255 - distanceFromWhite &&
            pixels[offset + 2] >= 255 - distanceFromWhite;
        if (visited[index] || !clearable)
        {
            return;
        }

        visited[index] = true;
        pending.Enqueue(index);
    }
}
