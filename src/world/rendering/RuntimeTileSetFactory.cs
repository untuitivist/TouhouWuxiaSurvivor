using Godot;
using TouhouWuxiaSurvivor.World.Coordinates;
using TouhouWuxiaSurvivor.World.Tiles;

namespace TouhouWuxiaSurvivor.World.Rendering;

/// <summary>
/// 从 TileCatalog 动态构造 Godot TileSet，避免维护易产生资源 ID 漂移的手写 .tres 文件。
/// </summary>
public static class RuntimeTileSetFactory
{
    /// <summary>
    /// 为每个 TileId 加载一张 16×16 PNG，并用枚举整数作为稳定的 TileSet source id。
    /// </summary>
    public static TileSet Create()
    {
        var tileSet = new TileSet
        {
            TileSize = new Vector2I(WorldMetrics.TilePixels, WorldMetrics.TilePixels)
        };

        foreach (TileId tile in Enum.GetValues<TileId>())
        {
            var source = new TileSetAtlasSource
            {
                Texture = GD.Load<Texture2D>(TileCatalog.GetResourcePath(tile)),
                TextureRegionSize = new Vector2I(WorldMetrics.TilePixels, WorldMetrics.TilePixels),
                UseTexturePadding = true
            };
            source.CreateTile(Vector2I.Zero);
            tileSet.AddSource(source, (int)tile);
        }

        return tileSet;
    }
}
