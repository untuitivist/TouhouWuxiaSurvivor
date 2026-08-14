using Godot;

namespace TouhouWuxiaSurvivor.Versioning.Changelog;

/// <summary>
/// 从仓库唯一的 CHANGELOG.md 解析版本、分节与项目，拒绝重复版本和不完整条目。
/// </summary>
public sealed class GameChangelogCatalog
{
    /// <summary>指向编辑器和内嵌 PCK 共用的唯一版本日志资源。</summary>
    public const string SourcePath = "res://CHANGELOG.md";

    /// <summary>按 Markdown 文件从新到旧的顺序暴露全部版本。</summary>
    public IReadOnlyList<GameChangelogEntry> Entries { get; }

    /// <summary>仅由通过严格校验的条目数组构建目录，避免调用方修改解析结果。</summary>
    private GameChangelogCatalog(IReadOnlyList<GameChangelogEntry> entries) => Entries = entries;

    /// <summary>
    /// 从项目资源读取日志，并确认第一条记录与项目当前版本完全一致。
    /// </summary>
    public static GameChangelogCatalog LoadDefault()
    {
        if (!Godot.FileAccess.FileExists(SourcePath))
        {
            throw new FileNotFoundException("The packaged changelog resource is missing.", SourcePath);
        }

        GameChangelogCatalog catalog = Parse(Godot.FileAccess.GetFileAsString(SourcePath));
        if (catalog.Entries[0].Version != GameVersion.Current)
        {
            throw new InvalidDataException(
                $"Latest changelog version {catalog.Entries[0].Version} does not match {GameVersion.Current}.");
        }

        return catalog;
    }

    /// <summary>
    /// 解析受仓库约束的二级版本标题、三级分节标题和无序项目，不猜测其他 Markdown 语法。
    /// </summary>
    public static GameChangelogCatalog Parse(string markdown)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(markdown);
        var entries = new List<GameChangelogEntry>();
        var sections = new List<GameChangelogSection>();
        var items = new List<string>();
        string? version = null;
        string? heading = null;

        foreach (string rawLine in markdown.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n'))
        {
            string line = rawLine.Trim();
            if (line.StartsWith("## ", StringComparison.Ordinal))
            {
                CommitSection(sections, items, ref heading);
                CommitEntry(entries, sections, ref version);
                version = line[3..].Trim();
                if (!GameVersion.IsValidFormat(version))
                {
                    throw new InvalidDataException($"Invalid changelog version heading: {version}.");
                }
            }
            else if (version is not null && line.StartsWith("### ", StringComparison.Ordinal))
            {
                CommitSection(sections, items, ref heading);
                heading = line[4..].Trim();
            }
            else if (version is not null && line.StartsWith("- ", StringComparison.Ordinal))
            {
                if (heading is null)
                {
                    throw new InvalidDataException($"Changelog item in {version} has no section heading.");
                }

                items.Add(line[2..].Trim());
            }
        }

        CommitSection(sections, items, ref heading);
        CommitEntry(entries, sections, ref version);
        if (entries.Count == 0 || entries.Select(entry => entry.Version).Distinct().Count() != entries.Count)
        {
            throw new InvalidDataException("Changelog must contain at least one uniquely named version.");
        }

        return new GameChangelogCatalog(entries.ToArray());
    }

    /// <summary>提交当前非空分节，并清空项目缓冲供下一个分节复用。</summary>
    private static void CommitSection(
        ICollection<GameChangelogSection> sections,
        List<string> items,
        ref string? heading)
    {
        if (heading is null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(heading) || items.Count == 0)
        {
            throw new InvalidDataException("Every changelog section requires a heading and at least one item.");
        }

        sections.Add(new GameChangelogSection(heading, items.ToArray()));
        items.Clear();
        heading = null;
    }

    /// <summary>提交当前非空版本，并清空分节缓冲供下一个版本复用。</summary>
    private static void CommitEntry(
        ICollection<GameChangelogEntry> entries,
        List<GameChangelogSection> sections,
        ref string? version)
    {
        if (version is null)
        {
            return;
        }

        if (sections.Count == 0)
        {
            throw new InvalidDataException($"Changelog version {version} has no sections.");
        }

        entries.Add(new GameChangelogEntry(version, sections.ToArray()));
        sections.Clear();
        version = null;
    }
}
