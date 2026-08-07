using TouhouWuxiaSurvivor.Actors.Enemies;
using TouhouWuxiaSurvivor.World.Tiles;
using TouhouWuxiaSurvivor.Gameplay.SpellCards.Definitions;

namespace TouhouWuxiaSurvivor.Ui.Compendium;

/// <summary>
/// 保存一个图鉴条目的分类、来源、列表摘要和完整详情。
/// </summary>
public sealed class CompendiumEntry
{
    public CompendiumCategory Category { get; }
    public string Name { get; }
    public string SourceId { get; }
    public string SourceName { get; }
    public string Summary { get; }
    public IReadOnlyList<CompendiumFact> Facts { get; }
    public string Details => string.Join("\n", Facts.Select(fact => $"{fact.Label}：{fact.Value}"));
    public TileId PreviewTile { get; }
    public int PreviewVariant { get; }
    public EnemyDefinition? Enemy { get; }
    public SpellCardDefinition? SpellCard { get; }

    /// <summary>
    /// 构造不可变图鉴条目，使列表和详情面板共享完全相同的数据快照。
    /// </summary>
    public CompendiumEntry(
        CompendiumCategory category,
        string name,
        string sourceId,
        string sourceName,
        string summary,
        IReadOnlyList<CompendiumFact> facts,
        TileId previewTile = TileId.GrassBase,
        int previewVariant = 0,
        EnemyDefinition? enemy = null,
        SpellCardDefinition? spellCard = null)
    {
        Category = category;
        Name = name;
        SourceId = sourceId;
        SourceName = sourceName;
        Summary = summary;
        Facts = facts;
        PreviewTile = previewTile;
        PreviewVariant = previewVariant;
        Enemy = enemy;
        SpellCard = spellCard;
    }
}
