using System.Collections.Frozen;

namespace TouhouWuxiaSurvivor.Content;

/// <summary>
/// 保存一个内容包的身份、开发状态、可选状态和分类内容清单。
/// </summary>
public sealed class ContentPackDefinition
{
    public int SchemaVersion { get; }
    public string ContentVersion { get; }
    public string HostApi { get; }
    public ContentPackKind Kind { get; }
    public string Id { get; }
    public int Number { get; }
    public string DisplayName { get; }
    public string EnglishName { get; }
    public ContentPackStatus Status { get; }
    public bool Selectable { get; }
    public IReadOnlyList<string> RequiredDependencies { get; }
    public IReadOnlyList<string> OptionalDependencies { get; }
    public IReadOnlySet<string> Capabilities { get; }
    public IReadOnlyList<ContentAddition> Additions { get; }
    public string ManifestPath { get; }
    public string ManifestFingerprint { get; }

    /// <summary>
    /// 构造从独立清单解析出的只读内容包定义，供目录和选择界面共享。
    /// </summary>
    public ContentPackDefinition(
        int schemaVersion,
        string contentVersion,
        string hostApi,
        ContentPackKind kind,
        string id,
        int number,
        string displayName,
        string englishName,
        ContentPackStatus status,
        bool selectable,
        IReadOnlyList<string> requiredDependencies,
        IReadOnlyList<string> optionalDependencies,
        IReadOnlySet<string> capabilities,
        IReadOnlyList<ContentAddition> additions,
        string manifestPath,
        string manifestFingerprint)
    {
        SchemaVersion = schemaVersion;
        ContentVersion = contentVersion;
        HostApi = hostApi;
        Kind = kind;
        Id = id;
        Number = number;
        DisplayName = displayName;
        EnglishName = englishName;
        Status = status;
        Selectable = selectable;
        RequiredDependencies = Array.AsReadOnly(requiredDependencies.ToArray());
        OptionalDependencies = Array.AsReadOnly(optionalDependencies.ToArray());
        Capabilities = capabilities.ToFrozenSet(StringComparer.Ordinal);
        Additions = Array.AsReadOnly(additions.ToArray());
        ManifestPath = manifestPath;
        ManifestFingerprint = manifestFingerprint;
    }

    /// <summary>判断清单是否明确声明宿主能力，不根据作品编号或名称进行推断。</summary>
    public bool HasCapability(string capabilityId) => Capabilities.Contains(capabilityId);
}
