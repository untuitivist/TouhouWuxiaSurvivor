using TouhouWuxiaSurvivor.World.Structures;

namespace TouhouWuxiaSurvivor.World.Rendering;

/// <summary>
/// 按结构在幻想乡中的语义选择俯视轮廓；这里集中维护跨作品结构分类而不污染生成数据。
/// </summary>
public static class InternalStructureShapeResolver
{
    /// <summary>
    /// 将每个可生成结构映射到明确轮廓，新增结构未登记时使用遗迹而非误画成红魔馆。
    /// </summary>
    public static InternalStructureShape Resolve(StructureId structure) => structure switch
    {
        StructureId.HakureiShrine or StructureId.ShrineCourt or
            StructureId.MoriyaShrine or StructureId.RuinedShrine => InternalStructureShape.Shrine,
        StructureId.HumanVillage or StructureId.FormerHellCity or
            StructureId.MakaiCitySquare => InternalStructureShape.Settlement,
        StructureId.MagicCircle or StructureId.FalseMoonAltar or
            StructureId.SeasonAltar or
            StructureId.ZoltaxianCore => InternalStructureShape.Circle,
        StructureId.LakeIsland or StructureId.Mugenkan or StructureId.Hakugyokurou or
            StructureId.PoisonFlowerField or StructureId.SunflowerGarden or
            StructureId.PrimateSpiritGarden => InternalStructureShape.Garden,
        StructureId.ScarletDevilMansion or StructureId.Reimaden or
            StructureId.Pandemonium or StructureId.PalaceOfEarthSpirits or
            StructureId.DivineSpiritTemple or StructureId.Eientei or
            StructureId.VoileLibrary or StructureId.HallOfDreamsGate => InternalStructureShape.Manor,
        StructureId.MountainTerrace => InternalStructureShape.Terrace,
        StructureId.BambooTrail or StructureId.Crossroads or
            StructureId.DreamBoundary or StructureId.BeastTrailMarker => InternalStructureShape.Crossroads,
        StructureId.HellGate or StructureId.MakaiGate or StructureId.MysticMakaiGate or
            StructureId.NetherworldBarrier or StructureId.GhostGate or
            StructureId.LunarCapitalGate or StructureId.BackDoor or
            StructureId.LunarTransferGate or StructureId.CemeteryGate or
            StructureId.SanctuaryGate => InternalStructureShape.Gate,
        StructureId.DemonTankWreck or StructureId.AbandonedInstrumentPile or
            StructureId.VinaRuinsCore => InternalStructureShape.Ruin,
        StructureId.BridgeOfJealousy => InternalStructureShape.Bridge,
        StructureId.PalanquinShip or StructureId.ProbabilityHypervessel => InternalStructureShape.Ship,
        StructureId.DimDreamArena or StructureId.BeastKingArena or
            StructureId.ThunderDrumStage or StructureId.HiddenGodStage or
            StructureId.BeastClanBattlefield => InternalStructureShape.Stage,
        StructureId.ShiningNeedleCastle or StructureId.UndergroundPyramid => InternalStructureShape.Tower,
        StructureId.CardMarket or StructureId.RainbowMarketAltar or
            StructureId.HellMarketStall => InternalStructureShape.Market,
        StructureId.KappaWorkshop or StructureId.RainbowDragonMine or
            StructureId.HokkaiSeal => InternalStructureShape.Cave,
        StructureId.CloudTreasure or StructureId.LakeTorii or StructureId.DreamPortal or
            StructureId.FlowerFieldPavilion or StructureId.TranquilityOutpost or
            StructureId.SanzuCheckpoint => InternalStructureShape.Outpost,
        _ => InternalStructureShape.Ruin,
    };
}
