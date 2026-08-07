namespace TouhouWuxiaSurvivor.Ui.Map;

/// <summary>
/// 表示一个待绘制地图名称的类别、文本和绝对 Tile 锚点。
/// </summary>
public readonly record struct MapLabel(
    MapLabelKind Kind,
    string Text,
    long TileX,
    long TileY);
