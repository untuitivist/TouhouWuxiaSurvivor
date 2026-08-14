using TouhouWuxiaSurvivor.World.Biomes;
using TouhouWuxiaSurvivor.World.Official;
using TouhouWuxiaSurvivor.World.StructureTemplates;
using TouhouWuxiaSurvivor.World.Tiles;

namespace TouhouWuxiaSurvivor.World.Structures;

/// <summary>
/// 注册本体与全部正作地标的数据定义，供定位、压印、地图和渲染共享。
/// </summary>
public static class StructureCatalog
{
    public static IReadOnlyList<StructureDefinition> All { get; } = CreateAll();
    public static int MaximumFootprintRadius { get; } =
        All.Max(item => item.Placement.FootprintRadius);
    private static readonly IReadOnlyDictionary<StructureId, StructureDefinition> ById =
        All.ToDictionary(item => item.Id);

    /// <summary>
    /// 按枚举身份返回规范定义；未登记结构属于内容错误并立即抛出具体异常。
    /// </summary>
    public static StructureDefinition GetRequired(StructureId id) =>
        ById.TryGetValue(id, out StructureDefinition? definition)
            ? definition
            : throw new KeyNotFoundException($"Structure definition is missing: {id}.");

    /// <summary>
    /// 构建六个本体定义和六十个正作定义，并验证每个枚举只登记一次。
    /// </summary>
    private static IReadOnlyList<StructureDefinition> CreateAll()
    {
        var definitions = new List<StructureDefinition>
        {
            Base(StructureId.HakureiShrine, "base.hakurei_shrine", [], BiomeId.HakureiShrine,
                TileId.ShrineGrassBase, TileId.ShrineGrassPetals, true),
            Base(StructureId.ShrineCourt, "base.shrine_court", [BiomeId.HakureiShrine],
                BiomeId.HakureiShrine, TileId.ShrineGrassBase, TileId.ShrinePathPebbles),
            Base(StructureId.HumanVillage, "base.human_village", [BiomeId.HumanVillage],
                BiomeId.HumanVillage, TileId.GrassDots, TileId.StoneCracks),
            Base(StructureId.MagicCircle, "base.magic_circle", [BiomeId.MagicForest],
                BiomeId.MagicForest, TileId.MossDots, TileId.MagicSoilSparkles),
            Base(StructureId.MountainTerrace, "base.mountain_terrace", [BiomeId.YoukaiMountain],
                BiomeId.YoukaiMountain, TileId.MountainGrassFlowers, TileId.MountainRockCracks),
            Base(StructureId.Crossroads, "base.crossroads", [BiomeId.Common],
                BiomeId.Common, TileId.GrassDots, TileId.DirtPebbles),
        };
        definitions.AddRange(OfficialWorldContentCatalog.All.Select(Official));
        StructureId[] missing = Enum.GetValues<StructureId>()
            .Where(id => definitions.All(item => item.Id != id)).ToArray();
        if (missing.Length > 0 || definitions.GroupBy(item => item.Id).Any(group => group.Count() != 1))
        {
            throw new InvalidOperationException(
                $"Structure catalog is incomplete or duplicated: {string.Join(',', missing)}.");
        }

        return definitions;
    }

    /// <summary>
    /// 创建本体结构定义；颜色参数与群系参数分开保留，模板不会退化为单色方块。
    /// </summary>
    private static StructureDefinition Base(
        StructureId id,
        string definitionId,
        IEnumerable<BiomeId> biomes,
        BiomeId sourceBiome,
        TileId baseTile,
        TileId detailTile,
        bool spawn = false)
    {
        StructureTemplateKind template = StructureTemplateResolver.Resolve(id);
        return new StructureDefinition(id, definitionId, string.Empty, biomes,
            StructureProfileFactory.Create(id, template), template, baseTile, detailTile, spawn);
    }

    /// <summary>
    /// 将正作世界目录投影为完整结构定义，来源包、合法群系和地区层级均保留。
    /// </summary>
    private static StructureDefinition Official(OfficialWorldContentDefinition source)
    {
        StructureTemplateKind template = StructureTemplateResolver.Resolve(source.Structure);
        return new StructureDefinition(
            source.Structure,
            $"{source.PackId}.{source.Structure.ToString().ToLowerInvariant()}",
            source.PackId,
            [source.Biome],
            StructureProfileFactory.Create(source.Structure, template, source.RegionIndex),
            template,
            source.BaseTile,
            source.DetailTile);
    }
}
