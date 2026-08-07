using Godot;

namespace TouhouWuxiaSurvivor.Actors.Pickups;

/// <summary>
/// 根据掉落定义配置中文名称文字和分类颜色，不再加载示例项目的道具图集。
/// </summary>
public static class PickupVisualFactory
{
    /// <summary>
    /// 将完整中文名称和容易区分的类别颜色写入场上的掉落物标签。
    /// </summary>
    public static void Configure(Label label, PickupDefinition definition)
    {
        label.Text = definition.DisplayName;
        label.Modulate = definition.Kind switch
        {
            PickupKind.MoveSpeed => new Color("8ad6c0"),
            PickupKind.RapidFire => new Color("f0d477"),
            PickupKind.SpiralShot => new Color("f092aa"),
            _ => Colors.White,
        };
    }
}
