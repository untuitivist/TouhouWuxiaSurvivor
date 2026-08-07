namespace TouhouWuxiaSurvivor.Content;

/// <summary>
/// 保存一个正作内容包的身份、开发状态、可选状态和分类增量内容清单。
/// </summary>
public sealed class ContentPackDefinition
{
    public string Id { get; }
    public int Number { get; }
    public string DisplayName { get; }
    public string EnglishName { get; }
    public string Status { get; }
    public bool Selectable { get; }
    public IReadOnlyList<ContentAddition> Additions { get; }

    /// <summary>
    /// 构造从独立清单解析出的只读内容包定义，供目录和选择界面共享。
    /// </summary>
    public ContentPackDefinition(
        string id,
        int number,
        string displayName,
        string englishName,
        string status,
        bool selectable,
        IReadOnlyList<ContentAddition> additions)
    {
        Id = id;
        Number = number;
        DisplayName = displayName;
        EnglishName = englishName;
        Status = status;
        Selectable = selectable;
        Additions = additions;
    }
}
