namespace TouhouWuxiaSurvivor.Ui.Compendium;

/// <summary>
/// 保存一个图鉴条目到内部素材及动画参数的不可变映射。
/// </summary>
public sealed class InternalPreviewDefinition
{
    public string SourceId { get; }
    public CompendiumCategory Category { get; }
    public string Name { get; }
    public string AssetPath { get; }
    public InternalPreviewKind Kind { get; }
    public int Variant { get; }
    public string? ProxySourceWork { get; }
    public string? ReasonZh { get; }
    public string? ReviewStatus { get; }

    /// <summary>
    /// 构造经过清单解析的映射值，资源路径由目录统一补全为内部隔离路径。
    /// </summary>
    public InternalPreviewDefinition(
        string sourceId,
        CompendiumCategory category,
        string name,
        string assetPath,
        InternalPreviewKind kind,
        int variant,
        string? proxySourceWork = null,
        string? reasonZh = null,
        string? reviewStatus = null)
    {
        SourceId = sourceId;
        Category = category;
        Name = name;
        AssetPath = assetPath;
        Kind = kind;
        Variant = variant;
        ProxySourceWork = proxySourceWork;
        ReasonZh = reasonZh;
        ReviewStatus = reviewStatus;
    }
}
