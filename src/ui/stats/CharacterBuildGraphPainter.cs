using Godot;
using TouhouWuxiaSurvivor.Ui.Stats.Build;

namespace TouhouWuxiaSurvivor.Ui.Stats;

/// <summary>
/// 集中绘制构筑图的泳道、关系线和节点，主控件只保留交互与视图状态。
/// </summary>
public sealed class CharacterBuildGraphPainter
{
    private static readonly string[] LaneNames = ["武学", "心法", "符卡", "特化"];
    private readonly CharacterBuildGraph _canvas;
    private readonly Font _font;

    /// <summary>
    /// 绑定目标画布与字体；绘制过程不保存模型或选择状态。
    /// </summary>
    public CharacterBuildGraphPainter(CharacterBuildGraph canvas, Font font)
    {
        _canvas = canvas;
        _font = font;
    }

    /// <summary>
    /// 绘制深墨背景、四条泳道、必要关系及全部当前可见节点。
    /// </summary>
    public void Draw(
        CharacterBuildViewModel model,
        IReadOnlyDictionary<string, CharacterBuildGraphItem> items,
        string? selectedNodeId,
        float zoom,
        Vector2 pan)
    {
        _canvas.DrawRect(new Rect2(Vector2.Zero, _canvas.Size), new Color("0b100d"));
        DrawLanes(zoom, pan);
        DrawRelations(model, items, selectedNodeId, zoom, pan);
        foreach (CharacterBuildGraphItem item in items.Values)
        {
            DrawNode(item, selectedNodeId, zoom, pan);
        }
    }

    /// <summary>
    /// 绘制固定语义的泳道标题和低对比度分隔线。
    /// </summary>
    private void DrawLanes(float zoom, Vector2 pan)
    {
        for (int lane = 0; lane < LaneNames.Length; lane++)
        {
            float y = 8.0f + lane * 52.0f;
            _canvas.DrawLine(Transform(new Vector2(4.0f, y + 38.0f), zoom, pan),
                Transform(new Vector2(2000.0f, y + 38.0f), zoom, pan),
                new Color(0.27f, 0.33f, 0.28f, 0.42f), 1.0f);
            _canvas.DrawString(_font, Transform(new Vector2(6.0f, y + 21.0f), zoom, pan),
                LaneNames[lane], HorizontalAlignment.Left, -1.0f,
                ScaleFont(10, zoom), new Color("96a18f"));
        }
    }

    /// <summary>
    /// 只绘制已得路径或当前选中节点的直接关系，减少完整内容目录造成的线网噪声。
    /// </summary>
    private void DrawRelations(
        CharacterBuildViewModel model,
        IReadOnlyDictionary<string, CharacterBuildGraphItem> items,
        string? selectedNodeId,
        float zoom,
        Vector2 pan)
    {
        foreach (CharacterBuildRelationView relation in model.Relations)
        {
            if (!items.TryGetValue(relation.FromNodeId, out var from) ||
                !items.TryGetValue(relation.ToNodeId, out var to))
            {
                continue;
            }

            bool related = selectedNodeId == relation.FromNodeId ||
                selectedNodeId == relation.ToNodeId;
            if (!related && !(from.Node.IsLearned && to.Node.IsLearned))
            {
                continue;
            }

            Color color = relation.Kind == CharacterBuildRelationKind.Exclusion
                ? new Color(0.55f, 0.24f, 0.2f, related ? 0.95f : 0.42f)
                : new Color(0.69f, 0.57f, 0.31f, related ? 0.95f : 0.42f);
            Vector2 start = from.Rect.GetCenter();
            Vector2 end = to.Rect.GetCenter();
            Vector2 corner = new(end.X, start.Y);
            _canvas.DrawLine(Transform(start, zoom, pan), Transform(corner, zoom, pan),
                color, related ? 2.0f : 1.0f);
            _canvas.DrawLine(Transform(corner, zoom, pan), Transform(end, zoom, pan),
                color, related ? 2.0f : 1.0f);
        }
    }

    /// <summary>
    /// 绘制节点状态底、选中边框、重数与短名称，锁定态用文字补充颜色信息。
    /// </summary>
    private void DrawNode(
        CharacterBuildGraphItem item,
        string? selectedNodeId,
        float zoom,
        Vector2 pan)
    {
        Rect2 rect = Transform(item.Rect, zoom, pan);
        CharacterBuildNodeView node = item.Node;
        bool selected = selectedNodeId == node.Id;
        _canvas.DrawRect(rect, CharacterBuildNodePalette.Fill(node));
        _canvas.DrawRect(rect,
            selected ? new Color("f0cf74") : CharacterBuildNodePalette.Border(node),
            false, selected ? 2.0f : 1.0f);
        string marker = CharacterBuildNodeStateText.GetMarker(node);
        _canvas.DrawString(_font, rect.Position + new Vector2(5.0f, 12.0f * zoom), marker,
            HorizontalAlignment.Left, -1.0f, ScaleFont(9, zoom),
            CharacterBuildNodePalette.Text(node));
        string name = node.DisplayName.Length > 6 ? node.DisplayName[..6] : node.DisplayName;
        _canvas.DrawString(_font, rect.Position + new Vector2(5.0f, 27.0f * zoom), name,
            HorizontalAlignment.Left, rect.Size.X - 8.0f, ScaleFont(10, zoom),
            CharacterBuildNodePalette.Text(node));
    }

    /// <summary>将图谱局部点按缩放与平移转换到控件坐标。</summary>
    private static Vector2 Transform(Vector2 point, float zoom, Vector2 pan) =>
        point * zoom + pan;

    /// <summary>将图谱局部矩形按缩放与平移转换到控件坐标。</summary>
    private static Rect2 Transform(Rect2 rect, float zoom, Vector2 pan) =>
        new(Transform(rect.Position, zoom, pan), rect.Size * zoom);

    /// <summary>按当前缩放返回仍可阅读的字体大小。</summary>
    private static int ScaleFont(int size, float zoom) =>
        Math.Max(7, (int)MathF.Round(size * zoom));
}
