using Godot;
using TouhouWuxiaSurvivor.Actors.Pickups;

namespace TouhouWuxiaSurvivor.Actors.Player;

/// <summary>
/// 独立管理玩家的临时移动、射速和弹幕强化计时，并向移动与武器组件暴露只读倍率。
/// </summary>
public partial class PlayerBuffController : Node
{
    private double _moveSpeedTimeLeft;
    private double _rapidFireTimeLeft;
    private double _spiralShotTimeLeft;
    private float _moveSpeedMultiplier = 1.0f;
    private float _rapidFireMultiplier = 1.0f;
    private float _spiralFireMultiplier = 1.0f;

    public float SpeedMultiplier => _moveSpeedTimeLeft > 0.0
        ? _moveSpeedMultiplier
        : 1.0f;
    public float FireRateMultiplier => IsSpiralActive
        ? _spiralFireMultiplier
        : _rapidFireTimeLeft > 0.0 ? _rapidFireMultiplier : 1.0f;
    public bool IsSpiralActive => _spiralShotTimeLeft > 0.0;

    /// <summary>
    /// 每帧独立递减三种强化的剩余时长，时间归零后对应倍率自动恢复基础值。
    /// </summary>
    public override void _Process(double delta)
    {
        _moveSpeedTimeLeft = Math.Max(0.0, _moveSpeedTimeLeft - delta);
        _rapidFireTimeLeft = Math.Max(0.0, _rapidFireTimeLeft - delta);
        _spiralShotTimeLeft = Math.Max(0.0, _spiralShotTimeLeft - delta);
    }

    /// <summary>
    /// 应用拾取定义，并采用延长至至少完整持续时间的规则处理重复拾取。
    /// </summary>
    public void Apply(PickupDefinition definition)
    {
        switch (definition.Kind)
        {
            case PickupKind.MoveSpeed:
                _moveSpeedMultiplier = definition.MoveSpeedMultiplier;
                _moveSpeedTimeLeft = Math.Max(_moveSpeedTimeLeft, definition.Duration);
                break;
            case PickupKind.RapidFire:
                _rapidFireMultiplier = definition.FireRateMultiplier;
                _rapidFireTimeLeft = Math.Max(_rapidFireTimeLeft, definition.Duration);
                break;
            case PickupKind.SpiralShot:
                _spiralFireMultiplier = definition.FireRateMultiplier;
                _spiralShotTimeLeft = Math.Max(_spiralShotTimeLeft, definition.Duration);
                break;
        }
    }

    /// <summary>
    /// 为调试 HUD 生成紧凑的强化摘要，使拾取结果无需额外面板也能立即观察。
    /// </summary>
    public string DescribeActiveEffects()
    {
        var active = new List<string>();
        AddEffect(active, "移速", _moveSpeedTimeLeft);
        AddEffect(active, "连射", _rapidFireTimeLeft);
        AddEffect(active, "螺旋", _spiralShotTimeLeft);
        return active.Count == 0 ? "无" : string.Join("  ", active);
    }

    /// <summary>
    /// 仅把仍然有效的强化及向上取整秒数追加到 HUD 文本片段列表。
    /// </summary>
    private static void AddEffect(List<string> active, string name, double timeLeft)
    {
        if (timeLeft > 0.0)
        {
            active.Add($"{name} {Math.Ceiling(timeLeft)}s");
        }
    }
}
