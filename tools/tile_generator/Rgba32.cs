namespace TouhouWuxiaSurvivor.Tools.TileGenerator;

/// <summary>
/// 表示按红、绿、蓝、透明度顺序存储的单个 8 位通道像素颜色。
/// </summary>
internal readonly record struct Rgba32(byte R, byte G, byte B, byte A = 255);
