using Godot;

namespace TouhouWuxiaSurvivor.Ui.Map;

/// <summary>
/// 保存全屏地图的中心坐标、语义缩放级别和亚 Tile 拖动余量。
/// 该类不依赖场景树，便于独立验证地图导航算法。
/// </summary>
public sealed class MapViewState
{
    private static readonly MapRenderScale[] ZoomLevels =
    [
        new(8, 1),
        new(4, 1),
        new(2, 1),
        new(1, 1),
        new(1, 2),
        new(1, 4),
        new(1, 8),
        new(1, 16),
    ];
    private Vector2 _dragRemainder;
    private int _zoomIndex = 6;

    public long CenterTileX { get; private set; }

    public long CenterTileY { get; private set; }

    public MapRenderScale Scale => ZoomLevels[_zoomIndex];

    public float PixelsPerTile => Scale.PixelsPerTile;

    /// <summary>
    /// 把视图中心直接设置到指定绝对 Tile，通常用于打开地图或按 F 回到玩家。
    /// </summary>
    public void Recenter(long tileX, long tileY)
    {
        CenterTileX = tileX;
        CenterTileY = tileY;
    }

    /// <summary>
    /// 开始一次新的指针拖动并清除上次不足一个 Tile 的像素余量。
    /// </summary>
    public void BeginPointerDrag() => _dragRemainder = Vector2.Zero;

    /// <summary>
    /// 使用绝对 Tile 增量平移地图中心，long 坐标允许地图在无限世界中持续移动。
    /// </summary>
    public void PanTiles(long x, long y)
    {
        CenterTileX += x;
        CenterTileY += y;
    }

    /// <summary>
    /// 累积鼠标像素位移并换算成整数 Tile 平移；产生可见移动时返回 true。
    /// </summary>
    public bool PanPixels(Vector2 relative)
    {
        _dragRemainder += relative;
        long x = (long)(_dragRemainder.X / PixelsPerTile);
        long y = (long)(_dragRemainder.Y / PixelsPerTile);
        if (x == 0 && y == 0)
        {
            return false;
        }

        _dragRemainder -= new Vector2(x * PixelsPerTile, y * PixelsPerTile);
        PanTiles(-x, -y);
        return true;
    }

    /// <summary>
    /// 在预设缩放级别间移动并钳制边界；缩放实际变化时返回 true。
    /// </summary>
    public bool ChangeZoom(int direction)
    {
        int next = Math.Clamp(_zoomIndex + direction, 0, ZoomLevels.Length - 1);
        if (next == _zoomIndex)
        {
            return false;
        }

        _zoomIndex = next;
        return true;
    }
}
