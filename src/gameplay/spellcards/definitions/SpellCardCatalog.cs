using TouhouWuxiaSurvivor.Content;

namespace TouhouWuxiaSurvivor.Gameplay.SpellCards.Definitions;

/// <summary>
/// 汇总全部作品清单中的符卡，并提供按内容包、角色和稳定 ID 查询的只读目录。
/// </summary>
public static class SpellCardCatalog
{
    private static IReadOnlyList<SpellCardDefinition>? _all;

    public static IReadOnlyList<SpellCardDefinition> All => _all ??= LoadAll();

    /// <summary>
    /// 按稳定 ID 返回唯一符卡；重复 ID 会在目录加载阶段被拒绝，不存在时返回空值。
    /// </summary>
    public static SpellCardDefinition? FindById(string id) => All.FirstOrDefault(
        card => string.Equals(card.Id, id, StringComparison.Ordinal));

    /// <summary>
    /// 返回本局已启用作品的符卡传承；本体奥义始终可用，可选正作仍按本局勾选隔离。
    /// </summary>
    public static IReadOnlyList<SpellCardDefinition> GetEnabled(ContentPackSelection selection) =>
        All.Where(card => string.Equals(
                card.SourcePackId, ContentPackCatalog.Base.Id, StringComparison.Ordinal) ||
            selection.IsEnabled(card.SourcePackId)).ToArray();

    /// <summary>
    /// 返回指定角色在原作目录中登记的代表符卡，供 Boss 签名攻击和图鉴归属查询。
    /// </summary>
    public static IReadOnlyList<SpellCardDefinition> GetByOwner(string characterId) =>
        All.Where(card => string.Equals(
            card.OwnerCharacterId, characterId, StringComparison.Ordinal)).ToArray();

    /// <summary>
    /// 先读取常驻本体，再按作品编号读取全部可选包，并在加载边界拒绝重复稳定 ID。
    /// </summary>
    private static IReadOnlyList<SpellCardDefinition> LoadAll()
    {
        var result = new List<SpellCardDefinition>();
        result.AddRange(SpellCardManifestLoader.Load(
            "res://content/base/pack.json", ContentPackCatalog.Base.Id));
        foreach (ContentPackDefinition pack in ContentPackCatalog.All)
        {
            result.AddRange(SpellCardManifestLoader.Load(
                $"res://content/packs/{pack.Id}/pack.json", pack.Id));
        }

        string? duplicateId = result.GroupBy(card => card.Id, StringComparer.Ordinal)
            .FirstOrDefault(group => group.Count() > 1)?.Key;
        if (duplicateId is not null)
        {
            throw new InvalidDataException($"Duplicate spell card id: {duplicateId}");
        }

        return result;
    }
}
