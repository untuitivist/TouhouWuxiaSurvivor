namespace TouhouWuxiaSurvivor.Ui.Compendium;

/// <summary>
/// 组合世界、敌人、角色、武学、奥义投影器，并在最后统一补全素材来源。
/// </summary>
public static class CompendiumCatalog
{
    public const string BaseSourceId = "base";
    public static IReadOnlyList<CompendiumEntry> All { get; } = BuildAll();

    /// <summary>
    /// 按固定分类组合运行时目录，内容与数值仍由各自领域目录保持唯一真源。
    /// </summary>
    private static IReadOnlyList<CompendiumEntry> BuildAll()
    {
        var entries = new List<CompendiumEntry>();
        entries.AddRange(WorldCompendiumEntryFactory.CreateAll());
        entries.AddRange(EnemyCompendiumEntryFactory.CreateAll());
        entries.AddRange(CharacterCompendiumEntryFactory.CreateAll());
        entries.AddRange(RunUpgradeCompendiumEntryFactory.CreateAll());
        entries.AddRange(SpellCardCompendiumEntryFactory.CreateAll());
        var provenance = new CompendiumVisualProvenanceCatalog();
        return entries.Select(provenance.Enrich).ToArray();
    }

}
