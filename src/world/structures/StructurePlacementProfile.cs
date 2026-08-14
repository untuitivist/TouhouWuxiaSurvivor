namespace TouhouWuxiaSurvivor.World.Structures;

/// <summary>
/// 保存一个结构自己的随机扩散网格参数，语义与 Minecraft 的 spacing/separation 结构集一致。
/// </summary>
public sealed class StructurePlacementProfile
{
    public ulong Salt { get; }
    public int Spacing { get; }
    public int Separation { get; }
    public double Chance { get; }
    public int SpawnProtectionRadius { get; }
    public int FootprintRadius { get; }
    public int ForeignSeparation { get; }
    public StructureRarity Rarity { get; }

    /// <summary>
    /// 创建经完整边界校验的选址参数，避免错误配置破坏负坐标或相邻网格硬间距。
    /// </summary>
    public StructurePlacementProfile(
        ulong salt,
        int spacing,
        int separation,
        double chance,
        int spawnProtectionRadius,
        int footprintRadius,
        int foreignSeparation,
        StructureRarity rarity)
    {
        if (spacing <= 0 || separation < 0 || separation >= spacing)
        {
            throw new ArgumentOutOfRangeException(nameof(spacing),
                "Spacing must be positive and greater than separation.");
        }

        if (chance is <= 0 or > 1 || footprintRadius <= 0 || foreignSeparation < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(chance),
                "Chance and distance values must describe a usable structure set.");
        }

        Salt = salt;
        Spacing = spacing;
        Separation = separation;
        Chance = chance;
        SpawnProtectionRadius = Math.Max(0, spawnProtectionRadius);
        FootprintRadius = footprintRadius;
        ForeignSeparation = foreignSeparation;
        Rarity = rarity;
    }
}
