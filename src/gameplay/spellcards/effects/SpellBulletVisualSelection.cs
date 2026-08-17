using Godot;
using TouhouWuxiaSurvivor.Gameplay.SpellCards.Definitions;

namespace TouhouWuxiaSurvivor.Gameplay.SpellCards.Effects;

/// <summary>
/// 保存从原作弹幕图集中选出的完整帧，以及归一化后的局内显示尺寸。
/// </summary>
public readonly record struct SpellBulletVisualSelection(
    Rect2 Source,
    float DisplaySize,
    SpellBulletStyleKind Style)
{
    /// <summary>
    /// 以指定世界坐标为中心创建 ECS 绘制目标区域。
    /// </summary>
    public Rect2 CreateDestination(Vector2 center)
    {
        var size = new Vector2(DisplaySize, DisplaySize);
        return new Rect2(center - (size * 0.5f), size);
    }

    /// <summary>
    /// 计算 Sprite2D 将原始帧归一到局内尺寸所需的缩放。
    /// </summary>
    public Vector2 CreateSpriteScale()
    {
        return new Vector2(DisplaySize / Source.Size.X, DisplaySize / Source.Size.Y);
    }
}
