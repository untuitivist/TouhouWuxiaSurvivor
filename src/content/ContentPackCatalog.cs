using Godot;

namespace TouhouWuxiaSurvivor.Content;

/// <summary>
/// 扫描全部整数编号正作目录并提供稳定排序的本体定义和可选内容包目录。
/// </summary>
public static class ContentPackCatalog
{
    private const string PackRoot = "res://content/packs";
    private static IReadOnlyList<ContentPackDefinition>? _all;
    private static ContentPackDefinition? _base;

    public static ContentPackDefinition Base => _base ??=
        ContentPackManifestLoader.Load("res://content/base/pack.json");
    public static IReadOnlyList<ContentPackDefinition> All => _all ??= LoadAll();

    /// <summary>
    /// 枚举正作子目录、读取存在的 pack.json，并按整数作品编号升序返回。
    /// </summary>
    private static IReadOnlyList<ContentPackDefinition> LoadAll()
    {
        var definitions = new List<ContentPackDefinition>();
        using DirAccess directory = DirAccess.Open(PackRoot);
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
        definitions.Sort((left, right) => left.Number.CompareTo(right.Number));
        return definitions;
    }
}
