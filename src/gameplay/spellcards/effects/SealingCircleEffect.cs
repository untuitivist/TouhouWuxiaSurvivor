using Godot;
using TouhouWuxiaSurvivor.Ecs.Combat.Projectiles;
using TouhouWuxiaSurvivor.Gameplay.SpellCards.Definitions;

namespace TouhouWuxiaSurvivor.Gameplay.SpellCards.Effects;

/// <summary>
/// 播放范围或护持奥义的短暂阵式演出，仅负责视觉生命周期而不重复结算伤害。
/// </summary>
public partial class SealingCircleEffect : Node2D
{
    private const double DurationSeconds = 0.55;
    private double _elapsed;
    private Label? _fallbackLabel;
    private string _sourcePackId = string.Empty;
    private string _spellCardName = string.Empty;
    private SpellCardGeometryKind _geometryKind = SpellCardGeometryKind.Ring;
    private SpellBulletStyleKind _bulletStyle = SpellBulletStyleKind.Amulet;
    private bool _configured;

    /// <summary>
    /// 在节点进入场景树前注入当前符卡视觉键，使所有作品复用结界演出而不复用错误素材身份。
    /// </summary>
    public void Configure(
        string sourcePackId,
        string spellCardName,
        SpellCardGeometryKind geometryKind,
        SpellBulletStyleKind bulletStyle)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePackId);
        ArgumentException.ThrowIfNullOrWhiteSpace(spellCardName);
        _sourcePackId = sourcePackId;
        _spellCardName = spellCardName;
        _geometryKind = geometryKind;
        _bulletStyle = bulletStyle;
        _configured = true;
    }

    /// <summary>
    /// 逐帧放大并淡出结界文字，在固定持续时间结束后自动回收节点。
    /// </summary>
    public override void _Ready()
    {
        if (!_configured)
        {
            throw new InvalidOperationException(
                "Sealing circle must receive a spell-card visual identity before entering the tree.");
        }

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
            bullet.Configure(
                _sourcePackId, _spellCardName, _bulletStyle, index + 5);
            bullet.Position = ResolveBulletPosition(index);
            bullet.Rotation = ProjectileVisualPosePolicy.ResolveRotation(
                _bulletStyle, bullet.Position);
            bullet.Scale *= 0.8f;
            available |= bullet.Visible;
        }

        return available;
    }

    /// <summary>按符卡几何改变结界弹幕轮廓，使范围效果在不增加命中预算时仍有明确辨识度。</summary>
    private Vector2 ResolveBulletPosition(int index)
    {
        float angle = Mathf.Tau * index / 8.0f;
        return _geometryKind switch
        {
            SpellCardGeometryKind.Fan => new Vector2(
                Mathf.Cos(Mathf.Lerp(-1.05f, 1.05f, index / 7.0f)),
                Mathf.Sin(Mathf.Lerp(-1.05f, 1.05f, index / 7.0f))) * 30.0f,
            SpellCardGeometryKind.Line => new Vector2((index - 3.5f) * 9.0f, 0.0f),
            SpellCardGeometryKind.Backstab => Vector2.FromAngle(angle) *
                (index % 2 == 0 ? 34.0f : 18.0f),
            SpellCardGeometryKind.Orbit => Vector2.FromAngle(angle + 0.42f) * 25.0f,
            _ => Vector2.FromAngle(angle) * 26.0f,
        };
    }
}
