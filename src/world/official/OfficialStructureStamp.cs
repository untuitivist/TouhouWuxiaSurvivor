using TouhouWuxiaSurvivor.World.Generation;
using TouhouWuxiaSurvivor.World.Structures;
using TouhouWuxiaSurvivor.World.StructureTemplates;
using TouhouWuxiaSurvivor.World.Tiles;

namespace TouhouWuxiaSurvivor.World.Official;

/// <summary>
/// 为旧调用保留正作结构压印入口，实际轮廓已统一委托给十六类分层语义模板。
/// </summary>
public static class OfficialStructureStamp
{
    /// <summary>
    /// 使用规范定义、实例朝向、变体和 footprint 压印正作结构，未登记项保持地表不变。
    /// </summary>
    public static void Stamp(GeneratedChunk chunk, StructurePlacement placement)
    {
        if (!OfficialWorldContentCatalog.TryGet(placement.Id, out _))
        {
            return;
        }

        StructureDefinition definition = StructureCatalog.GetRequired(placement.Id);
        int radius = placement.FootprintRadius;
        for (int dy = -radius; dy <= radius; dy++)
        {
            for (int dx = -radius; dx <= radius; dx++)
            {
                StructureTileRole role = StructureTemplateSampler.Sample(
                    definition.Template, dx, dy, radius,
                    placement.QuarterTurns, placement.Variant);
                if (StructureTilePalette.TryResolve(definition, role, out TileId tile))
                {
                    chunk.TrySetAbsolute(placement.X + dx, placement.Y + dy, tile);
                }
            }
        }
    }
}
