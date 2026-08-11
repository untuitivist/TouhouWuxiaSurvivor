namespace TouhouWuxiaSurvivor.Visuals.Internal;

/// <summary>
/// 保存内容条目到内部素材路径、版式与动画变体的只读映射。
/// </summary>
public sealed class InternalVisualDefinition
{
    public string SourceId { get; }
    public InternalVisualCategory Category { get; }
    public string Name { get; }
    public string AssetPath { get; }
    public InternalVisualKind Kind { get; }
    public int Variant { get; }
    public string? ProxySourceWork { get; }

    /// <summary>
    /// 构造一条已由清单解析和校验的共享视觉定义。
    /// </summary>
    public InternalVisualDefinition(
        string sourceId,
        InternalVisualCategory category,
        string name,
        string assetPath,
        InternalVisualKind kind,
        int variant,
        string? proxySourceWork = null)
    {
        SourceId = sourceId;
        Category = category;
        Name = name;
        AssetPath = assetPath;
        Kind = kind;
        Variant = variant;
        ProxySourceWork = proxySourceWork;
    }
}
