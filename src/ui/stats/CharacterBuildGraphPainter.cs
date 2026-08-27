using Godot;
using TouhouWuxiaSurvivor.Ui.Stats.Build;

namespace TouhouWuxiaSurvivor.Ui.Stats;

/// <summary>
/// 集中绘制构筑图的泳道、关系线和节点，主控件只保留交互与视图状态。
/// </summary>
public sealed class CharacterBuildGraphPainter
{
    private static readonly string[] LaneNames = ["武学", "心法", "符卡", "特化"];
    private static readonly Color CanvasColor = new("0b100d");
    private static readonly Color CanvasBand = new(0.08f, 0.13f, 0.095f, 0.46f);
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
        _canvas.DrawRect(new Rect2(Vector2.Zero, _canvas.Size), CanvasColor);
        DrawPaperGrid();
        DrawLanes(zoom, pan);
        DrawRelations(model, items, selectedNodeId, zoom, pan);
        foreach (CharacterBuildGraphItem item in items.Values)
        {
            DrawNode(item, selectedNodeId, zoom, pan);
        }
    }

    /// <summary>
    /// 在固定屏幕空间绘制极低对比纸格与零散纤维点，让空白区域保持材质感而不干扰关系线。
    /// </summary>
    private void DrawPaperGrid()
    {
        for (int x = 24; x < _canvas.Size.X; x += 48)
        {
            _canvas.DrawLine(new Vector2(x, 0), new Vector2(x, _canvas.Size.Y),
                new Color(0.18f, 0.25f, 0.18f, 0.12f), 1.0f);
        }

        for (int y = 19; y < _canvas.Size.Y; y += 37)
        {
            _canvas.DrawRect(new Rect2((y * 7) % Math.Max(1.0f, _canvas.Size.X - 8), y, 5, 1),
                new Color(0.48f, 0.46f, 0.31f, 0.16f));
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
            if (lane % 2 == 0)
            {
                Rect2 band = Transform(new Rect2(0.0f, y - 7.0f, 2000.0f, 45.0f), zoom, pan);
                _canvas.DrawRect(band, CanvasBand);
            }

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
            float width = related ? 2.0f : 1.0f;
            _canvas.DrawLine(Transform(start, zoom, pan), Transform(corner, zoom, pan),
                new Color(0.02f, 0.035f, 0.025f, 0.9f), width + 2.0f);
            _canvas.DrawLine(Transform(corner, zoom, pan), Transform(end, zoom, pan),
                new Color(0.02f, 0.035f, 0.025f, 0.9f), width + 2.0f);
            _canvas.DrawLine(Transform(start, zoom, pan), Transform(corner, zoom, pan),
                color, width);
            _canvas.DrawLine(Transform(corner, zoom, pan), Transform(end, zoom, pan),
                color, width);
            Vector2 joint = Transform(corner, zoom, pan);
            _canvas.DrawRect(new Rect2(joint - Vector2.One, new Vector2(3, 3)), color);
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
        Color border = selected ? new Color("f0cf74") : CharacterBuildNodePalette.Border(node);
        DrawNodeFrame(rect, CharacterBuildNodePalette.Fill(node), border, selected);
        string marker = CharacterBuildNodeStateText.GetMarker(node);
        _canvas.DrawString(_font, rect.Position + new Vector2(8.0f, 12.0f * zoom), marker,
            HorizontalAlignment.Left, -1.0f, ScaleFont(9, zoom),
            CharacterBuildNodePalette.Text(node));
        float titleWidth = Math.Max(8.0f, rect.Size.X - 14.0f);
        (string title, int titleSize) = CharacterBuildNodeTitleLayout.Fit(
            _font, node.DisplayName, titleWidth, ScaleFont(10, zoom), ScaleFont(7, zoom));
        _canvas.DrawString(_font, rect.Position + new Vector2(8.0f, 27.0f * zoom), title,
            HorizontalAlignment.Left, titleWidth, titleSize,
            CharacterBuildNodePalette.Text(node));
    }

    /// <summary>
    /// 绘制节点的偏移阴影、双层削角边、顶部反光和左侧状态签，并保持原矩形命中范围不变。
    /// </summary>
    private void DrawNodeFrame(Rect2 rect, Color fill, Color border, bool selected)
    {
        float cut = Math.Max(2.0f, MathF.Round(3.0f * Math.Min(rect.Size.Y / 36.0f, 1.0f)));
        _canvas.DrawRect(new Rect2(rect.Position + new Vector2(2, 2), rect.Size),
            new Color(0.01f, 0.02f, 0.014f, 0.86f));
        _canvas.DrawRect(rect, fill);
        CutNodeCorners(rect, cut);
        _canvas.DrawLine(rect.Position + new Vector2(cut, 0),
            new Vector2(rect.End.X - cut, rect.Position.Y), border, selected ? 2.0f : 1.0f);
        _canvas.DrawLine(new Vector2(rect.Position.X + cut, rect.End.Y),
            rect.End - new Vector2(cut, 0), border.Darkened(0.25f), selected ? 2.0f : 1.0f);
        _canvas.DrawLine(rect.Position + new Vector2(0, cut),
            new Vector2(rect.Position.X, rect.End.Y - cut), border, 1.0f);
        _canvas.DrawLine(new Vector2(rect.End.X, rect.Position.Y + cut),
            rect.End - new Vector2(0, cut), border, 1.0f);
        _canvas.DrawLine(rect.Position + new Vector2(cut + 2, 3),
            new Vector2(rect.End.X - cut - 2, rect.Position.Y + 3),
            fill.Lightened(0.18f), 1.0f);
        _canvas.DrawRect(new Rect2(rect.Position + new Vector2(3, 5),
            new Vector2(2, Math.Max(4.0f, rect.Size.Y - 10))), border);
    }

    /// <summary>
    /// 用画布底色擦去四个角的方形像素，形成传统木牌式硬削角而非平滑圆角。
    /// </summary>
    private void CutNodeCorners(Rect2 rect, float cut)
    {
        _canvas.DrawRect(new Rect2(rect.Position, new Vector2(cut, cut)), CanvasColor);
        _canvas.DrawRect(new Rect2(new Vector2(rect.End.X - cut, rect.Position.Y),
            new Vector2(cut, cut)), CanvasColor);
        _canvas.DrawRect(new Rect2(new Vector2(rect.Position.X, rect.End.Y - cut),
            new Vector2(cut, cut)), CanvasColor);
        _canvas.DrawRect(new Rect2(rect.End - new Vector2(cut, cut),
            new Vector2(cut, cut)), CanvasColor);
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
