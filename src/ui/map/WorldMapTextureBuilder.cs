using Godot;
using TouhouWuxiaSurvivor.World.Map;
using TouhouWuxiaSurvivor.World.Tiles;

namespace TouhouWuxiaSurvivor.Ui.Map;

/// <summary>
/// 把当前视口覆盖到的绝对 Tile 区域转换为一张一 Tile 一像素的地图纹理。
/// 缩放由显示层完成，因此生成成本只与可见地图范围有关，不依赖世界总尺寸。
/// </summary>
public sealed class WorldMapTextureBuilder
{
    public ImageTexture? Texture { get; private set; }

    public int Width { get; private set; }

    public int Height { get; private set; }

    public long LeftTile { get; private set; }

    public long TopTile { get; private set; }

    /// <summary>
    /// 根据地图中心、视口尺寸和缩放级别重建 RGBA8 纹理，并记录纹理对应的左上角绝对坐标。
    /// 当尺寸不变时复用 ImageTexture，避免连续平移时重复分配 GPU 资源。
    /// </summary>
    public void Rebuild(
        ExploredMapStore exploredMap,
        long centerTileX,
        long centerTileY,
        Vector2 viewportSize,
        float pixelsPerTile)
    {
        Width = Math.Max(1, Mathf.CeilToInt(viewportSize.X / pixelsPerTile));
        Height = Math.Max(1, Mathf.CeilToInt(viewportSize.Y / pixelsPerTile));
        LeftTile = centerTileX - Width / 2;
        TopTile = centerTileY - Height / 2;
        var pixels = new byte[Width * Height * 4];

        for (int y = 0; y < Height; y++)
        {
            long worldY = TopTile + y;
            for (int x = 0; x < Width; x++)
            {
                TileId? tile = exploredMap.TryGetTile(LeftTile + x, worldY, out TileId found)
                    ? found
                    : null;
                MapColorPalette.WritePixel(pixels, (y * Width + x) * 4, tile);
            }
        }

        Image image = Image.CreateFromData(Width, Height, false, Image.Format.Rgba8, pixels);
        if (Texture is not null && Texture.GetWidth() == Width && Texture.GetHeight() == Height)
        {
            Texture.Update(image);
        }
        else
        {
            Texture = ImageTexture.CreateFromImage(image);
        }
    }
}
