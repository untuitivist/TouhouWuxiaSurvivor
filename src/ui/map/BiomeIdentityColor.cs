using TouhouWuxiaSurvivor.World.Biomes;
using TouhouWuxiaSurvivor.World.Generation;

namespace TouhouWuxiaSurvivor.Ui.Map;

/// <summary>
/// 从稳定群系 ID 派生地图身份色，让平行作品内容无需维护另一份手工颜色清单。
/// </summary>
public static class BiomeIdentityColor
{
    private static readonly uint[] Palette =
    [
        0x5B8C63, 0xA76A6A, 0x6692A8, 0xA69355,
        0x7C6E9D, 0x5E927F, 0x9A7257, 0x7C8790,
    ];

    /// <summary>
    /// 使用确定性哈希选择基础色并施加轻微变调，同 ID 在所有存档和缩放级别保持一致。
    /// </summary>
    public static uint GetRgb(BiomeId biome)
    {
        ulong hash = DeterministicHash.At(0xB10EUL, (long)biome, 0, 0xC010UL);
        uint baseColor = Palette[(int)(hash % (ulong)Palette.Length)];
        int adjustment = (int)((hash >> 12) % 25) - 12;
        return Adjust(baseColor, adjustment);
    }

    /// <summary>
    /// 对 RGB 三通道施加同一明暗偏移并保持在有效字节范围。
    /// </summary>
    private static uint Adjust(uint rgb, int amount)
    {
        int red = Math.Clamp((int)((rgb >> 16) & 255) + amount, 0, 255);
        int green = Math.Clamp((int)((rgb >> 8) & 255) + amount, 0, 255);
        int blue = Math.Clamp((int)(rgb & 255) + amount, 0, 255);
        return (uint)((red << 16) | (green << 8) | blue);
    }
}
