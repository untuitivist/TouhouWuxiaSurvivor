namespace TouhouWuxiaSurvivor.Ui.Map;

/// <summary>
/// 描述旅行地图一次纹理采样覆盖的世界 Tile 数，以及该采样在屏幕占用的像素数。
/// </summary>
public readonly record struct MapRenderScale(int TilesPerSample, int PixelsPerSample)
{
    public float PixelsPerTile => PixelsPerSample / (float)TilesPerSample;
}
