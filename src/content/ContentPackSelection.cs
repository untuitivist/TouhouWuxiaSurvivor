namespace TouhouWuxiaSurvivor.Content;

/// <summary>
/// 表示一局游戏启用的不可变可选内容包集合；本体不在集合中且始终有效。
/// </summary>
public sealed class ContentPackSelection
{
    private readonly HashSet<string> _enabled;
    private readonly IReadOnlyList<string> _enabledPackIds;
    public static ContentPackSelection BaseOnly { get; } = new([]);
    public IReadOnlyList<string> EnabledPackIds => _enabledPackIds;

    /// <summary>
    /// 从内容包标识符建立去重快照，后续菜单改动不会影响已经开始的世界。
    /// </summary>
    public ContentPackSelection(IEnumerable<string> enabledPackIds)
    {
        ArgumentNullException.ThrowIfNull(enabledPackIds);
        string[] normalized = enabledPackIds
            .Select(id => string.IsNullOrWhiteSpace(id)
                ? throw new ArgumentException("Content pack id cannot be empty.", nameof(enabledPackIds))
                : id)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();
        _enabledPackIds = Array.AsReadOnly(normalized);
        _enabled = new HashSet<string>(normalized, StringComparer.Ordinal);
    }

    /// <summary>
    /// 判断指定可选内容包是否存在于本局快照中。
    /// </summary>
    public bool IsEnabled(string packId) => _enabled.Contains(packId);

    /// <summary>
    /// 生成用于 HUD 的内容摘要；没有可选包时明确显示仅启用本体。
    /// </summary>
    public string Describe()
    {
        string[] names = ContentPackCatalog.All
            .Where(pack => IsEnabled(pack.Id))
            .Select(pack => pack.DisplayName)
            .ToArray();
        return names.Length switch
        {
            0 => "幻想乡本体",
            <= 3 => $"幻想乡本体 + {string.Join(" + ", names)}",
            _ => $"幻想乡本体 + {names.Length} 个正作包",
        };
    }
}
