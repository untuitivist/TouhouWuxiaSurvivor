namespace TouhouWuxiaSurvivor.World.Generation;

/// <summary>
/// 提供只依赖种子、绝对坐标和盐值的确定性哈希，是可重现世界生成的基础随机源。
/// </summary>
public static class DeterministicHash
{
    /// <summary>
    /// 混合输入并返回 64 位哈希；unchecked 溢出是算法设计的一部分。
    /// </summary>
    public static ulong At(ulong seed, long x, long y, ulong salt = 0)
    {
        unchecked
        {
            ulong value = seed ^ salt;
            value ^= (ulong)x * 0x9E3779B185EBCA87UL;
            value ^= (ulong)y * 0xC2B2AE3D27D4EB4FUL;
            value ^= value >> 30;
            value *= 0xBF58476D1CE4E5B9UL;
            value ^= value >> 27;
            value *= 0x94D049BB133111EBUL;
            return value ^ (value >> 31);
        }
    }

    /// <summary>
    /// 将坐标哈希映射为 [0, 1) 双精度值，保留 53 位有效随机精度。
    /// </summary>
    public static double Unit(ulong seed, long x, long y, ulong salt = 0) =>
        (At(seed, x, y, salt) >> 11) * (1.0 / (1UL << 53));

    /// <summary>
    /// 将坐标哈希映射为 [0, maximum) 整数，用于稳定选择地表变体。
    /// </summary>
    public static int Range(ulong seed, long x, long y, int maximum, ulong salt = 0) =>
        (int)(At(seed, x, y, salt) % (uint)maximum);
}
