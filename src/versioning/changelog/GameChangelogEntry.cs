namespace TouhouWuxiaSurvivor.Versioning.Changelog;

/// <summary>
/// 表示一个阶段优先版本及其全部分节，供版本索引和正文视图共享同一不可变数据。
/// </summary>
public sealed record GameChangelogEntry(
    string Version,
    IReadOnlyList<GameChangelogSection> Sections)
{
    /// <summary>统计该版本的实际改动项目数，用于界面给出紧凑信息密度提示。</summary>
    public int ItemCount => Sections.Sum(section => section.Items.Count);
}
