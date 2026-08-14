using TouhouWuxiaSurvivor.World.Structures;
using TouhouWuxiaSurvivor.World.Tiles;

namespace TouhouWuxiaSurvivor.World.StructureTemplates;

/// <summary>
/// 将结构模板的语义层映射为该结构自己的 Tile 组合，路径和场地不会再退化为同一种色块。
/// </summary>
public static class StructureTilePalette
{
    /// <summary>
    /// 依据角色返回地表 Tile；None 保持原群系，Socket 使用高辨识细节供未来事件系统读取。
    /// </summary>
    public static bool TryResolve(
        StructureDefinition definition,
        StructureTileRole role,
        out TileId tile)
    {
        tile = role switch
        {
            StructureTileRole.Ground => definition.BaseTile,
            StructureTileRole.Detail => definition.DetailTile,
            StructureTileRole.Path => PathFor(definition.Template),
            StructureTileRole.Arena => ArenaFor(definition.Template, definition.BaseTile),
            StructureTileRole.Socket => definition.DetailTile,
            _ => default,
        };
        return role != StructureTileRole.None;
    }

    /// <summary>
    /// 为水域、山地、竹林和常规建筑选择不同道路材质，避免所有地标出现相同神社参道。
    /// </summary>
    private static TileId PathFor(StructureTemplateKind template) => template switch
    {
        StructureTemplateKind.Garden => TileId.LakeShorePebbles,
        StructureTemplateKind.Terrace or StructureTemplateKind.Cave => TileId.MountainRockCracks,
        StructureTemplateKind.Circle => TileId.MagicSoilSparkles,
        StructureTemplateKind.Crossroads or StructureTemplateKind.Market => TileId.DirtPebbles,
        StructureTemplateKind.Bridge or StructureTemplateKind.Ship => TileId.StoneBase,
        _ => TileId.ShrinePathPebbles,
    };

    /// <summary>
    /// 为战斗场或主体内部选取稳定材质；大型建筑用石基，自然庭园保留原定义地表。
    /// </summary>
    private static TileId ArenaFor(StructureTemplateKind template, TileId fallback) => template switch
    {
        StructureTemplateKind.Manor or StructureTemplateKind.Stage or
            StructureTemplateKind.Outpost => TileId.StoneBase,
        StructureTemplateKind.Ship => TileId.ShrinePathBase,
        _ => fallback,
    };
}
