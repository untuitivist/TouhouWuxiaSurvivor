using Godot;

namespace TouhouWuxiaSurvivor.Actors.Spirit;

/// <summary>
/// 表示场上持久存在的灵息文字，进入吸引范围后追向玩家并交付累计经验值。
/// </summary>
public partial class SpiritDropActor : Node2D
{
    private Node2D? _target;
    private Func<float>? _attractionRange;
    private Label? _label;
    private int _value = 1;
    private double _pulseTime;

    public int Value => _value;
    public event Action<int>? Collected;

    /// <summary>
    /// 在加入场景树前注入玩家目标、经验值和动态吸引范围读取器。
    /// </summary>
    public void Configure(Node2D target, int value, Func<float> attractionRange)
    {
        _target = target;
        _value = Math.Max(1, value);
        _attractionRange = attractionRange;
    }

    /// <summary>
    /// 缓存文字节点并根据当前累计值建立初始显示。
    /// </summary>
    public override void _Ready()
    {
        _label = GetNode<Label>("Visual");
        RefreshLabel();
    }

    /// <summary>
    /// 在范围外轻微呼吸，进入范围后加速追踪，并在接近玩家时完成一次拾取。
    /// </summary>
    public override void _Process(double delta)
    {
        if (_target is null || !GodotObject.IsInstanceValid(_target))
        {
            return;
        }

        _pulseTime += delta;
        if (_label is not null)
        {
            float pulse = 0.88f + MathF.Sin((float)_pulseTime * 5.0f) * 0.12f;
            _label.Modulate = new Color(0.72f, 0.94f, 0.82f, pulse);
        }

        float distance = GlobalPosition.DistanceTo(_target.GlobalPosition);
        if (distance <= 11.0f)
        {
            Collected?.Invoke(_value);
            QueueFree();
            return;
        }

        float range = Math.Max(1.0f, _attractionRange?.Invoke() ?? 72.0f);
        if (distance > range)
        {
            return;
        }

        float speed = Mathf.Lerp(230.0f, 120.0f, Mathf.Clamp(distance / range, 0.0f, 1.0f));
        GlobalPosition = GlobalPosition.MoveToward(_target.GlobalPosition, speed * (float)delta);
    }

    /// <summary>
    /// 把新经验合并到当前灵息，供生成上限触发时避免丢失击破奖励。
    /// </summary>
    public void AddValue(int amount)
    {
        _value += Math.Max(0, amount);
        RefreshLabel();
    }

    /// <summary>
    /// 一点灵息显示短名称，合并后附加数量，保持场景中文字尺寸稳定。
    /// </summary>
    private void RefreshLabel()
    {
        if (_label is not null)
        {
            _label.Text = _value == 1 ? "灵息" : $"灵息×{_value}";
        }
    }
}
