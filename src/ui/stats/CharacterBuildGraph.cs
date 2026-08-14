using Godot;
using TouhouWuxiaSurvivor.Ui.Stats.Build;

namespace TouhouWuxiaSurvivor.Ui.Stats;

/// <summary>
/// 绘制可平移缩放的本局构筑关系图，并将点击或键盘选择作为只读事件广播。
/// </summary>
public partial class CharacterBuildGraph : Control
{
    private readonly Dictionary<string, CharacterBuildGraphItem> _items =
        new(StringComparer.Ordinal);
    private CharacterBuildViewModel? _model;
    private CharacterBuildNodeView? _selected;
    private Vector2 _pan;
    private Vector2 _dragOrigin;
    private bool _dragging;
    private float _zoom = 1.0f;
    private Font? _font;
    private CharacterBuildGraphPainter? _painter;

    public string? SelectedNodeId => _selected?.Id;
    public float Zoom => _zoom;
    public int VisibleNodeCount => _items.Count;
    public event Action<CharacterBuildNodeView>? SelectionChanged;

    /// <summary>
    /// 初始化最近邻、鼠标截断和键盘焦点，使图谱输入不会泄漏到暂停后的世界。
    /// </summary>
    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Stop;
        FocusMode = FocusModeEnum.All;
        TextureFilter = TextureFilterEnum.Nearest;
        ClipContents = true;
        _font = ThemeDB.FallbackFont;
        _painter = new CharacterBuildGraphPainter(this, _font);
    }

    /// <summary>
    /// 写入不可变模型及筛选结果，重建稳定布局并默认选择首个已习得或可达节点。
    /// </summary>
    public void SetModel(
        CharacterBuildViewModel model,
        IReadOnlyList<CharacterBuildNodeView> visibleNodes,
        string? preferredNodeId = null)
    {
        _model = model;
        _items.Clear();
        foreach (CharacterBuildGraphItem item in CharacterBuildGraphLayout.Create(visibleNodes))
        {
            _items[item.Node.Id] = item;
        }

        _selected = visibleNodes.FirstOrDefault(node => node.Id == preferredNodeId) ??
            visibleNodes.FirstOrDefault(node => node.IsLearned) ??
            visibleNodes.FirstOrDefault(node => node.IsAvailable) ?? visibleNodes.FirstOrDefault();
        FitContent();
        QueueRedraw();
        if (_selected is not null)
        {
            SelectionChanged?.Invoke(_selected);
        }
    }

    /// <summary>
    /// 供键盘导航、自动化测试与无障碍入口按稳定 ID 选择节点，失败时不改变当前详情。
    /// </summary>
    public bool SelectNode(string nodeId)
    {
        if (!_items.TryGetValue(nodeId, out CharacterBuildGraphItem? item))
        {
            return false;
        }

        Select(item.Node);
        return true;
    }

    /// <summary>
    /// 处理左键点选、空白拖动和滚轮缩放；所有已识别手势都会被消费。
    /// </summary>
    public override void _GuiInput(InputEvent inputEvent)
    {
        if (inputEvent is InputEventMouseButton button)
        {
            HandleMouseButton(button);
        }
        else if (inputEvent is InputEventMouseMotion motion && _dragging)
        {
            _pan += motion.Position - _dragOrigin;
            _dragOrigin = motion.Position;
            QueueRedraw();
            AcceptEvent();
        }
    }

    /// <summary>
    /// 允许方向键按屏幕方向切换最近节点，回车确认保持选择并刷新详情。
    /// </summary>
    public override void _UnhandledKeyInput(InputEvent inputEvent)
    {
        if (!HasFocus() || inputEvent is not InputEventKey key || !key.Pressed || key.Echo)
        {
            return;
        }

        Vector2 direction = key.Keycode switch
        {
            Key.Left => Vector2.Left,
            Key.Right => Vector2.Right,
            Key.Up => Vector2.Up,
            Key.Down => Vector2.Down,
            _ => Vector2.Zero,
        };
        if (direction != Vector2.Zero && SelectDirectional(direction))
        {
            GetViewport().SetInputAsHandled();
        }
    }

    /// <summary>
    /// 按真实关系绘制连线，再绘制四条泳道和固定尺寸文字节点。
    /// </summary>
    public override void _Draw()
    {
        if (_model is null || _painter is null)
        {
            DrawRect(new Rect2(Vector2.Zero, Size), new Color("0b100d"));
            return;
        }

        _painter.Draw(_model, _items, _selected?.Id, _zoom, _pan);
    }

    /// <summary>
    /// 处理按下、抬起与缩放，将点击位置转换回图谱局部坐标后进行节点命中。
    /// </summary>
    private void HandleMouseButton(InputEventMouseButton button)
    {
        if (button.ButtonIndex == MouseButton.Left)
        {
            if (button.Pressed)
            {
                CharacterBuildGraphItem? hit = HitTest(ToGraph(button.Position));
                if (hit is not null)
                {
                    Select(hit.Node);
                }
                else
                {
                    _dragging = true;
                    _dragOrigin = button.Position;
                }
                GrabFocus();
            }
            else
            {
                _dragging = false;
            }
            AcceptEvent();
        }
        else if (button.Pressed && button.ButtonIndex is
            MouseButton.WheelUp or MouseButton.WheelDown)
        {
            float factor = button.ButtonIndex == MouseButton.WheelUp ? 1.12f : 0.89f;
            ZoomAround(button.Position, factor);
            AcceptEvent();
        }
    }

    /// <summary>
    /// 选择方向投影最接近且位于对应半平面的节点，支持键盘和手柄式浏览。
    /// </summary>
    private bool SelectDirectional(Vector2 direction)
    {
        if (_selected is null || !_items.TryGetValue(_selected.Id, out var current))
        {
            return false;
        }

        Vector2 center = current.Rect.GetCenter();
        CharacterBuildGraphItem? best = _items.Values
            .Where(item => item.Node.Id != _selected.Id)
            .Where(item => (item.Rect.GetCenter() - center).Dot(direction) > 1.0f)
            .OrderBy(item => (item.Rect.GetCenter() - center).LengthSquared() -
                1200.0f * (item.Rect.GetCenter() - center).Normalized().Dot(direction))
            .FirstOrDefault();
        if (best is null)
        {
            return false;
        }

        Select(best.Node);
        return true;
    }

    /// <summary>
    /// 更新选中节点、广播详情并立即重绘相邻关系高亮。
    /// </summary>
    private void Select(CharacterBuildNodeView node)
    {
        _selected = node;
        SelectionChanged?.Invoke(node);
        QueueRedraw();
    }

    /// <summary>
    /// 将当前全部节点自动缩入可视区域，节点过多时仍可通过滚轮和拖动深入查看。
    /// </summary>
    private void FitContent()
    {
        if (_items.Count == 0 || Size.X < 1.0f || Size.Y < 1.0f)
        {
            _zoom = 1.0f;
            _pan = Vector2.Zero;
            return;
        }

        float width = _items.Values.Max(item => item.Rect.End.X) + 8.0f;
        float height = _items.Values.Max(item => item.Rect.End.Y) + 8.0f;
        _zoom = Mathf.Clamp(MathF.Min(Size.X / width, Size.Y / height), 0.52f, 1.0f);
        _pan = new Vector2(4.0f, MathF.Max(0.0f, (Size.Y - height * _zoom) * 0.5f));
    }

    /// <summary>
    /// 围绕鼠标位置缩放，确保玩家查看的节点不会在缩放时跳离指针。
    /// </summary>
    private void ZoomAround(Vector2 pivot, float factor)
    {
        Vector2 graphPoint = ToGraph(pivot);
        _zoom = Mathf.Clamp(_zoom * factor, 0.52f, 1.65f);
        _pan = pivot - graphPoint * _zoom;
        QueueRedraw();
    }

    /// <summary>将控件坐标反变换为图谱局部坐标，用于稳定命中测试。</summary>
    private Vector2 ToGraph(Vector2 point) => (point - _pan) / _zoom;

    /// <summary>返回命中指定图谱点的最上层节点。</summary>
    private CharacterBuildGraphItem? HitTest(Vector2 point) =>
        _items.Values.LastOrDefault(item => item.Rect.HasPoint(point));

}
