using Godot;
using TouhouWuxiaSurvivor.Content.Characters;
using TouhouWuxiaSurvivor.Visuals.Internal;

namespace TouhouWuxiaSurvivor.Actors.Player;

/// <summary>
/// 在实际局内呈现当前自机的内部素材或中文名，并统一处理朝向、强化、受伤与死亡反馈。
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
    private CharacterDefinition _character = CharacterCatalog.Default;
    private string _visualSourcePackId = CharacterCatalog.Default.SourcePackId;
    private bool _portraitMode;

    public bool UsesSprite { get; private set; }
    public bool IsArmedVisible => _armedEffect?.Visible ?? false;
    public string DisplayName => _character.DisplayName;
    public string VisualSourcePackId => _visualSourcePackId;

    /// <summary>
    /// 获取视觉节点并应用菜单选定角色；缺少内部素材时自动保留完整中文名。
    /// </summary>
    public override void _Ready()
    {
        _sprite = GetNode<Sprite2D>("Sprite");
        _fallbackName = GetNode<Label>("FallbackName");
        _armedEffect = GetNode<Label>("ArmedEffect");
        ApplyCharacterVisual();
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
    /// 注入本局选择的共享角色定义，并立即切换原作映射或中文回退，不复制角色身份字段。
    /// </summary>
    public void ConfigureCharacter(
        CharacterDefinition character,
        string visualSourcePackId)
    {
        _character = character ?? throw new ArgumentNullException(nameof(character));
        ArgumentException.ThrowIfNullOrWhiteSpace(visualSourcePackId);
        if (!character.AvailableSourcePackIds.Contains(
                visualSourcePackId, StringComparer.Ordinal))
        {
            throw new ArgumentException(
                "Player visual source must belong to the selected character.",
                nameof(visualSourcePackId));
        }

        _visualSourcePackId = visualSourcePackId;
        if (_sprite is not null && _fallbackName is not null)
        {
            ApplyCharacterVisual();
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
        if (_portraitMode)
        {
            _sprite.Position = new Vector2(0.0f,
                MathF.Sin((float)_motionTime * 7.0f) * (_moving ? 1.5f : 0.0f));
            return;
        }

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
    /// 从共享清单读取当前角色动画条或立绘，并固定最近邻采样；缺失映射时返回 false。
    /// </summary>
    private bool TryLoadCharacterTexture()
    {
        if (!VisualCatalog.TryGet(
                _visualSourcePackId,
                InternalVisualCategory.Character,
                _character.DisplayName,
                out var definition) ||
            definition.Kind is not (InternalVisualKind.ActorStrip or InternalVisualKind.Portrait) ||
            !VisualCatalog.TryGetTexture(definition, out Texture2D texture))
        {
            return false;
        }

        _sprite!.Texture = texture;
        _sprite.TextureFilter = TextureFilterEnum.Nearest;
        _portraitMode = definition.Kind == InternalVisualKind.Portrait;
        _sprite.RegionEnabled = !_portraitMode;
        _sprite.Scale = Vector2.One * (_portraitMode ? 0.44f : 0.7f);
        return true;
    }

    /// <summary>
    /// 将角色姓名、字体和纹理模式原子应用到现有节点，缺图时完整显示中文名而不是空白角色。
    /// </summary>
    private void ApplyCharacterVisual()
    {
        _portraitMode = false;
        _currentFrame = -1;
        _fallbackName!.Text = _character.DisplayName;
        _fallbackName.AddThemeFontSizeOverride("font_size", _character.DisplayName.Length switch
        {
            <= 5 => 12,
            <= 9 => 9,
            _ => 7,
        });
        UsesSprite = TryLoadCharacterTexture();
        _sprite!.Visible = UsesSprite;
        _fallbackName.Visible = !UsesSprite;
        UpdateFrame();
    }

    /// <summary>
    /// 仅在帧序号变化时更新 48×48 图集区域，降低玩家视觉每帧属性写入次数。
    /// </summary>
    private void UpdateFrame()
    {
        if (!UsesSprite || _sprite is null || _portraitMode)
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
