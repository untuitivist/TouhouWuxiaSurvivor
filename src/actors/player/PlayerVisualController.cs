using Godot;
using TouhouWuxiaSurvivor.Visuals.Internal;

namespace TouhouWuxiaSurvivor.Actors.Player;

/// <summary>
/// 在实际局内呈现灵梦内部素材，并统一处理移动朝向、强化标记、受伤闪烁与死亡反馈。
/// </summary>
public partial class PlayerVisualController : Node2D
{
    private static readonly InternalVisualCatalog VisualCatalog = new();
    private Sprite2D? _sprite;
    private Label? _fallbackName;
    private Label? _armedEffect;
    private bool _moving;
    private double _motionTime;
    private int _currentFrame = -1;

    public bool UsesSprite { get; private set; }
    public bool IsArmedVisible => _armedEffect?.Visible ?? false;
    public string DisplayName => "博丽灵梦";

    /// <summary>
    /// 加载灵梦内部角色图；公开包不含内部素材时自动保留中文名作为玩家视觉。
    /// </summary>
    public override void _Ready()
    {
        _sprite = GetNode<Sprite2D>("Sprite");
        _fallbackName = GetNode<Label>("FallbackName");
        _armedEffect = GetNode<Label>("ArmedEffect");
        UsesSprite = TryLoadReimuTexture();
        _sprite.Visible = UsesSprite;
        _fallbackName.Visible = !UsesSprite;
        Modulate = Colors.White;
        SetArmed(false);
        UpdateFrame();
    }

    /// <summary>
    /// 根据实际移动速度更新朝向与轻微步行动势，停止时平滑回到稳定站姿。
    /// </summary>
    public void SetMotion(Vector2 velocity)
    {
        _moving = velocity.LengthSquared() > 1.0f;
        if (UsesSprite && Math.Abs(velocity.X) > 0.1f)
        {
            _sprite!.FlipH = velocity.X < 0.0f;
        }
    }

    /// <summary>
    /// 移动时按固定八帧每秒播放原作四帧行走条，停止时稳定停在第一帧。
    /// </summary>
    public override void _Process(double delta)
    {
        if (!UsesSprite || _sprite is null)
        {
            return;
        }

        _motionTime = _moving ? _motionTime + delta : 0.0;
        UpdateFrame();
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
    /// 切换整个角色视觉的可见性，供生命组件实现受伤无敌期间的闪烁反馈。
    /// </summary>
    public void SetBlinkVisible(bool visible) => Visible = visible;

    /// <summary>
    /// 恢复可见、停止移动并降低角色亮度，形成稳定且与素材兼容的死亡状态。
    /// </summary>
    public void SetDefeated()
    {
        Visible = true;
        _moving = false;
        SetArmed(false);
        Modulate = new Color(0.45f, 0.45f, 0.45f);
    }

    /// <summary>
    /// 从共享内部清单读取本体灵梦四帧角色条，并固定为最近邻区域采样避免像素模糊。
    /// </summary>
    private bool TryLoadReimuTexture()
    {
        if (!VisualCatalog.TryGet(
                "base", InternalVisualCategory.Character, "博丽灵梦", out var definition) ||
            definition.Kind != InternalVisualKind.ActorStrip ||
            !VisualCatalog.TryGetTexture(definition, out Texture2D texture))
        {
            return false;
        }

        _sprite!.Texture = texture;
        _sprite.TextureFilter = TextureFilterEnum.Nearest;
        _sprite.RegionEnabled = true;
        return true;
    }

    /// <summary>
    /// 仅在帧序号变化时更新 48×48 图集区域，降低玩家视觉每帧属性写入次数。
    /// </summary>
    private void UpdateFrame()
    {
        if (!UsesSprite || _sprite is null)
        {
            return;
        }

        int frame = _moving ? (int)(_motionTime * 8.0) % 4 : 0;
        if (frame == _currentFrame)
        {
            return;
        }

        _currentFrame = frame;
        _sprite.RegionRect = new Rect2(frame * 48.0f, 0.0f, 48.0f, 48.0f);
    }
}
