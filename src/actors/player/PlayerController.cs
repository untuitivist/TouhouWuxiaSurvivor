using Godot;
using TouhouWuxiaSurvivor.Gameplay.Progression.Runtime;

namespace TouhouWuxiaSurvivor.Actors.Player;

/// <summary>
/// 控制首个可操作角色的八方向移动，并把移动和强化状态同步给独立角色视觉。
/// </summary>
public partial class PlayerController : CharacterBody2D
{
    private PlayerVisualController? _visual;
    private PlayerBuffController? _buffs;
    private PlayerHealth? _health;
    private RunModifierState? _runModifiers;

    [Export]
    public float MoveSpeed { get; set; } = 120.0f;

    /// <summary>
    /// 获取角色视觉、强化和生命组件，并初始化为没有强化标记的存活状态。
    /// </summary>
    public override void _Ready()
    {
        _visual = GetNode<PlayerVisualController>("Visual");
        _buffs = GetNode<PlayerBuffController>("Buffs");
        _health = GetNode<PlayerHealth>("Health");
        _visual.SetArmed(false);
    }

    /// <summary>
    /// 注入本局构筑倍率，使移动速度与临时道具倍率组合而不修改基础角色数值。
    /// </summary>
    public void ConfigureRunModifiers(RunModifierState modifiers) => _runModifiers = modifiers;

    /// <summary>
    /// 读取 WASD 或方向键输入、执行碰撞移动，并同步角色朝向与当前螺旋强化标记。
    /// </summary>
    public override void _PhysicsProcess(double delta)
    {
        if (_health?.IsDead == true)
        {
            Velocity = Vector2.Zero;
            _visual?.SetMotion(Velocity);
            _visual?.SetArmed(false);
            return;
        }

        Vector2 input = Input.GetVector("move_left", "move_right", "move_up", "move_down");
        float speedMultiplier = _buffs?.SpeedMultiplier ?? 1.0f;
        float runMultiplier = _runModifiers?.MoveSpeedMultiplier ?? 1.0f;
        Velocity = input * MoveSpeed * speedMultiplier * runMultiplier;
        MoveAndSlide();
        _visual?.SetMotion(Velocity);
        _visual?.SetArmed(_buffs?.IsSpiralActive == true);
    }
}
