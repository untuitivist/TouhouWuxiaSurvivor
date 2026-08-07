namespace TouhouWuxiaSurvivor.World.Structures;

/// <summary>
/// 表示一个结构在无限世界中的确定性绝对 Tile 锚点和类型。
/// </summary>
public readonly record struct StructurePlacement(StructureId Id, long X, long Y);
