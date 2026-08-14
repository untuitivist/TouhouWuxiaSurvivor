using TouhouWuxiaSurvivor.World.Structures;
using TouhouWuxiaSurvivor.World.StructureTemplates;

namespace TouhouWuxiaSurvivor.World.Rendering;

/// <summary>
/// 按结构在幻想乡中的语义选择俯视轮廓；这里集中维护跨作品结构分类而不污染生成数据。
/// </summary>
public static class InternalStructureShapeResolver
{
    /// <summary>
    /// 将每个可生成结构映射到明确轮廓，新增结构未登记时使用遗迹而非误画成红魔馆。
    /// </summary>
    public static InternalStructureShape Resolve(StructureId structure) =>
        (InternalStructureShape)(int)StructureTemplateResolver.Resolve(structure);
}
