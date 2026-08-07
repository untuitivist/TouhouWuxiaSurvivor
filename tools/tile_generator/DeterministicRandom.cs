namespace TouhouWuxiaSurvivor.Tools.TileGenerator;

/// <summary>
/// 为像素图案生成提供轻量、可重复的 xorshift32 随机序列。
/// </summary>
internal sealed class DeterministicRandom
{
    private uint _state;

    /// <summary>
    /// 创建指定种子的序列；零种子会替换为非零常量以避免 xorshift 锁死。
    /// </summary>
    public DeterministicRandom(uint seed)
    {
        _state = seed == 0 ? 0x6D2B79F5u : seed;
    }

    /// <summary>
    /// 返回 [0, exclusiveMaximum) 的整数，无效上界会抛出参数异常。
    /// </summary>
    public int Next(int exclusiveMaximum)
    {
        if (exclusiveMaximum <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(exclusiveMaximum));
        }

        uint value = NextUInt();
        return (int)(value % (uint)exclusiveMaximum);
    }

    /// <summary>
    /// 推进一步 xorshift32 内部状态并返回新的 32 位随机值。
    /// </summary>
    private uint NextUInt()
    {
        uint value = _state;
        value ^= value << 13;
        value ^= value >> 17;
        value ^= value << 5;
        _state = value;
        return value;
    }
}
