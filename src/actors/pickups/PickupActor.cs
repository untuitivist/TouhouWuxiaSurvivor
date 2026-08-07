using Godot;
using TouhouWuxiaSurvivor.Actors.Player;

namespace TouhouWuxiaSurvivor.Actors.Pickups;

/// <summary>
/// 表示场上的临时强化掉落物，负责显示、临期闪烁、玩家拾取和超时回收。
/// </summary>
public partial class PickupActor : Area2D
{
    private PickupDefinition? _definition;
    private Label? _label;
    private double _lifetimeLeft = 14.0;
    private double _blinkAccumulator;
    public event Action? Collected;

    /// <summary>
    /// 在加入场景树前注入具体掉落定义，确保就绪时能够立即创建正确图集纹理。
    /// </summary>
    public void Configure(PickupDefinition definition) => _definition = definition;

    /// <summary>
    /// 校验配置、绑定玩家碰撞通知并应用目录中声明的中文掉落物名称。
    /// </summary>
    public override void _Ready()
    {
        if (_definition is null)
        {
            _definition = PickupCatalog.All[0];
        }

        _label = GetNode<Label>("Visual");
        PickupVisualFactory.Configure(_label, _definition);
        BodyEntered += OnBodyEntered;
    }

    /// <summary>
    /// 推进掉落寿命，并在最后两秒以稳定频率闪烁提示即将消失。
    /// </summary>
    public override void _Process(double delta)
    {
        _lifetimeLeft -= delta;
        if (_lifetimeLeft <= 0.0)
        {
            QueueFree();
            return;
        }

        if (_label is null || _lifetimeLeft > 2.0)
        {
            return;
        }

        _blinkAccumulator += delta;
        _label.Visible = ((int)(_blinkAccumulator * 10.0) & 1) == 0;
    }

    /// <summary>
    /// 仅接受带有玩家强化组件的物理体，成功应用效果后立即回收掉落实例。
    /// </summary>
    private void OnBodyEntered(Node2D body)
    {
        if (_definition is null)
        {
            return;
        }

        PlayerBuffController? buffs = body.GetNodeOrNull<PlayerBuffController>("Buffs");
        if (buffs is null)
        {
            return;
        }

        buffs.Apply(_definition);
        Collected?.Invoke();
        QueueFree();
    }
}
