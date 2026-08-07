namespace TouhouWuxiaSurvivor.Content;

/// <summary>
/// 表示内容包新增的一项具名内容，以及它在选择列表中所属的中文分类。
/// </summary>
public sealed class ContentAddition
{
    public string Category { get; }
    public string Name { get; }

    /// <summary>
    /// 构造可供界面分组显示的增量内容条目。
    /// </summary>
    public ContentAddition(string category, string name)
    {
        Category = category;
        Name = name;
    }
}
