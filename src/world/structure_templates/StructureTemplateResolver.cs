using TouhouWuxiaSurvivor.World.Structures;

namespace TouhouWuxiaSurvivor.World.StructureTemplates;

/// <summary>
/// 集中维护所有结构的语义模板分类，使地表压印与上层素材始终使用同一轮廓。
/// </summary>
public static class StructureTemplateResolver
{
    /// <summary>
    /// 为每个已登记地标返回明确模板；未知项使用遗迹，避免错误表现成某座特定建筑。
    /// </summary>
    public static StructureTemplateKind Resolve(StructureId structure) => structure switch
    {
        StructureId.HakureiShrine or StructureId.ShrineCourt or StructureId.MoriyaShrine or
            StructureId.RuinedShrine => StructureTemplateKind.Shrine,
        StructureId.HumanVillage or StructureId.FormerHellCity or StructureId.MakaiCitySquare =>
            StructureTemplateKind.Settlement,
        StructureId.MagicCircle or StructureId.FalseMoonAltar or StructureId.SeasonAltar or
            StructureId.ZoltaxianCore => StructureTemplateKind.Circle,
        StructureId.LakeIsland or StructureId.Mugenkan or StructureId.Hakugyokurou or
            StructureId.PoisonFlowerField or StructureId.SunflowerGarden or
            StructureId.PrimateSpiritGarden => StructureTemplateKind.Garden,
        StructureId.ScarletDevilMansion or StructureId.Reimaden or StructureId.Pandemonium or
            StructureId.PalaceOfEarthSpirits or StructureId.DivineSpiritTemple or
            StructureId.Eientei or StructureId.VoileLibrary or StructureId.HallOfDreamsGate =>
            StructureTemplateKind.Manor,
        StructureId.MountainTerrace => StructureTemplateKind.Terrace,
        StructureId.BambooTrail or StructureId.Crossroads or StructureId.DreamBoundary or
            StructureId.BeastTrailMarker => StructureTemplateKind.Crossroads,
        StructureId.HellGate or StructureId.MakaiGate or StructureId.MysticMakaiGate or
            StructureId.NetherworldBarrier or StructureId.GhostGate or
            StructureId.LunarCapitalGate or StructureId.BackDoor or
            StructureId.LunarTransferGate or StructureId.CemeteryGate or
            StructureId.SanctuaryGate => StructureTemplateKind.Gate,
        StructureId.DemonTankWreck or StructureId.AbandonedInstrumentPile or
            StructureId.VinaRuinsCore => StructureTemplateKind.Ruin,
        StructureId.BridgeOfJealousy => StructureTemplateKind.Bridge,
        StructureId.PalanquinShip or StructureId.ProbabilityHypervessel => StructureTemplateKind.Ship,
        StructureId.DimDreamArena or StructureId.BeastKingArena or StructureId.ThunderDrumStage or
            StructureId.HiddenGodStage or StructureId.BeastClanBattlefield => StructureTemplateKind.Stage,
        StructureId.ShiningNeedleCastle or StructureId.UndergroundPyramid => StructureTemplateKind.Tower,
        StructureId.CardMarket or StructureId.RainbowMarketAltar or StructureId.HellMarketStall =>
            StructureTemplateKind.Market,
        StructureId.KappaWorkshop or StructureId.RainbowDragonMine or StructureId.HokkaiSeal =>
            StructureTemplateKind.Cave,
        StructureId.CloudTreasure or StructureId.LakeTorii or StructureId.DreamPortal or
            StructureId.FlowerFieldPavilion or StructureId.TranquilityOutpost or
            StructureId.SanzuCheckpoint => StructureTemplateKind.Outpost,
        _ => StructureTemplateKind.Ruin,
    };
}
