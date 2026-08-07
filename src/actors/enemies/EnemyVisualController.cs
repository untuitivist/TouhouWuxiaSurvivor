using Godot;
using TouhouWuxiaSurvivor.Visuals.Internal;

namespace TouhouWuxiaSurvivor.Actors.Enemies;

/// <summary>
/// 在正式战斗中播放共享敌人动画，并在内部素材不可用时保留中文文字回退与死亡反馈。
/// </summary>
public partial class EnemyVisualController : Node2D
{
    private const string BaseSourceId = "base";
    private static readonly InternalVisualCatalog VisualCatalog = new();
    private Sprite2D? _sprite;
    private Label? _fallbackLabel;
    private EnemyDefinition? _definition;
    private bool _usesSprite;
    private double _animationTime;
    private int _currentFrame = -1;

    public bool UsesSprite => _usesSprite;

    /// <summary>
    /// 缓存场景子节点并默认停止动画，直到父敌人注入完整定义。
    /// </summary>
    public override void _Ready()
    {
        _sprite = GetNode<Sprite2D>("Sprite");
        _fallbackLabel = GetNode<Label>("FallbackLabel");
        SetProcess(false);
    }

    /// <summary>
    /// 按内容包和敌人名加载四帧图鉴动画；缺少匹配资源时显示原有中文名称。
    /// </summary>
    public void Configure(EnemyDefinition definition)
    {
        _definition = definition;
        string sourceId = definition.RequiredContentPack ?? BaseSourceId;
        Texture2D? texture = null;
        if (VisualCatalog.TryGet(
                sourceId, InternalVisualCategory.Enemy, definition.DisplayName, out var visual) &&
            visual.Kind == InternalVisualKind.ActorStrip &&
            VisualCatalog.TryGetTexture(visual, out Texture2D loadedTexture))
        {
            texture = loadedTexture;
        }

        _usesSprite = texture is not null;
        if (_usesSprite)
        {
            ConfigureSprite(texture!, definition);
        }
        else
        {
            ConfigureFallback(definition);
        }

        _animationTime = 0.0;
        _currentFrame = -1;
        SetProcess(_usesSprite);
        UpdateFrame();
    }

    /// <summary>
    /// 按固定六帧每秒循环动画条，避免敌人移动速度改变播放节奏。
    /// </summary>
    public override void _Process(double delta)
    {
        _animationTime += delta;
        UpdateFrame();
    }

    /// <summary>
    /// 根据水平移动方向翻转精灵；文字回退不翻转，保持中文可读性。
    /// </summary>
    public void SetFacing(float horizontalDirection)
    {
        if (_usesSprite && _sprite is not null && Math.Abs(horizontalDirection) > 0.01f)
        {
            _sprite.FlipH = horizontalDirection < 0.0f;
        }
    }

    /// <summary>
    /// 对精灵或文字应用受伤红闪，结束时分别恢复白色纹理和原型识别色。
    /// </summary>
    public void SetHurt(bool active)
    {
        if (_usesSprite && _sprite is not null)
        {
            _sprite.Modulate = active ? new Color(1.0f, 0.42f, 0.42f) : Colors.White;
        }
        else if (_fallbackLabel is not null && _definition is not null)
        {
            _fallbackLabel.Modulate = active
                ? new Color(1.0f, 0.45f, 0.45f)
                : EnemyVisualFactory.GetBaseColor(_definition);
        }
    }

    /// <summary>
    /// 停止动画并隐藏精灵，统一切换到可读的中文消散或爆散反馈。
    /// </summary>
    public void ShowDefeated(bool exploded)
    {
        SetProcess(false);
        if (_sprite is not null)
        {
            _sprite.Visible = false;
        }

        if (_fallbackLabel is not null)
        {
            _fallbackLabel.Visible = true;
            EnemyVisualFactory.ConfigureDefeated(_fallbackLabel, exploded);
        }
    }

    /// <summary>
    /// 设置动画纹理、首帧区域和两档稳定像素尺寸，强敌只在配置时放大一次。
    /// </summary>
    private void ConfigureSprite(Texture2D texture, EnemyDefinition definition)
    {
        _sprite!.Texture = texture;
        _sprite.Visible = true;
        _sprite.RegionEnabled = true;
        _sprite.TextureFilter = TextureFilterEnum.Nearest;
        float scale = definition.CollisionRadius >= 10.0f ? 0.75f : 0.5f;
        _sprite.Scale = Vector2.One * scale;
        _fallbackLabel!.Visible = false;
    }

    /// <summary>
    /// 隐藏精灵并用既有工厂恢复敌人中文名和原型颜色。
    /// </summary>
    private void ConfigureFallback(EnemyDefinition definition)
    {
        _sprite!.Visible = false;
        _fallbackLabel!.Visible = true;
        EnemyVisualFactory.Configure(_fallbackLabel, definition);
    }

    /// <summary>
    /// 仅在帧索引变化时更新 48×48 源区域，减少大量敌人同时存在时的属性写入。
    /// </summary>
    private void UpdateFrame()
    {
        if (!_usesSprite || _sprite is null)
        {
            return;
        }

        int frame = (int)(_animationTime * 6.0) % 4;
        if (frame == _currentFrame)
        {
            return;
        }

        _currentFrame = frame;
        _sprite.RegionRect = new Rect2(frame * 48.0f, 0.0f, 48.0f, 48.0f);
    }
}
