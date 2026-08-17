using TouhouWuxiaSurvivor.Content;
using TouhouWuxiaSurvivor.World.Biomes;
using TouhouWuxiaSurvivor.World.Official;
using TouhouWuxiaSurvivor.World.Regions;
using TouhouWuxiaSurvivor.World.Structures;
using TouhouWuxiaSurvivor.World.Tiles;

namespace TouhouWuxiaSurvivor.Ui.Compendium;

/// <summary>把正式宏域与结构目录投影为地区、地标图鉴，展示真实生成和发现规则。</summary>
public static class WorldCompendiumEntryFactory
{
    private static readonly BiomeId[] BaseBiomes =
        [BiomeId.Common, BiomeId.HakureiShrine, BiomeId.HumanVillage,
            BiomeId.MagicForest, BiomeId.YoukaiMountain];
    private static readonly TileId[] BaseBiomeTiles =
        [TileId.GrassBase, TileId.ShrineGrassBase, TileId.DirtBase,
            TileId.ForestFloorBase, TileId.MountainGrassBase];
    private static readonly StructureId[] BaseStructures =
        [StructureId.HakureiShrine, StructureId.ShrineCourt, StructureId.HumanVillage,
            StructureId.MagicCircle, StructureId.MountainTerrace, StructureId.Crossroads];

    /// <summary>按本体、作品编号和地区层级返回全部地区与结构，保持来源筛选顺序稳定。</summary>
    public static IReadOnlyList<CompendiumEntry> CreateAll()
    {
        var entries = new List<CompendiumEntry>();
        AddBase(entries);
        foreach (ContentPackDefinition source in ContentPackCatalog.All)
        foreach (OfficialWorldContentDefinition world in OfficialWorldContentCatalog.GetByPack(source.Id))
        {
            entries.Add(CreateOfficialBiome(source, world));
            entries.Add(CreateStructure(source, world.BiomeName,
                StructureCatalog.GetRequired(world.Structure), world.RegionIndex));
        }

        return entries;
    }

    /// <summary>将本体五个群系与六个地标绑定到真实枚举，拒绝依赖清单数组下标之外的猜测。</summary>
    private static void AddBase(List<CompendiumEntry> entries)
    {
        ContentPackDefinition source = ContentPackCatalog.Base;
        ContentAddition[] biomes = source.Additions.Where(item => item.Category == "地区").ToArray();
        ContentAddition[] structures = source.Additions.Where(item => item.Category == "结构").ToArray();
        if (biomes.Length != BaseBiomes.Length || structures.Length != BaseStructures.Length)
        {
            throw new InvalidDataException("Base world manifest does not match the runtime world catalog.");
        }

        for (int index = 0; index < biomes.Length; index++)
        {
            BiomeId biome = BaseBiomes[index];
            string landmarks = string.Join("、", StructureCatalog.All
                .Where(item => item.SourcePackId.Length == 0 &&
                    (item.Allows(biome) || item.IsSpawnStructure && biome == BiomeId.HakureiShrine))
                .Select(item => StructureNames.GetChinese(item.Id)));
            entries.Add(new CompendiumEntry(
                CompendiumCategory.Biome, biomes[index].Name, CompendiumCatalog.BaseSourceId,
                source.DisplayName, "本体常驻宏域 · 无限生成",
                [
                    new("宏域归属", "幻想乡本体与启用内容包等权分布", true),
                    new("宏域尺度", $"{WorldRegionPlanner.CellSize} Tile 抖动 Voronoi"),
                    new("内部地貌", "连续噪声塑造地表细节"),
                    new("关联结构", landmarks.Length == 0 ? "无固定地标" : landmarks, true),
                    new("生成与素材", "本体始终参与新一局 · " +
                        CompendiumVisualProvenanceCatalog.Placeholder, true),
                ], BaseBiomeTiles[index], index));
        }

        for (int index = 0; index < structures.Length; index++)
        {
            entries.Add(CreateStructure(source, BiomeNames.GetChinese(
                StructureCatalog.GetRequired(BaseStructures[index]).AllowedBiomes.FirstOrDefault()),
                StructureCatalog.GetRequired(BaseStructures[index]), index % 3));
        }
    }

    /// <summary>创建正作三层地区条目，明确它们是同一宏域的横向路线而非纵向难度等级。</summary>
    private static CompendiumEntry CreateOfficialBiome(
        ContentPackDefinition source,
        OfficialWorldContentDefinition world)
    {
        string layer = GetLayerName(world.RegionIndex);
        return new CompendiumEntry(
            CompendiumCategory.Biome, world.BiomeName, source.Id,
            CompendiumSourceText.GetLabel(source), $"{layer} · {world.EnemyName}",
            [
                new("宏域层级", layer),
                new("宏域尺度", $"{WorldRegionPlanner.CellSize} Tile"),
                new("关联结构", world.StructureName),
                new("地区敌人", world.EnemyName),
                new("生成与素材", "与同作其他地区组成外围至核心路线 · " +
                    CompendiumVisualProvenanceCatalog.Placeholder, true),
            ], world.BaseTile, world.RegionIndex);
    }

    /// <summary>创建结构条目并展示其独立候选网格、间距、占地、模板和地图发现语义。</summary>
    private static CompendiumEntry CreateStructure(
        ContentPackDefinition source,
        string biomeName,
        StructureDefinition structure,
        int previewVariant)
    {
        StructurePlacementProfile placement = structure.Placement;
        return new CompendiumEntry(
            CompendiumCategory.Structure, StructureNames.GetChinese(structure.Id),
            CompendiumSourceText.GetId(source), CompendiumSourceText.GetLabel(source),
            $"{GetRarityName(placement.Rarity)} · {GetTemplateName(structure.Template)}",
            [
                new("所在地区", structure.IsSpawnStructure ? "固定出生地" : biomeName, true),
                new("俯视模板", GetTemplateName(structure.Template)),
                new("空间稀有度", GetRarityName(placement.Rarity)),
                new("候选间距", $"{placement.Spacing} Tile"),
                new("同类分离", $"{placement.Separation} Tile"),
                new("生成概率", $"{placement.Chance:P0}"),
                new("占地半径", $"{placement.FootprintRadius} Tile"),
                new("地图发现", "进入发现范围后揭示"),
                new("发现与素材", "世界种子决定实例位置、朝向与变体 · " +
                    CompendiumVisualProvenanceCatalog.Placeholder, true),
            ], structure.BaseTile, previewVariant);
    }

    /// <summary>将三层宏域索引转换为空间名称，不把核心地区错误描述为数值增幅。</summary>
    private static string GetLayerName(int index) => index switch
    {
        0 => "外围地区",
        1 => "内部地区",
        _ => "核心地区",
    };

    /// <summary>将结构稀有度转换为只描述空间密度的中文文本。</summary>
    private static string GetRarityName(StructureRarity rarity) => rarity switch
    {
        StructureRarity.Common => "常见",
        StructureRarity.Regional => "区域地标",
        StructureRarity.Landmark => "远距地标",
        StructureRarity.Mythic => "极远地标",
        _ => throw new ArgumentOutOfRangeException(nameof(rarity)),
    };

    /// <summary>将结构模板枚举转换为图鉴使用的武侠风空间称谓。</summary>
    private static string GetTemplateName(World.StructureTemplates.StructureTemplateKind template) =>
        template switch
        {
            World.StructureTemplates.StructureTemplateKind.Shrine => "神社院落",
            World.StructureTemplates.StructureTemplateKind.Settlement => "聚落",
            World.StructureTemplates.StructureTemplateKind.Circle => "阵坛",
            World.StructureTemplates.StructureTemplateKind.Garden => "庭园",
            World.StructureTemplates.StructureTemplateKind.Manor => "馆邸",
            World.StructureTemplates.StructureTemplateKind.Terrace => "台地",
            World.StructureTemplates.StructureTemplateKind.Crossroads => "道口",
            World.StructureTemplates.StructureTemplateKind.Gate => "门关",
            World.StructureTemplates.StructureTemplateKind.Ruin => "遗迹",
            World.StructureTemplates.StructureTemplateKind.Bridge => "桥梁",
            World.StructureTemplates.StructureTemplateKind.Ship => "船体",
            World.StructureTemplates.StructureTemplateKind.Stage => "演武台",
            World.StructureTemplates.StructureTemplateKind.Tower => "高塔",
            World.StructureTemplates.StructureTemplateKind.Market => "市集",
            World.StructureTemplates.StructureTemplateKind.Cave => "洞窟",
            World.StructureTemplates.StructureTemplateKind.Outpost => "据点",
            _ => throw new ArgumentOutOfRangeException(nameof(template)),
        };
}
