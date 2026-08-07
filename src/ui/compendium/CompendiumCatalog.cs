using TouhouWuxiaSurvivor.Actors.Enemies;
using TouhouWuxiaSurvivor.Content;
using TouhouWuxiaSurvivor.World.Biomes;
using TouhouWuxiaSurvivor.World.Official;
using TouhouWuxiaSurvivor.World.Tiles;

namespace TouhouWuxiaSurvivor.Ui.Compendium;

/// <summary>
/// 从内容清单、世界目录和战斗目录构建唯一的图鉴数据源。
/// </summary>
public static class CompendiumCatalog
{
    public const string BaseSourceId = "base";
    public static IReadOnlyList<CompendiumEntry> All { get; } = BuildAll();

    /// <summary>
    /// 先收集本体内容，再按作品编号收集每个正作的地区、结构、敌人和角色。
    /// </summary>
    private static IReadOnlyList<CompendiumEntry> BuildAll()
    {
        var entries = new List<CompendiumEntry>();
        AddBaseEntries(entries);
        foreach (ContentPackDefinition pack in ContentPackCatalog.All)
        {
            AddOfficialEntries(entries, pack);
        }

        entries.AddRange(SpellCardCompendiumEntryFactory.CreateAll());

        return entries;
    }

    /// <summary>
    /// 从本体清单加入地区、结构和角色，并从敌人目录补充真实战斗数值。
    /// </summary>
    private static void AddBaseEntries(List<CompendiumEntry> entries)
    {
        ContentPackDefinition source = ContentPackCatalog.Base;
        AddManifestEntries(entries, source, "地区", CompendiumCategory.Biome,
            "幻想乡本体地区", new CompendiumFact("生成规则", "未选择正作时仍会生成", true),
            [TileId.GrassBase, TileId.ShrineGrassBase, TileId.DirtBase,
                TileId.ForestFloorBase, TileId.MountainGrassBase]);
        AddManifestEntries(entries, source, "结构", CompendiumCategory.Structure,
            "幻想乡本体地标", new CompendiumFact("地图标注", "固定显示名称", true),
            [TileId.ShrineGrassBase, TileId.BoundarySoilBase, TileId.DirtBase,
                TileId.MagicSoilBase, TileId.MountainRockBase, TileId.GrassBase]);
        foreach (EnemyDefinition enemy in EnemyCatalog.All.Where(item => item.RequiredContentPack is null))
        {
            entries.Add(CreateEnemyEntry(enemy, source, GetEnemyPreviewTile(enemy)));
        }

        AddCharacterEntries(entries, source);
    }

    /// <summary>
    /// 将一个正作包的全部世界定义转换为成对地区、结构、敌人条目，再附加角色目录。
    /// </summary>
    private static void AddOfficialEntries(List<CompendiumEntry> entries, ContentPackDefinition source)
    {
        foreach (OfficialWorldContentDefinition world in OfficialWorldContentCatalog.GetByPack(source.Id))
        {
            string regionTier = world.RegionIndex switch
            {
                0 => "外围地区",
                1 => "核心地区",
                _ => "深层地区",
            };
            entries.Add(new CompendiumEntry(
                CompendiumCategory.Biome,
                world.BiomeName,
                source.Id,
                SourceLabel(source),
                $"{regionTier} · {world.EnemyName}",
                [
                    new("地区层级", regionTier),
                    new("地表砖块", $"{world.BaseTile} / {world.DetailTile}", true),
                    new("关联结构", world.StructureName),
                    new("地区敌人", world.EnemyName),
                ],
                world.BaseTile,
                world.RegionIndex));
            entries.Add(new CompendiumEntry(
                CompendiumCategory.Structure,
                world.StructureName,
                source.Id,
                SourceLabel(source),
                $"位于 {world.BiomeName}",
                [
                    new("所在地区", world.BiomeName),
                    new("生成轮廓", StructurePatternName(world.RegionIndex)),
                    new("地图标注", "是"),
                ],
                world.BaseTile,
                world.RegionIndex));
            EnemyDefinition? enemy = EnemyCatalog.All.FirstOrDefault(item =>
                item.RequiredContentPack == source.Id && item.DisplayName == world.EnemyName);
            if (enemy is not null)
            {
                entries.Add(CreateEnemyEntry(enemy, source, world.BaseTile, world.RegionIndex));
            }
        }

        AddCharacterEntries(entries, source);
    }

    /// <summary>
    /// 把清单指定分类转换为通用图鉴条目，适用于没有额外运行参数的本体内容。
    /// </summary>
    private static void AddManifestEntries(
        List<CompendiumEntry> entries,
        ContentPackDefinition source,
        string additionCategory,
        CompendiumCategory category,
        string summary,
        CompendiumFact fact,
        IReadOnlyList<TileId> previewTiles)
    {
        ContentAddition[] additions = source.Additions
            .Where(item => item.Category == additionCategory)
            .ToArray();
        for (int index = 0; index < additions.Length; index++)
        {
            ContentAddition addition = additions[index];
            entries.Add(new CompendiumEntry(
                category, addition.Name, BaseSourceId, source.DisplayName,
                summary, [fact],
                previewTiles[Math.Min(index, previewTiles.Count - 1)], index % 3));
        }
    }

    /// <summary>
    /// 将清单角色加入图鉴，并明确区分“已登记角色”和“已有运行时角色实现”。
    /// </summary>
    private static void AddCharacterEntries(List<CompendiumEntry> entries, ContentPackDefinition source)
    {
        string sourceId = source.Number == 0 ? BaseSourceId : source.Id;
        foreach (ContentAddition character in source.Additions.Where(item => item.Category == "角色"))
        {
            bool playable = source.Number == 0 && character.Name == "博丽灵梦";
            string state = playable ? "当前可操作自机" : "内容目录角色，尚未制作运行时角色";
            entries.Add(new CompendiumEntry(
                CompendiumCategory.Character,
                character.Name,
                sourceId,
                SourceLabel(source),
                state,
                [new("实现状态", state, true)],
                source.Number == 0
                    ? TileId.ShrineGrassBase
                    : OfficialWorldContentCatalog.GetByPack(source.Id)[0].BaseTile,
                source.Number % 4));
        }
    }

    /// <summary>
    /// 从战斗定义生成敌人条目，展示生命、移动、解锁时间、掉落率和栖息地区。
    /// </summary>
    private static CompendiumEntry CreateEnemyEntry(
        EnemyDefinition enemy,
        ContentPackDefinition source,
        TileId previewTile,
        int previewVariant = 0)
    {
        string biomes = enemy.AllowedBiomes.Count == 0
            ? "所有地区"
            : string.Join("、", enemy.AllowedBiomes.Select(BiomeNames.GetChinese));
        string sourceId = source.Number == 0 ? BaseSourceId : source.Id;
        string special = enemy.ExplodesOnDeath ? "死亡时爆炸" : "无";
        EnemyBalanceProfile balance = EnemyBalanceProfile.Evaluate(enemy);
        return new CompendiumEntry(
            CompendiumCategory.Enemy,
            enemy.DisplayName,
            sourceId,
            SourceLabel(source),
            $"威胁 {balance.ThreatRank}/5 · {balance.CombatRole} · 生命 {enemy.MaxHealth}",
            [
                new("栖息地区", biomes, true),
                new("威胁等级", $"{balance.ThreatRank}/5 · {balance.ThreatLabel}"),
                new("战斗定位", balance.CombatRole),
                new("登场阶段", balance.ArrivalPhase),
                new("生命", enemy.MaxHealth.ToString()),
                new("移动速度", $"{enemy.MoveSpeed:0.#}"),
                new("刷新权重", $"{enemy.SpawnWeight:0.#}"),
                new("出现时间", $"{enemy.UnlockTime:0} 秒"),
                new("预计击破", $"{balance.BaseTimeToKill:0.0} 秒"),
                new("掉落率", $"{enemy.DropChance:P0}"),
                new("特殊", special),
            ],
            previewTile,
            previewVariant,
            enemy);
    }

    /// <summary>
    /// 为本体敌人选择其首个栖息地区的代表地表，通用敌人使用幻想乡草地。
    /// </summary>
    private static TileId GetEnemyPreviewTile(EnemyDefinition enemy)
    {
        if (enemy.AllowedBiomes.Count == 0)
        {
            return TileId.GrassBase;
        }

        return enemy.AllowedBiomes[0] switch
        {
            BiomeId.HakureiShrine => TileId.ShrineGrassBase,
            BiomeId.HumanVillage => TileId.DirtBase,
            BiomeId.MagicForest => TileId.ForestFloorBase,
            BiomeId.YoukaiMountain => TileId.MountainGrassBase,
            _ => TileId.GrassBase,
        };
    }

    /// <summary>
    /// 将包编号和名称组合为适合详情页显示的稳定来源标签。
    /// </summary>
    private static string SourceLabel(ContentPackDefinition source) =>
        source.Number == 0 ? source.DisplayName : $"TH{source.Number:00} {source.DisplayName}";

    /// <summary>
    /// 将三个结构生成层级转换为玩家可理解的轮廓名称。
    /// </summary>
    private static string StructurePatternName(int regionIndex) => regionIndex switch
    {
        0 => "据点",
        1 => "殿堂",
        _ => "秘境",
    };
}
