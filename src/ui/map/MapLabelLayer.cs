using Godot;
using TouhouWuxiaSurvivor.World.Biomes;
using TouhouWuxiaSurvivor.World.Map;
using TouhouWuxiaSurvivor.World.Structures;

namespace TouhouWuxiaSurvivor.Ui.Map;

/// <summary>
/// 在地图纹理上方绘制群系与结构名称，并通过矩形碰撞避让保持文字可读。
/// </summary>
public partial class MapLabelLayer : Control
{
    private static readonly Color BiomeText = new("d8e5ce");
    private static readonly Color BiomeBackground = new(0.04f, 0.09f, 0.055f, 0.88f);
    private static readonly Color StructureText = new("f2d995");
    private static readonly Color StructureBackground = new(0.16f, 0.065f, 0.045f, 0.92f);
    private IReadOnlyList<MapLabel> _labels = [];
    private ExploredMapStore? _exploredMap;
    private MapLabelProvider? _provider;
    private MapLabel? _hoveredBiome;
    private Vector2 _pointerPosition;
    private long _leftTile;
    private long _topTile;
    private float _pixelsPerTile;
    private bool _hasPointer;

    public int LabelCount => _labels.Count + (_hoveredBiome.HasValue ? 1 : 0);

    public int BiomeLabelCount => _hoveredBiome.HasValue ? 1 : 0;

    public int StructureLabelCount => _labels.Count(label => label.Kind == MapLabelKind.Structure);

    /// <summary>
    /// 初始化最近邻绘制和鼠标穿透，使标签不拦截地图拖动与滚轮事件。
    /// </summary>
    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        TextureFilter = TextureFilterEnum.Nearest;
    }

    /// <summary>
    /// 注入标签所需的已探索地图、群系规则和共享结构选址器。
    /// </summary>
    public void Configure(
        ExploredMapStore exploredMap,
        DiscoveredStructureStore structures)
    {
        _exploredMap = exploredMap;
        _provider = new MapLabelProvider(structures);
    }

    /// <summary>
    /// 更新地图投影参数并重新生成当前视口标签集合。
    /// </summary>
    public void UpdateView(long left, long top, long width, long height, float pixelsPerTile)
    {
        if (_provider is null)
        {
            return;
        }

        _leftTile = left;
        _topTile = top;
        _pixelsPerTile = pixelsPerTile;
        _labels = _provider.Build(left, top, width, height);
        if (_hasPointer)
        {
            UpdatePointer(_pointerPosition);
        }
        QueueRedraw();
    }

    /// <summary>
    /// 将鼠标位置换算为绝对 Tile；已探索时生成跟随光标的群系标签，否则隐藏标签。
    /// </summary>
    public void UpdatePointer(Vector2 localPosition)
    {
        _hasPointer = true;
        _pointerPosition = localPosition;
        _hoveredBiome = null;
        if (_exploredMap is null || _pixelsPerTile <= 0.0f ||
            !new Rect2(Vector2.Zero, Size).HasPoint(localPosition))
        {
            QueueRedraw();
            return;
        }

        long tileX = _leftTile + Mathf.FloorToInt(localPosition.X / _pixelsPerTile);
        long tileY = _topTile + Mathf.FloorToInt(localPosition.Y / _pixelsPerTile);
        if (_exploredMap.TryGetBiome(tileX, tileY, out BiomeId biome))
        {
            _hoveredBiome = new MapLabel(
                MapLabelKind.Biome,
                $"群系 · {BiomeNames.GetChinese(biome)}",
                tileX,
                tileY);
        }

        QueueRedraw();
    }

    /// <summary>
    /// 先绘制固定结构名称，再在最上层绘制跟随鼠标的群系提示。
    /// </summary>
    public override void _Draw()
    {
        var occupied = new List<Rect2>();
        foreach (MapLabel label in _labels)
        {
            DrawLabel(label, occupied);
        }

        if (_hoveredBiome is { } biome)
        {
            DrawBiomeTooltip(biome);
        }
    }

    /// <summary>
    /// 将绝对锚点投影到屏幕，寻找不重叠位置并绘制锚点、底板与文字。
    /// </summary>
    private void DrawLabel(MapLabel label, List<Rect2> occupied)
    {
        Font font = ThemeDB.FallbackFont;
        int fontSize = label.Kind == MapLabelKind.Structure ? 13 : 11;
        Vector2 textSize = font.GetStringSize(label.Text, HorizontalAlignment.Left, -1, fontSize);
        Vector2 anchor = new(
            (label.TileX - _leftTile + 0.5f) * _pixelsPerTile,
            (label.TileY - _topTile + 0.5f) * _pixelsPerTile);
        Rect2? placement = FindPlacement(anchor, textSize, occupied);
        if (placement is not { } textRect)
        {
            return;
        }

        Color textColor = label.Kind == MapLabelKind.Structure ? StructureText : BiomeText;
        Color background = label.Kind == MapLabelKind.Structure
            ? StructureBackground
            : BiomeBackground;
        DrawCircle(anchor, label.Kind == MapLabelKind.Structure ? 2.5f : 1.5f, textColor);
        DrawRect(textRect.Grow(3.0f), background);
        DrawString(
            font,
            textRect.Position + new Vector2(0, font.GetAscent(fontSize)),
            label.Text,
            HorizontalAlignment.Left,
            -1,
            fontSize,
            textColor);
        occupied.Add(textRect.Grow(6.0f));
    }

    /// <summary>
    /// 在鼠标右下方绘制必定可见的群系提示，并在视口边缘自动反向收纳。
    /// </summary>
    private void DrawBiomeTooltip(MapLabel label)
    {
        Font font = ThemeDB.FallbackFont;
        const int fontSize = 12;
        Vector2 textSize = font.GetStringSize(label.Text, HorizontalAlignment.Left, -1, fontSize);
        Vector2 position = _pointerPosition + new Vector2(13.0f, 15.0f);
        position.X = Mathf.Clamp(position.X, 6.0f, Math.Max(6.0f, Size.X - textSize.X - 6.0f));
        position.Y = Mathf.Clamp(position.Y, 6.0f, Math.Max(6.0f, Size.Y - textSize.Y - 6.0f));
        var textRect = new Rect2(position, textSize);
        DrawRect(textRect.Grow(4.0f), BiomeBackground);
        DrawString(
            font,
            position + new Vector2(0, font.GetAscent(fontSize)),
            label.Text,
            HorizontalAlignment.Left,
            -1,
            fontSize,
            BiomeText);
    }

    /// <summary>
    /// 尝试锚点上方、下方和更高位置，返回第一个位于视口内且不与已有标签相交的矩形。
    /// </summary>
    private Rect2? FindPlacement(Vector2 anchor, Vector2 textSize, IReadOnlyList<Rect2> occupied)
    {
        float[] verticalOffsets = [-textSize.Y - 9.0f, 9.0f, -textSize.Y - 27.0f];
        foreach (float offset in verticalOffsets)
        {
            var rectangle = new Rect2(
                new Vector2(anchor.X - textSize.X * 0.5f, anchor.Y + offset),
                textSize);
            rectangle.Position = new Vector2(
                Mathf.Clamp(rectangle.Position.X, 6.0f, Math.Max(6.0f, Size.X - textSize.X - 6.0f)),
                Mathf.Clamp(rectangle.Position.Y, 6.0f, Math.Max(6.0f, Size.Y - textSize.Y - 6.0f)));
            if (!occupied.Any(other => other.Intersects(rectangle.Grow(4.0f))))
            {
                return rectangle;
            }
        }

        return null;
    }
}
