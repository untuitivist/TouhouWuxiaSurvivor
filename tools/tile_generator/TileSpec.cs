namespace TouhouWuxiaSurvivor.Tools.TileGenerator;

/// <summary>
/// 描述一张可生成 Tile 的路径标识、三色调色板、图案算法和确定性随机种子。
/// </summary>
internal sealed record TileSpec(
    string Category,
    string Id,
    Rgba32 BaseColor,
    Rgba32 AccentA,
    Rgba32 AccentB,
    PatternKind Pattern,
    uint Seed);
