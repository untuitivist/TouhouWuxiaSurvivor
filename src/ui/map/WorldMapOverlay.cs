using Godot;
using TouhouWuxiaSurvivor.World.Biomes;
using TouhouWuxiaSurvivor.World.Coordinates;
using TouhouWuxiaSurvivor.World.Map;
using TouhouWuxiaSurvivor.World.Structures;

namespace TouhouWuxiaSurvivor.Ui.Map;

/// <summary>
/// 提供旅行地图式全屏界面：显示已探索 Tile、结构细节、区块参考线和玩家位置。
/// 负责 Godot 输入与绘制，探索数据、视图状态和纹理生成分别委托给独立对象。
/// </summary>
public partial class WorldMapOverlay : Control
{
    private static readonly Color BackgroundColor = new("0b0f11");
    private static readonly Color ChunkGridColor = new(0.85f, 0.9f, 0.88f, 0.16f);
    private readonly WorldMapTextureBuilder _textureBuilder = new();
    private readonly MapViewState _view = new();
    private ExploredMapStore? _exploredMap;
    private MapLabelLayer? _labelLayer;
    private Vector2 _lastSize;
    private long _playerTileX;
    private long _playerTileY;
    private bool _dragging;
    private bool _wasPaused;

    public float PixelsPerTile => _view.PixelsPerTile;
    public bool HasRenderedTexture => _textureBuilder.Texture is not null;
    public int VisibleBiomeLabelCount => _labelLayer?.BiomeLabelCount ?? 0;
    public int VisibleStructureLabelCount => _labelLayer?.StructureLabelCount ?? 0;
    public bool InputBlocked { get; set; }

    /// <summary>
    /// 初始化始终处理模式、最近邻纹理过滤和输入接收，并让地图默认隐藏。
    /// </summary>
    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        MouseFilter = MouseFilterEnum.Stop;
        TextureFilter = TextureFilterEnum.Nearest;
        _labelLayer = GetNode<MapLabelLayer>("Labels");
        SetProcessUnhandledInput(true);
        Hide();
    }

    /// <summary>
    /// 地图可见时监视视口尺寸变化，保证窗口缩放后纹理覆盖完整画面。
    /// </summary>
    public override void _Process(double delta)
    {
        if (Visible && Size != _lastSize)
        {
            RebuildTexture();
        }
    }

    /// <summary>
    /// 处理不会依赖鼠标命中测试的地图快捷键，并消费事件以阻止底层角色响应。
    /// </summary>
    public override void _UnhandledInput(InputEvent inputEvent)
    {
        if (InputBlocked)
        {
            return;
        }

        if (inputEvent.IsActionPressed("toggle_map"))
        {
            SetOpen(!Visible);
            GetViewport().SetInputAsHandled();
            return;
        }

        if (!Visible)
        {
            return;
        }

        if (inputEvent.IsActionPressed("pause_menu"))
        {
            SetOpen(false);
        }
        else if (inputEvent.IsActionPressed("map_recenter"))
        {
            Recenter();
        }
        else if (inputEvent.IsActionPressed("ui_left"))
        {
            PanTiles(-16, 0);
        }
        else if (inputEvent.IsActionPressed("ui_right"))
        {
            PanTiles(16, 0);
        }
        else if (inputEvent.IsActionPressed("ui_up"))
        {
            PanTiles(0, -16);
        }
        else if (inputEvent.IsActionPressed("ui_down"))
        {
            PanTiles(0, 16);
        }
        else
        {
            return;
        }

        GetViewport().SetInputAsHandled();
    }

    /// <summary>
    /// 处理地图表面的左键拖动和滚轮缩放输入。
    /// </summary>
    public override void _GuiInput(InputEvent inputEvent)
    {
        if (inputEvent is InputEventMouseButton button)
        {
            HandleMouseButton(button);
        }
        else if (inputEvent is InputEventMouseMotion motion && _dragging)
        {
            _labelLayer?.UpdatePointer(motion.Position);
            if (_view.PanPixels(motion.Relative))
            {
                RebuildTexture();
            }
            AcceptEvent();
        }
        else if (inputEvent is InputEventMouseMotion pointerMotion)
        {
            _labelLayer?.UpdatePointer(pointerMotion.Position);
        }
    }

    /// <summary>
    /// 按顺序绘制背景、Tile 纹理、低对比度区块网格和玩家标记。
    /// </summary>
    public override void _Draw()
    {
        DrawRect(new Rect2(Vector2.Zero, Size), BackgroundColor);
        if (_textureBuilder.Texture is null)
        {
            return;
        }

        var mapSize = new Vector2(
            _textureBuilder.Width * PixelsPerTile,
            _textureBuilder.Height * PixelsPerTile);
        DrawTextureRect(_textureBuilder.Texture, new Rect2(Vector2.Zero, mapSize), false);
        DrawChunkGrid();
        DrawPlayerMarker();
    }

    /// <summary>
    /// 节点退出时恢复打开地图前的暂停状态，防止切换场景后游戏意外保持暂停。
    /// </summary>
    public override void _ExitTree()
    {
        if (Visible)
        {
            GetTree().Paused = _wasPaused;
        }
    }

    /// <summary>
    /// 注入探索数据、群系规则和共享结构选址器，地图本身不重新生成地形或结构。
    /// </summary>
    public void Configure(
        ExploredMapStore exploredMap,
        BiomeSelector biomes,
        StructureLocator structures)
    {
        _exploredMap = exploredMap;
        _labelLayer!.Configure(exploredMap, biomes, structures);
    }
    /// <summary>
    /// 更新玩家的绝对 Tile 坐标，供重新居中和玩家标记绘制使用。
    /// </summary>
    public void SetPlayerTile(long tileX, long tileY) => (_playerTileX, _playerTileY) = (tileX, tileY);

    /// <summary>
    /// 关闭地图并恢复打开前的暂停状态；地图已经关闭时不产生副作用。
    /// </summary>
    public void Close() => SetOpen(false);

    /// <summary>
    /// 从其他互斥覆盖层显式打开地图，并复用同一暂停所有权与纹理刷新流程。
    /// </summary>
    public void Open() => SetOpen(true);

    /// <summary>
    /// 将鼠标按键转换为拖动生命周期或离散缩放操作，并消费已处理事件。
    /// </summary>
    private void HandleMouseButton(InputEventMouseButton button)
    {
        _labelLayer?.UpdatePointer(button.Position);
        if (button.ButtonIndex == MouseButton.Left)
        {
            _dragging = button.Pressed;
            if (_dragging)
            {
                _view.BeginPointerDrag();
            }
            AcceptEvent();
        }
        else if (button.Pressed && button.ButtonIndex == MouseButton.WheelUp)
        {
            ChangeZoom(1);
            AcceptEvent();
        }
        else if (button.Pressed && button.ButtonIndex == MouseButton.WheelDown)
        {
            ChangeZoom(-1);
            AcceptEvent();
        }
    }

    /// <summary>
    /// 以 Tile 为单位平移视图并立即重建当前可见纹理。
    /// </summary>
    private void PanTiles(long x, long y)
    {
        _view.PanTiles(x, y);
        RebuildTexture();
    }
    /// <summary>
    /// 请求改变离散缩放级别，仅在级别实际变化时重建纹理。
    /// </summary>
    private void ChangeZoom(int direction)
    {
        if (_view.ChangeZoom(direction))
        {
            RebuildTexture();
        }
    }
    /// <summary>
    /// 将地图中心恢复到最新玩家位置，并刷新显示区域。
    /// </summary>
    private void Recenter()
    {
        _view.Recenter(_playerTileX, _playerTileY);
        RebuildTexture();
    }

    /// <summary>
    /// 打开时记录并设置暂停状态，关闭时精确恢复此前状态。
    /// </summary>
    private void SetOpen(bool open)
    {
        if (open == Visible)
        {
            return;
        }

        if (open)
        {
            _wasPaused = GetTree().Paused;
            GetTree().Paused = true;
            Show();
            Recenter();
        }
        else
        {
            _dragging = false;
            Hide();
            GetTree().Paused = _wasPaused;
        }
    }
    /// <summary>
    /// 从探索存储生成当前可见 Tile 纹理，并请求 Godot 重绘控件。
    /// </summary>
    private void RebuildTexture()
    {
        if (_exploredMap is null || Size.X < 1.0f || Size.Y < 1.0f)
        {
            return;
        }

        _lastSize = Size;
        _textureBuilder.Rebuild(
            _exploredMap,
            _view.CenterTileX,
            _view.CenterTileY,
            Size,
            PixelsPerTile);
        _labelLayer!.UpdateView(
            _textureBuilder.LeftTile,
            _textureBuilder.TopTile,
            _textureBuilder.Width,
            _textureBuilder.Height,
            PixelsPerTile);
        QueueRedraw();
    }

    /// <summary>
    /// 根据纹理左上角绝对坐标绘制 32 Tile 间距的弱化区块参考线。
    /// </summary>
    private void DrawChunkGrid()
    {
        long left = _textureBuilder.LeftTile;
        long top = _textureBuilder.TopTile;
        long right = left + _textureBuilder.Width;
        long bottom = top + _textureBuilder.Height;
        long firstX = GridMath.FloorDiv(left, WorldMetrics.ChunkTiles) * WorldMetrics.ChunkTiles;
        long firstY = GridMath.FloorDiv(top, WorldMetrics.ChunkTiles) * WorldMetrics.ChunkTiles;

        for (long x = firstX; x <= right; x += WorldMetrics.ChunkTiles)
        {
            float screenX = (x - left) * PixelsPerTile;
            DrawLine(new Vector2(screenX, 0), new Vector2(screenX, Size.Y), ChunkGridColor);
        }

        for (long y = firstY; y <= bottom; y += WorldMetrics.ChunkTiles)
        {
            float screenY = (y - top) * PixelsPerTile;
            DrawLine(new Vector2(0, screenY), new Vector2(Size.X, screenY), ChunkGridColor);
        }
    }

    /// <summary>
    /// 将玩家绝对坐标投影到地图屏幕坐标；标记不在视口内时跳过绘制。
    /// </summary>
    private void DrawPlayerMarker()
    {
        var position = new Vector2(
            (_playerTileX - _textureBuilder.LeftTile + 0.5f) * PixelsPerTile,
            (_playerTileY - _textureBuilder.TopTile + 0.5f) * PixelsPerTile);
        if (!new Rect2(Vector2.Zero, Size).HasPoint(position))
        {
            return;
        }

        DrawCircle(position, 5.0f, new Color("f4f1df"));
        DrawCircle(position, 3.0f, new Color("dc3f38"));
    }
}
