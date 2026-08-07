using TouhouWuxiaSurvivor.Content;
using TouhouWuxiaSurvivor.World.Biomes;
using TouhouWuxiaSurvivor.World.Structures;
using TouhouWuxiaSurvivor.World.Tiles;

namespace TouhouWuxiaSurvivor.World.Official;

/// <summary>
/// 集中注册 TH01 至 TH20 的可生成世界增量，并提供按包、群系和结构的稳定查询。
/// </summary>
public static class OfficialWorldContentCatalog
{
    public static IReadOnlyList<OfficialWorldContentDefinition> All { get; } =
    [
        D(1, 0, ContentPackIds.HighlyResponsiveToPrayers, BiomeId.HellRuins, "地狱遗迹", StructureId.HellGate, "地狱门", "封印恶灵", TileId.MagicSoilBase, TileId.MagicSoilSparkles),
        D(1, 1, ContentPackIds.HighlyResponsiveToPrayers, BiomeId.MakaiBorder, "魔界边境", StructureId.MakaiGate, "魔界门", "魔界邪眼", TileId.BoundarySoilBase, TileId.BoundarySoilSparkles),
        D(1, 2, ContentPackIds.HighlyResponsiveToPrayers, BiomeId.HakureiRuins, "博丽神社遗迹", StructureId.RuinedShrine, "崩坏神社", "阴阳玉怨灵", TileId.ShrineGrassBase, TileId.StoneCracks),
        D(2, 0, ContentPackIds.StoryOfEasternWonderland, BiomeId.DemonShrine, "妖怪侵占神社", StructureId.DemonTankWreck, "战车残骸", "战车妖怪", TileId.DirtBase, TileId.StoneCracks),
        D(2, 1, ContentPackIds.StoryOfEasternWonderland, BiomeId.DreamWorld, "幻梦界", StructureId.DreamBoundary, "幻梦界壁", "幻梦界魔石", TileId.BoundarySoilBase, TileId.BoundarySoilSparkles),
        D(2, 2, ContentPackIds.StoryOfEasternWonderland, BiomeId.ReimadenGrounds, "灵魔殿境内", StructureId.Reimaden, "灵魔殿", "灵魔殿使魔", TileId.MagicSoilBase, TileId.MagicSoilSparkles),
        D(3, 0, ContentPackIds.PhantasmagoriaOfDimDream, BiomeId.ProbabilitySpace, "概率空间", StructureId.ProbabilityHypervessel, "可能性空间移动船", "时空幻影", TileId.StoneBase, TileId.StoneCracks),
        D(3, 1, ContentPackIds.PhantasmagoriaOfDimDream, BiomeId.VinaRuins, "维纳遗迹", StructureId.VinaRuinsCore, "维纳遗迹核心", "北白河机械灵", TileId.DirtBase, TileId.MagicSoilSparkles),
        D(3, 2, ContentPackIds.PhantasmagoriaOfDimDream, BiomeId.DimDreamArena, "梦时空决斗场", StructureId.DimDreamArena, "梦时空竞技台", "可能性复制体", TileId.BoundarySoilBase, TileId.StoneCracks),
        D(4, 0, ContentPackIds.LotusLandStory, BiomeId.DreamyWorld, "梦幻世界", StructureId.DreamPortal, "梦幻世界入口", "梦幻花妖", TileId.ForestFloorBase, TileId.MagicSoilSparkles),
        D(4, 1, ContentPackIds.LotusLandStory, BiomeId.FantasyFlowerField, "幻想花田", StructureId.FlowerFieldPavilion, "花田凉亭", "幻想莲灵", TileId.ShrineGrassBase, TileId.ShrineGrassPetals),
        D(4, 2, ContentPackIds.LotusLandStory, BiomeId.MugenkanGarden, "梦幻馆庭园", StructureId.Mugenkan, "梦幻馆", "梦幻馆卫灵", TileId.MossBase, TileId.ForestFloorLeaves),
        D(5, 0, ContentPackIds.MysticSquare, BiomeId.MysticMakaiBorder, "魔界边境都市", StructureId.MysticMakaiGate, "魔界都市门", "魔界魔法阵", TileId.BoundarySoilBase, TileId.MagicSoilSparkles),
        D(5, 1, ContentPackIds.MysticSquare, BiomeId.MakaiCity, "魔界都市", StructureId.MakaiCitySquare, "魔界中央广场", "魔界居民", TileId.StoneBase, TileId.BoundarySoilSparkles),
        D(5, 2, ContentPackIds.MysticSquare, BiomeId.PandemoniumGrounds, "万魔殿境内", StructureId.Pandemonium, "万魔殿", "万魔殿使魔", TileId.MagicSoilBase, TileId.StoneCracks),
        D(6, 0, ContentPackIds.EmbodimentOfScarletDevil, BiomeId.MistyLake, "雾之湖", StructureId.LakeIsland, "雾湖小岛", "湖上妖精", TileId.LakeWaterBase, TileId.LakeWaterRipples),
        D(6, 1, ContentPackIds.EmbodimentOfScarletDevil, BiomeId.ScarletDevilMansionGrounds, "红魔馆领地", StructureId.ScarletDevilMansion, "红魔馆", "红雾妖虫", TileId.WetGrassBase, TileId.BoundarySoilSparkles),
        D(6, 2, ContentPackIds.EmbodimentOfScarletDevil, BiomeId.VoileLibrary, "巴瓦鲁魔法图书馆", StructureId.VoileLibrary, "大图书馆", "使魔书灵", TileId.MagicSoilBase, TileId.StoneCracks),
        D(7, 0, ContentPackIds.PerfectCherryBlossom, BiomeId.WinterCherryForest, "冬樱林", StructureId.NetherworldBarrier, "冥界结界", "冬季妖精", TileId.ShrineGrassBase, TileId.StoneCracks),
        D(7, 1, ContentPackIds.PerfectCherryBlossom, BiomeId.Netherworld, "冥界", StructureId.GhostGate, "幽明结界门", "幽灵", TileId.ShrineGrassBase, TileId.ShrineGrassPetals),
        D(7, 2, ContentPackIds.PerfectCherryBlossom, BiomeId.HakugyokurouGrounds, "白玉楼庭园", StructureId.Hakugyokurou, "白玉楼", "樱花亡灵", TileId.WetGrassBase, TileId.ShrineGrassPetals),
        D(8, 0, ContentPackIds.ImperishableNight, BiomeId.BambooForest, "迷途竹林", StructureId.BambooTrail, "竹林古道", "竹叶妖", TileId.BambooFloorBase, TileId.BambooFloorLeaves),
        D(8, 1, ContentPackIds.ImperishableNight, BiomeId.EienteiGrounds, "永远亭境内", StructureId.Eientei, "永远亭", "月兔", TileId.BambooMossBase, TileId.ShrinePathPebbles),
        D(8, 2, ContentPackIds.ImperishableNight, BiomeId.FalseMoonSpace, "伪月空间", StructureId.FalseMoonAltar, "伪月祭坛", "永夜虫", TileId.BoundarySoilBase, TileId.WaterShallowRipples),
        D(9, 0, ContentPackIds.PhantasmagoriaOfFlowerView, BiomeId.NamelessHill, "无名之丘", StructureId.PoisonFlowerField, "铃兰花田", "铃兰毒灵", TileId.WetGrassBase, TileId.WetGrassDroplets),
        D(9, 1, ContentPackIds.PhantasmagoriaOfFlowerView, BiomeId.Muenzuka, "无缘塚", StructureId.MuenzukaGraveyard, "无缘塚墓园", "魂花", TileId.StoneBase, TileId.ShrineGrassPetals),
        D(9, 2, ContentPackIds.PhantasmagoriaOfFlowerView, BiomeId.GardenOfTheSun, "太阳花田", StructureId.SunflowerGarden, "太阳花园", "太阳花妖", TileId.GrassBase, TileId.MountainGrassFlowers),
        D(10, 0, ContentPackIds.MountainOfFaith, BiomeId.GreatYoukaiWaterfall, "妖怪之山大瀑布", StructureId.KappaWorkshop, "河童工房", "河童斥候", TileId.StreamStoneBase, TileId.StreamStoneWet),
        D(10, 1, ContentPackIds.MountainOfFaith, BiomeId.WindGodLake, "风神之湖", StructureId.LakeTorii, "风神湖鸟居", "天狗巡卫", TileId.LakeWaterBase, TileId.MountainGrassFlowers),
        D(10, 2, ContentPackIds.MountainOfFaith, BiomeId.MoriyaShrineGrounds, "守矢神社境内", StructureId.MoriyaShrine, "守矢神社", "信仰灵", TileId.ShrineGrassBase, TileId.ShrinePathPebbles),
        D(11, 0, ContentPackIds.SubterraneanAnimism, BiomeId.UnderworldPassage, "地底通道", StructureId.BridgeOfJealousy, "嫉妒之桥", "地底怨灵", TileId.StoneBase, TileId.StoneCracks),
        D(11, 1, ContentPackIds.SubterraneanAnimism, BiomeId.FormerHell, "旧地狱街道", StructureId.FormerHellCity, "旧都", "地狱鸦", TileId.DirtBase, TileId.MagicSoilSparkles),
        D(11, 2, ContentPackIds.SubterraneanAnimism, BiomeId.PalaceOfEarthSpiritsGrounds, "地灵殿境内", StructureId.PalaceOfEarthSpirits, "地灵殿", "火车猫灵", TileId.MagicSoilBase, TileId.StoneCracks),
        D(12, 0, ContentPackIds.UndefinedFantasticObject, BiomeId.SpringCloudSea, "春云之海", StructureId.CloudTreasure, "云海宝船遗物", "飞天妖怪", TileId.WaterShallowBase, TileId.WaterShallowRipples),
        D(12, 1, ContentPackIds.UndefinedFantasticObject, BiomeId.PalanquinShipDeck, "圣辇船甲板", StructureId.PalanquinShip, "圣辇船", "飞仓灵", TileId.StoneBase, TileId.ShrinePathPebbles),
        D(12, 2, ContentPackIds.UndefinedFantasticObject, BiomeId.Hokkai, "法界", StructureId.HokkaiSeal, "法界封印", "法界魔灵", TileId.MagicSoilBase, TileId.BoundarySoilSparkles),
        D(13, 0, ContentPackIds.TenDesires, BiomeId.NightCemetery, "夜樱墓地", StructureId.CemeteryGate, "命莲寺墓园门", "僵尸", TileId.WetGrassBase, TileId.StoneCracks),
        D(13, 1, ContentPackIds.TenDesires, BiomeId.HallOfDreams, "梦殿大祀庙", StructureId.HallOfDreamsGate, "梦殿石门", "欲灵", TileId.StoneBase, TileId.ShrinePathBase),
        D(13, 2, ContentPackIds.TenDesires, BiomeId.DivineSpiritMausoleum, "神灵庙深处", StructureId.DivineSpiritTemple, "神灵庙", "道士灵", TileId.ShrineGrassBase, TileId.BoundarySoilSparkles),
        D(14, 0, ContentPackIds.DoubleDealingCharacter, BiomeId.StormyLake, "暴风湖面", StructureId.AbandonedInstrumentPile, "遗弃乐器堆", "水栖妖怪", TileId.LakeWaterBase, TileId.LakeWaterRipples),
        D(14, 1, ContentPackIds.DoubleDealingCharacter, BiomeId.TsukumogamiSky, "付丧神云海", StructureId.ThunderDrumStage, "雷鼓舞台", "付丧神", TileId.BoundarySoilBase, TileId.MagicSoilSparkles),
        D(14, 2, ContentPackIds.DoubleDealingCharacter, BiomeId.ShiningNeedleRealm, "逆转天", StructureId.ShiningNeedleCastle, "辉针城", "小人卫兵", TileId.StoneBase, TileId.BoundarySoilSparkles),
        D(15, 0, ContentPackIds.LegacyOfLunaticKingdom, BiomeId.LunarRainbowSea, "月之虹海", StructureId.LunarTransferGate, "月面传送门", "月兔兵", TileId.WaterShallowBase, TileId.WaterShallowRipples),
        D(15, 1, ContentPackIds.LegacyOfLunaticKingdom, BiomeId.LunarCapital, "月之都", StructureId.LunarCapitalGate, "月都门", "月都卫兵", TileId.StoneBase, TileId.ShrinePathPebbles),
        D(15, 2, ContentPackIds.LegacyOfLunaticKingdom, BiomeId.SeaOfTranquility, "静海", StructureId.TranquilityOutpost, "静海前哨", "纯化灵", TileId.MountainRockBase, TileId.WaterShallowRipples),
        D(16, 0, ContentPackIds.HiddenStarInFourSeasons, BiomeId.FourSeasonsForest, "四季异变林", StructureId.SeasonAltar, "季节祭坛", "季节妖精", TileId.ForestFloorBase, TileId.ShrineGrassPetals),
        D(16, 1, ContentPackIds.HiddenStarInFourSeasons, BiomeId.LandOfBackdoors, "后户之国", StructureId.BackDoor, "秘神后户", "后户舞童", TileId.BoundarySoilBase, TileId.BoundarySoilSparkles),
        D(16, 2, ContentPackIds.HiddenStarInFourSeasons, BiomeId.HiddenStarSanctum, "隐岐奈秘境", StructureId.HiddenGodStage, "秘神舞台", "秘神侍从", TileId.MagicSoilBase, TileId.ShrinePathPebbles),
        D(17, 0, ContentPackIds.WilyBeastAndWeakestCreature, BiomeId.HellCheckpoint, "地狱关口", StructureId.SanzuCheckpoint, "三途关卡", "兽灵", TileId.DirtBase, TileId.StoneCracks),
        D(17, 1, ContentPackIds.WilyBeastAndWeakestCreature, BiomeId.AnimalRealm, "畜生界", StructureId.BeastClanBattlefield, "兽组织战场", "鹰灵", TileId.StoneBase, TileId.MagicSoilSparkles),
        D(17, 2, ContentPackIds.WilyBeastAndWeakestCreature, BiomeId.PrimateSpiritGardenGrounds, "灵长园境内", StructureId.PrimateSpiritGarden, "灵长园", "埴轮兵", TileId.BoundarySoilBase, TileId.ShrinePathPebbles),
        D(18, 0, ContentPackIds.UnconnectedMarketeers, BiomeId.MountainMarket, "妖怪之山集市", StructureId.CardMarket, "能力卡摊位", "卡牌妖怪", TileId.MountainGrassBase, TileId.DirtPebbles),
        D(18, 1, ContentPackIds.UnconnectedMarketeers, BiomeId.RainbowDragonCave, "虹龙洞", StructureId.RainbowDragonMine, "虹龙洞矿场", "矿洞蜈蚣", TileId.MountainRockBase, TileId.MagicSoilSparkles),
        D(18, 2, ContentPackIds.UnconnectedMarketeers, BiomeId.LunarRainbowCliff, "月虹悬崖", StructureId.RainbowMarketAltar, "月虹市场祭坛", "天虹灵", TileId.BoundarySoilBase, TileId.MountainGrassFlowers),
        D(19, 0, ContentPackIds.UnfinishedDreamOfAllLivingGhost, BiomeId.BeastTrail, "兽灵争道", StructureId.BeastTrailMarker, "兽道界碑", "游荡兽灵", TileId.DirtBase, TileId.DirtPebbles),
        D(19, 1, ContentPackIds.UnfinishedDreamOfAllLivingGhost, BiomeId.HellMarket, "地狱市场", StructureId.HellMarketStall, "地狱交易摊", "市场怨灵", TileId.StoneBase, TileId.MagicSoilSparkles),
        D(19, 2, ContentPackIds.UnfinishedDreamOfAllLivingGhost, BiomeId.BeastKingGarden, "兽王园", StructureId.BeastKingArena, "兽王竞技场", "组织战士", TileId.GrassBase, TileId.BoundarySoilSparkles),
        D(20, 0, ContentPackIds.FossilizedWonders, BiomeId.Sanctuary, "山麓圣域", StructureId.SanctuaryGate, "圣域门", "山姥", TileId.MountainGrassBase, TileId.MountainRockCracks),
        D(20, 1, ContentPackIds.FossilizedWonders, BiomeId.ZoltaxianLabyrinth, "佐尔塔克斯迷宫", StructureId.ZoltaxianCore, "迷宫核心", "异变石灵", TileId.BoundarySoilBase, TileId.MagicSoilSparkles),
        D(20, 2, ContentPackIds.FossilizedWonders, BiomeId.AsamaPurificationMountain, "浅间净化山", StructureId.UndergroundPyramid, "地下金字塔", "月都净化兵", TileId.MountainRockBase, TileId.MountainRockCracks),
    ];
    private static readonly IReadOnlyDictionary<BiomeId, OfficialWorldContentDefinition> ByBiome =
        All.ToDictionary(item => item.Biome);
    private static readonly IReadOnlyDictionary<StructureId, OfficialWorldContentDefinition> ByStructure =
        All.ToDictionary(item => item.Structure);
    private static readonly IReadOnlyDictionary<string, IReadOnlyList<OfficialWorldContentDefinition>> ByPack =
        All.GroupBy(item => item.PackId, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group =>
                (IReadOnlyList<OfficialWorldContentDefinition>)group.OrderBy(item => item.RegionIndex).ToArray(),
                StringComparer.Ordinal);

    /// <summary>
    /// 按群系查找正作定义；本体群系不会返回定义。
    /// </summary>
    public static bool TryGet(BiomeId biome, out OfficialWorldContentDefinition definition) =>
        ByBiome.TryGetValue(biome, out definition!);

    /// <summary>
    /// 按结构查找正作定义；本体结构不会返回定义。
    /// </summary>
    public static bool TryGet(StructureId structure, out OfficialWorldContentDefinition definition) =>
        ByStructure.TryGetValue(structure, out definition!);

    /// <summary>
    /// 按包标识符返回全部地区定义；未知包返回空数组，调用方无需处理 null。
    /// </summary>
    public static IReadOnlyList<OfficialWorldContentDefinition> GetByPack(string packId) =>
        ByPack.TryGetValue(packId, out IReadOnlyList<OfficialWorldContentDefinition>? definitions)
            ? definitions
            : Array.Empty<OfficialWorldContentDefinition>();

    /// <summary>
    /// 缩短目录声明行并保持构造参数顺序只在一个位置定义。
    /// </summary>
    private static OfficialWorldContentDefinition D(
        int number, int regionIndex, string packId, BiomeId biome, string biomeName,
        StructureId structure, string structureName, string enemyName,
        TileId baseTile, TileId detailTile) =>
        new(number, regionIndex, packId, biome, biomeName, structure, structureName,
            enemyName, baseTile, detailTile);
}
