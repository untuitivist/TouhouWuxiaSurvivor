using Godot;
using TouhouWuxiaSurvivor.World.Biomes;
using TouhouWuxiaSurvivor.World.Map;
using TouhouWuxiaSurvivor.World.Tiles;

namespace TouhouWuxiaSurvivor.Ui.Map;

/// <summary>
/// 把当前视口覆盖到的绝对区域转换为有界地图纹理；远景以多 Tile 合并为一个采样点。
/// </summary>
public sealed class WorldMapTextureBuilder
{
    public ImageTexture? Texture { get; private set; }

    public int Width { get; private set; }

    public int Height { get; private set; }

    public long LeftTile { get; private set; }

    public long TopTile { get; private set; }

    public int TilesPerSample { get; private set; } = 1;

    public int PixelsPerSample { get; private set; } = 1;

    public long SpanTilesX => (long)Width * TilesPerSample;

    public long SpanTilesY => (long)Height * TilesPerSample;

    /// <summary>
    /// 根据地图中心、视口尺寸和缩放级别重建 RGBA8 纹理，并记录纹理对应的左上角绝对坐标。
    /// 当尺寸不变时复用 ImageTexture，避免连续平移时重复分配 GPU 资源。
    /// </summary>
    public void Rebuild(
        ExploredMapStore exploredMap,
        long centerTileX,
        long centerTileY,
        Vector2 viewportSize,
        MapRenderScale scale)
    {
        TilesPerSample = Math.Max(1, scale.TilesPerSample);
        PixelsPerSample = Math.Max(1, scale.PixelsPerSample);
        Width = Math.Max(1, Mathf.CeilToInt(viewportSize.X / PixelsPerSample));
        Height = Math.Max(1, Mathf.CeilToInt(viewportSize.Y / PixelsPerSample));
        LeftTile = centerTileX - SpanTilesX / 2;
        TopTile = centerTileY - SpanTilesY / 2;
        var pixels = new byte[Width * Height * 4];

        for (int y = 0; y < Height; y++)
        {
            long worldY = TopTile + (long)y * TilesPerSample + TilesPerSample / 2;
            for (int x = 0; x < Width; x++)
            {
                long worldX = LeftTile + (long)x * TilesPerSample + TilesPerSample / 2;
                if (exploredMap.TryGetCell(worldX, worldY, out ExploredMapCell cell))
                {
                    MapColorPalette.WritePixel(
                        pixels, (y * Width + x) * 4, cell.Tile, cell.Biome);
                }
                else
                {
                    MapColorPalette.WritePixel(
                        pixels, (y * Width + x) * 4, null, null);
                }
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
