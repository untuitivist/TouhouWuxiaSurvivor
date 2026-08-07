namespace TouhouWuxiaSurvivor.World.Coordinates;

/// <summary>
/// 表示无限世界中的绝对区块坐标；使用 long 避免长距离探索时整数溢出。
/// </summary>
public readonly record struct ChunkCoordinate(long X, long Y)
{
    /// <summary>
    /// 对两个区块坐标逐分量相加，主要用于组合绝对原点与本地区块偏移。
    /// </summary>
    public static ChunkCoordinate operator +(ChunkCoordinate left, ChunkCoordinate right) =>
        new(left.X + right.X, left.Y + right.Y);

    /// <summary>
    /// 对两个区块坐标逐分量相减，主要用于把绝对区块投影到本地 TileMap。
    /// </summary>
    public static ChunkCoordinate operator -(ChunkCoordinate left, ChunkCoordinate right) =>
        new(left.X - right.X, left.Y - right.Y);

    /// <summary>
    /// 返回适合调试界面和日志读取的坐标文本。
    /// </summary>
    public override string ToString() => $"({X}, {Y})";
}
