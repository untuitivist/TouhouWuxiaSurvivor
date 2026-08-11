using System.Security.Cryptography;
using System.Text;

namespace TouhouWuxiaSurvivor.Content.Characters;

/// <summary>
/// 从本体与全部正作清单建立规范角色目录，并合并跨作品重复出现的同一人物。
/// </summary>
public static class CharacterCatalog
{
    private const string CharacterCategory = "角色";
    private static IReadOnlyList<CharacterDefinition>? _all;

    public static IReadOnlyList<CharacterDefinition> All => _all ??= LoadAll();
    public static CharacterDefinition Default => GetRequiredByDisplayName("博丽灵梦");

    /// <summary>
    /// 按稳定角色标识符查找定义；不存在时返回空值，让调用方自行决定错误策略。
    /// </summary>
    public static CharacterDefinition? FindById(string characterId) => All.FirstOrDefault(
        character => string.Equals(character.CharacterId, characterId, StringComparison.Ordinal));

    /// <summary>
    /// 按规范化中文显示名查找角色，兼容清单中可能存在的首尾空白和宽度差异。
    /// </summary>
    public static CharacterDefinition? FindByDisplayName(string displayName)
    {
        string canonicalName = CanonicalizeName(displayName);
        return All.FirstOrDefault(character => string.Equals(
            CanonicalizeName(character.DisplayName), canonicalName, StringComparison.Ordinal));
    }

    /// <summary>
    /// 按标识符取得必然存在的角色；无效标识会抛出包含原值的明确异常。
    /// </summary>
    public static CharacterDefinition GetRequired(string characterId) =>
        FindById(characterId) ?? throw new KeyNotFoundException(
            $"Character id is not registered: {characterId}");

    /// <summary>
    /// 按显示名取得必然存在的角色；用于默认角色等受清单契约保证的入口。
    /// </summary>
    public static CharacterDefinition GetRequiredByDisplayName(string displayName) =>
        FindByDisplayName(displayName) ?? throw new KeyNotFoundException(
            $"Character name is not registered: {displayName}");

    /// <summary>
    /// 按本局内容选择返回可用角色；本体始终有效，多来源角色启用任一来源即可出现。
    /// </summary>
    public static IReadOnlyList<CharacterDefinition> GetAvailable(ContentPackSelection selection) =>
        All.Where(character => IsAvailable(character, selection)).ToArray();

    /// <summary>
    /// 判断角色是否由本体或至少一个已启用来源包提供，不依赖其首个登记来源。
    /// </summary>
    public static bool IsAvailable(
        CharacterDefinition character,
        ContentPackSelection selection) => character.AvailableSourcePackIds.Any(
            packId => string.Equals(packId, ContentPackCatalog.Base.Id, StringComparison.Ordinal) ||
                selection.IsEnabled(packId));

    /// <summary>
    /// 依次读取本体与 TH01 至 TH20 的角色条目，以规范名合并重复身份并保持首次出现顺序。
    /// </summary>
    private static IReadOnlyList<CharacterDefinition> LoadAll()
    {
        var order = new List<string>();
        var primarySources = new Dictionary<string, ContentPackDefinition>(StringComparer.Ordinal);
        var primaryOrdinals = new Dictionary<string, int>(StringComparer.Ordinal);
        var availableSources = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        IEnumerable<ContentPackDefinition> packs = [ContentPackCatalog.Base, .. ContentPackCatalog.All];
        foreach (ContentPackDefinition pack in packs)
        {
            ContentAddition[] characters = pack.Additions.Where(
                item => string.Equals(item.Category, CharacterCategory, StringComparison.Ordinal)).ToArray();
            for (int sourceOrdinal = 0; sourceOrdinal < characters.Length; sourceOrdinal++)
            {
                ContentAddition addition = characters[sourceOrdinal];
                string name = CanonicalizeName(addition.Name);
                if (!availableSources.TryGetValue(name, out List<string>? sourceIds))
                {
                    sourceIds = [];
                    availableSources.Add(name, sourceIds);
                    primarySources.Add(name, pack);
                    primaryOrdinals.Add(name, sourceOrdinal);
                    order.Add(name);
                }

                if (!sourceIds.Contains(pack.Id, StringComparer.Ordinal))
                {
                    sourceIds.Add(pack.Id);
                }
            }
        }

        return order.Select(name => CreateDefinition(
            name, primarySources[name], primaryOrdinals[name], availableSources[name])).ToArray();
    }

    /// <summary>
    /// 根据规范名的稳定摘要生成身份与平衡档案，使清单遍历顺序变化不会改写角色数值。
    /// </summary>
    private static CharacterDefinition CreateDefinition(
        string displayName,
        ContentPackDefinition primarySource,
        int primaryOrdinal,
        IReadOnlyList<string> availableSourceIds)
    {
        string characterId = $"character_{primarySource.Id}_{primaryOrdinal:00}";
        byte[] digest = SHA256.HashData(Encoding.UTF8.GetBytes(characterId));
        var playable = new PlayableCharacterProfile(
            4.0f + digest[8] % 3,
            0.92f + digest[9] % 9 * 0.02f,
            0.92f + digest[10] % 9 * 0.02f);
        var boss = new BossCharacterProfile(
            700.0f + digest[11] % 11 * 50.0f,
            30.0f + digest[12] % 9 * 2.0f,
            8.0f + digest[13] % 9,
            16.0f + digest[14] % 7);
        return new CharacterDefinition(characterId, primarySource.Id, primarySource.Number,
            displayName, availableSourceIds, playable, boss);
    }

    /// <summary>
    /// 使用兼容等价规范化统一角色名，使相同中文名可靠合并为单一角色身份。
    /// </summary>
    private static string CanonicalizeName(string displayName) =>
        displayName.Trim().Normalize(NormalizationForm.FormKC);
}
