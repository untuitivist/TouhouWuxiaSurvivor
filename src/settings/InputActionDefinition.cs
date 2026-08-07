using Godot;

namespace TouhouWuxiaSurvivor.Settings;

/// <summary>
/// 描述一个允许玩家重绑定的游戏操作，以及它的中文名称和两个键位槽默认值。
/// Key.None 表示该槽默认不绑定，但仍允许玩家稍后设置。
/// </summary>
public sealed record InputActionDefinition(
    string Id,
    string DisplayName,
    Key PrimaryKey,
    Key SecondaryKey);
