using TouhouWuxiaSurvivor.World.Biomes;
using TouhouWuxiaSurvivor.World.StructureTemplates;
using TouhouWuxiaSurvivor.World.Tiles;

namespace TouhouWuxiaSurvivor.World.Structures;

/// <summary>
/// 汇总一个结构的稳定身份、内容来源、合法群系、空间规则与多层俯视模板。
/// </summary>
public sealed class StructureDefinition
{
    public StructureId Id { get; }
    public string DefinitionId { get; }
    public string SourcePackId { get; }
    public IReadOnlySet<BiomeId> AllowedBiomes { get; }
    public StructurePlacementProfile Placement { get; }
    public StructureTemplateKind Template { get; }
    public TileId BaseTile { get; }
    public TileId DetailTile { get; }
    public bool IsSpawnStructure { get; }

    /// <summary>
    /// 创建不可变结构定义；允许群系为空只适用于固定出生结构。
    /// </summary>
    public StructureDefinition(
        StructureId id,
        string definitionId,
        string sourcePackId,
        IEnumerable<BiomeId> allowedBiomes,
        StructurePlacementProfile placement,
        StructureTemplateKind template,
        TileId baseTile,
        TileId detailTile,
        bool isSpawnStructure = false)
    {
        Id = id;
        DefinitionId = definitionId;
        SourcePackId = sourcePackId;
        AllowedBiomes = new HashSet<BiomeId>(allowedBiomes);
        Placement = placement;
        Template = template;
        BaseTile = baseTile;
        DetailTile = detailTile;
        IsSpawnStructure = isSpawnStructure;
    }

    /// <summary>
    /// 判断锚点群系能否承载该结构，固定出生结构不参与普通群系选址。
    /// </summary>
    public bool Allows(BiomeId biome) => !IsSpawnStructure && AllowedBiomes.Contains(biome);
}
