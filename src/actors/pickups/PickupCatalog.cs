using Godot;

namespace TouhouWuxiaSurvivor.Actors.Pickups;

/// <summary>
/// 集中提供三类示例掉落物定义、精确图集区域和加权随机选择。
/// </summary>
public static class PickupCatalog
{
    public static IReadOnlyList<PickupDefinition> All { get; } =
    [
        new(PickupKind.MoveSpeed, "移速道具", 2.0f, 5.0f, 1.5f, 1.0f, false),
        new(PickupKind.RapidFire, "射速道具", 2.0f, 5.0f, 1.0f, 2.0f, false),
        new(PickupKind.SpiralShot, "强化道具", 1.0f, 5.0f, 1.0f, 20.0f, true),
    ];

    /// <summary>
    /// 按目录权重随机返回一种强化，保证稀有弹幕强化出现频率低于基础强化。
    /// </summary>
    public static PickupDefinition Choose(RandomNumberGenerator random)
    {
        float totalWeight = All.Sum(definition => definition.DropWeight);
        float targetWeight = random.RandfRange(0.0f, totalWeight);
        foreach (PickupDefinition definition in All)
        {
            targetWeight -= definition.DropWeight;
            if (targetWeight <= 0.0f)
            {
                return definition;
            }
        }

        return All[^1];
    }

    /// <summary>
    /// 按枚举取得唯一配置，供测试、宝箱或未来固定奖励直接生成指定掉落物。
    /// </summary>
    public static PickupDefinition Get(PickupKind kind) =>
        All.First(definition => definition.Kind == kind);
}
