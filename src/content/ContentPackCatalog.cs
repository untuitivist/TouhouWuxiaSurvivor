using Godot;

namespace TouhouWuxiaSurvivor.Content;

/// <summary>
/// 一次性加载并验证本体与可选内容包，为菜单和本局冻结提供同一权威目录。
/// </summary>
public static class ContentPackCatalog
{
    private const string PackRoot = "res://content/packs";
    private static readonly object Sync = new();
    private static ContentPackDefinition? _base;
    private static IReadOnlyList<ContentPackDefinition>? _all;
    private static IReadOnlyList<ContentPackDefinition>? _installed;

    public static ContentPackDefinition Base { get { EnsureLoaded(); return _base!; } }
    public static IReadOnlyList<ContentPackDefinition> All
        { get { EnsureLoaded(); return _all!; } }
    public static IReadOnlyList<ContentPackDefinition> Installed
        { get { EnsureLoaded(); return _installed!; } }

    /// <summary>按稳定包 ID 取得已安装定义；未知身份立即失败而不是从本体回退。</summary>
    public static ContentPackDefinition GetRequired(string packId)
    {
        ContentPackDefinition? definition = Installed.FirstOrDefault(
            pack => pack.Id == packId);
        return definition ?? throw new InvalidOperationException(
            $"Unknown content pack id: {packId}");
    }

    /// <summary>把菜单选择解析为按目录顺序冻结的本体加可选包集合，并拒绝未知 ID。</summary>
    public static IReadOnlyList<ContentPackDefinition> ResolveActive(
        ContentPackSelection selection)
    {
        ArgumentNullException.ThrowIfNull(selection);
        foreach (string selectedId in selection.EnabledPackIds)
        {
            ContentPackDefinition selected = GetRequired(selectedId);
            if (selected.Kind != ContentPackKind.Optional || !selected.Selectable)
            {
                throw new InvalidOperationException(
                    $"Content pack cannot be selected for a run: {selectedId}");
            }
        }

        ContentPackDefinition[] active =
            [Base, .. All.Where(pack => selection.IsEnabled(pack.Id))];
        return Array.AsReadOnly(active);
    }

    /// <summary>首次访问时在锁内完成完整目录装载，之后只返回不可变快照。</summary>
    private static void EnsureLoaded()
    {
        if (_installed is not null)
        {
            return;
        }

        lock (Sync)
        {
            if (_installed is not null)
            {
                return;
            }

            ContentPackDefinition foundation = ContentPackManifestLoader.Load(
                "res://content/base/pack.json");
            IReadOnlyList<ContentPackDefinition> optionals = LoadOptionalPacks();
            ValidateCatalog(foundation, optionals);
            _base = foundation;
            _all = optionals;
            ContentPackDefinition[] installed = [foundation, .. optionals];
            _installed = Array.AsReadOnly(installed);
        }
    }

    /// <summary>
    /// 枚举可选包目录并按整数作品编号稳定排序。
    /// </summary>
    private static IReadOnlyList<ContentPackDefinition> LoadOptionalPacks()
    {
        var definitions = new List<ContentPackDefinition>();
        using DirAccess directory = DirAccess.Open(PackRoot) ?? throw new InvalidOperationException(
            $"Content pack root does not exist: {PackRoot}");
        directory.ListDirBegin();
        string entry = directory.GetNext();
        while (!string.IsNullOrEmpty(entry))
        {
            string manifestPath = $"{PackRoot}/{entry}/pack.json";
            if (directory.CurrentIsDir() && Godot.FileAccess.FileExists(manifestPath))
            {
                definitions.Add(ContentPackManifestLoader.Load(manifestPath));
            }

            entry = directory.GetNext();
        }

        directory.ListDirEnd();
        definitions.Sort((left, right) =>
        {
            int numberOrder = left.Number.CompareTo(right.Number);
            return numberOrder != 0
                ? numberOrder
                : string.Compare(left.Id, right.Id, StringComparison.Ordinal);
        });
        return definitions.AsReadOnly();
    }

    /// <summary>验证唯一身份、宿主边界和横向依赖，防止内容包暗中替换核心规则。</summary>
    private static void ValidateCatalog(
        ContentPackDefinition foundation,
        IReadOnlyList<ContentPackDefinition> optionals)
    {
        if (foundation.Id != "base" || foundation.Kind != ContentPackKind.Foundation ||
            foundation.Number != 0 || foundation.RequiredDependencies.Count != 0 ||
            foundation.OptionalDependencies.Count != 0)
        {
            throw new InvalidOperationException(
                "Base manifest must be the dependency-free foundation pack.");
        }

        var ids = new HashSet<string>(StringComparer.Ordinal) { foundation.Id };
        var numbers = new HashSet<int>();
        foreach (ContentPackDefinition optional in optionals)
        {
            if (!ids.Add(optional.Id) || optional.Kind != ContentPackKind.Optional ||
                optional.Number <= 0 || !numbers.Add(optional.Number) ||
                optional.RequiredDependencies.Count != 1 ||
                optional.RequiredDependencies[0] != foundation.Id)
            {
                throw new InvalidOperationException(
                    $"Optional content identity or Base dependency is invalid: {optional.Id}");
            }
        }

        foreach (ContentPackDefinition optional in optionals)
        {
            if (optional.OptionalDependencies.Any(dependency =>
                    dependency == optional.Id || !ids.Contains(dependency)))
            {
                throw new InvalidOperationException(
                    $"Optional dependency is unknown or recursive: {optional.Id}");
            }
        }
    }
}
