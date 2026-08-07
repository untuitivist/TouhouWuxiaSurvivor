using TouhouWuxiaSurvivor.World.Coordinates;

namespace TouhouWuxiaSurvivor.World.Generation;

/// <summary>
/// 在绝对整数坐标上生成连续的二维值噪声和四倍频分形噪声。
/// </summary>
public static class ValueNoise2D
{
    /// <summary>
    /// 对所在噪声网格四角的确定性随机值做平滑双线性插值。
    /// </summary>
    public static double Sample(ulong seed, long x, long y, int scale, ulong salt = 0)
    {
        long x0 = GridMath.FloorDiv(x, scale);
        long y0 = GridMath.FloorDiv(y, scale);
        double tx = Smooth(GridMath.PositiveMod(x, scale) / (double)scale);
        double ty = Smooth(GridMath.PositiveMod(y, scale) / (double)scale);
        double top = Lerp(
            DeterministicHash.Unit(seed, x0, y0, salt),
            DeterministicHash.Unit(seed, x0 + 1, y0, salt),
            tx);
        double bottom = Lerp(
            DeterministicHash.Unit(seed, x0, y0 + 1, salt),
            DeterministicHash.Unit(seed, x0 + 1, y0 + 1, salt),
            tx);
        return Lerp(top, bottom, ty);
    }

    /// <summary>
    /// 叠加四层频率递增、振幅递减的值噪声并归一化，用于形成大尺度自然区域。
    /// </summary>
    public static double Fractal(ulong seed, long x, long y, int baseScale, ulong salt = 0)
    {
        double value = 0;
        double amplitude = 1;
        double totalAmplitude = 0;
        int scale = baseScale;

        for (int octave = 0; octave < 4; octave++)
        {
            value += Sample(seed, x, y, scale, salt + (ulong)octave) * amplitude;
            totalAmplitude += amplitude;
            amplitude *= 0.5;
            scale = Math.Max(8, scale / 2);
        }

        return value / totalAmplitude;
    }

    /// <summary>
    /// 对插值权重应用 smoothstep，消除噪声网格边界的一阶突变。
    /// </summary>
    private static double Smooth(double value) => value * value * (3 - 2 * value);

    /// <summary>
    /// 在两个标量之间按权重执行线性插值。
    /// </summary>
    private static double Lerp(double from, double to, double weight) =>
        from + (to - from) * weight;
}
