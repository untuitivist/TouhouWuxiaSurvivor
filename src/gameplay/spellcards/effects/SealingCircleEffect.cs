using Godot;

namespace TouhouWuxiaSurvivor.Gameplay.SpellCards.Effects;

/// <summary>
/// 播放封魔阵的短暂文字结界演出，仅负责视觉生命周期而不重复结算伤害。
/// </summary>
public partial class SealingCircleEffect : Node2D
{
    private const double DurationSeconds = 0.55;
    private double _elapsed;
    private Label? _fallbackLabel;

    /// <summary>
    /// 逐帧放大并淡出结界文字，在固定持续时间结束后自动回收节点。
    /// </summary>
    public override void _Ready()
    {
        _fallbackLabel = GetNode<Label>("FallbackLabel");
        bool hasBullets = CreateBulletRing();
        _fallbackLabel.Visible = !hasBullets;
    }

    /// <summary>
    /// 逐帧放大并淡出结界视觉，在固定持续时间结束后自动回收节点。
    /// </summary>
    public override void _Process(double delta)
    {
        _elapsed += delta;
        float progress = Math.Clamp((float)(_elapsed / DurationSeconds), 0.0f, 1.0f);
        Scale = Vector2.One * Mathf.Lerp(0.35f, 1.65f, progress);
        Modulate = new Color(1.0f, 1.0f, 1.0f, 1.0f - progress);
        if (_elapsed >= DurationSeconds)
        {
            QueueFree();
        }
    }

    /// <summary>
    /// 从封魔阵对应图集生成八枚环状子弹；无法加载内部素材时返回 false 供文字回退。
    /// </summary>
    private bool CreateBulletRing()
    {
        bool available = false;
        for (int index = 0; index < 8; index++)
        {
            var bullet = new InternalSpellBulletVisual();
            AddChild(bullet);
            bullet.Configure("梦符「封魔阵」", index + 5);
            bullet.Position = Vector2.FromAngle(Mathf.Tau * index / 8.0f) * 26.0f;
            bullet.Scale = Vector2.One * 0.8f;
            available |= bullet.Visible;
        }

        return available;
    }
}
