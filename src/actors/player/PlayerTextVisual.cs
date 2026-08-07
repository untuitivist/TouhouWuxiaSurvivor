using Godot;

namespace TouhouWuxiaSurvivor.Actors.Player;

/// <summary>
/// 使用中文名称呈现临时玩家视觉，并封装强化、受伤闪烁和死亡状态的显示细节。
/// </summary>
public partial class PlayerTextVisual : Node2D
{
    private Label? _nameLabel;
    private Label? _armedEffect;

    public bool IsArmedVisible => _armedEffect?.Visible ?? false;
    public string DisplayName => _nameLabel?.Text ?? string.Empty;

    /// <summary>
    /// 缓存两个文字节点，并确保进入场景时使用正常颜色且不显示强化标记。
    /// </summary>
    public override void _Ready()
    {
        _nameLabel = GetNode<Label>("Name");
        _armedEffect = GetNode<Label>("ArmedEffect");
        Modulate = Colors.White;
        _armedEffect.Hide();
    }

    /// <summary>
    /// 根据螺旋强化状态显示或隐藏角色上方的中文阴阳玉标记。
    /// </summary>
    public void SetArmed(bool armed)
    {
        if (_armedEffect is not null)
        {
            _armedEffect.Visible = armed;
        }
    }

    /// <summary>
    /// 切换整个角色文字的可见性，供生命组件实现受伤无敌期间的闪烁反馈。
    /// </summary>
    public void SetBlinkVisible(bool visible) => Visible = visible;

    /// <summary>
    /// 恢复可见、隐藏强化标记并降低文字亮度，形成稳定的死亡状态反馈。
    /// </summary>
    public void SetDefeated()
    {
        Visible = true;
        SetArmed(false);
        Modulate = new Color(0.45f, 0.45f, 0.45f);
    }
}
