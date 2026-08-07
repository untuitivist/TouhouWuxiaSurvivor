using TouhouWuxiaSurvivor.Visuals.Internal;

namespace TouhouWuxiaSurvivor.Ui.Compendium;

/// <summary>
/// 将共享内部视觉目录适配为图鉴原有定义，保持图鉴与正式玩法使用同一份映射。
/// </summary>
public sealed class InternalPreviewCatalog
{
    private readonly InternalVisualCatalog _catalog = new();

    public int Count => _catalog.Count;

    /// <summary>
    /// 构造共享目录适配器；共享目录负责处理内部清单缺失时的空目录回退。
    /// </summary>
    public InternalPreviewCatalog()
    {
    }

    /// <summary>
    /// 按图鉴条目的稳定复合键查找素材定义，避免内容模型持有 UI 资源路径。
    /// </summary>
    public bool TryGet(CompendiumEntry entry, out InternalPreviewDefinition definition)
    {
        InternalVisualCategory category = Enum.Parse<InternalVisualCategory>(
            entry.Category.ToString(), false);
        if (!_catalog.TryGet(entry.SourceId, category, entry.Name, out var shared))
        {
            definition = null!;
            return false;
        }

        definition = new InternalPreviewDefinition(
            shared.SourceId,
            entry.Category,
            shared.Name,
            shared.AssetPath,
            Enum.Parse<InternalPreviewKind>(shared.Kind.ToString(), false),
            shared.Variant);
        return true;
    }

    /// <summary>
    /// 检查指定条目是否被内部映射完整覆盖，供集成测试验证本体与红魔乡清单。
    /// </summary>
    public bool Contains(CompendiumEntry entry) => TryGet(entry, out _);

    /// <summary>
}
