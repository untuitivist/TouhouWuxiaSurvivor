namespace TouhouWuxiaSurvivor.Versioning.Changelog;

/// <summary>
/// 表示一个版本中的具名改动分节，并保持 Markdown 中的项目顺序供界面稳定呈现。
/// </summary>
public sealed record GameChangelogSection(
    string Heading,
    IReadOnlyList<string> Items);
