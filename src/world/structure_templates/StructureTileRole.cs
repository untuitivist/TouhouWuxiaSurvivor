namespace TouhouWuxiaSurvivor.World.StructureTemplates;

/// <summary>
/// 描述模板格在结构中的功能层，供地表、地图和未来交互共同读取而非只保存颜色。
/// </summary>
public enum StructureTileRole
{
    None,
    Ground,
    Detail,
    Path,
    Arena,
    Socket,
}
