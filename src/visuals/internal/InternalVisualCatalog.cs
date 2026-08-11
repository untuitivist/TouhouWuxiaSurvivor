using Godot;
using System.Text.Json;

namespace TouhouWuxiaSurvivor.Visuals.Internal;

/// <summary>
/// 统一加载内部视觉清单与纹理缓存，供图鉴和正式玩法按相同稳定键查询资源。
/// </summary>
public sealed class InternalVisualCatalog
{
    private const string ManifestPath = "res://assets/internal_original/preview_mappings.json";
    private const string PackManifestRoot = "res://assets/internal_original/mappings";
    private const string AssetRoot = "res://assets/internal_original/";
    private readonly Dictionary<string, InternalVisualDefinition> _definitions;
    private readonly Dictionary<string, Texture2D?> _textures = new(StringComparer.Ordinal);

    public int Count => _definitions.Count;

    /// <summary>
    /// 统计指定内容分类的映射数量，使图鉴适配器不会把仅供正式战斗使用的道具映射计为图鉴条目。
    /// </summary>
    public int CountCategories(params InternalVisualCategory[] categories)
    {
        var accepted = new HashSet<InternalVisualCategory>(categories);
        return _definitions.Values.Count(definition => accepted.Contains(definition.Category));
    }

    /// <summary>
    /// 内部清单存在时严格解析；公开包排除清单时建立空目录，让正式玩法安全回退文字。
    /// </summary>
    public InternalVisualCatalog()
    {
        _definitions = LoadDefinitions();
    }

    /// <summary>
    /// 按内容包、分类和中文名查找唯一视觉定义，避免玩法数据持有资源路径。
    /// </summary>
    public bool TryGet(
        string sourceId,
        InternalVisualCategory category,
        string name,
        out InternalVisualDefinition definition) =>
        _definitions.TryGetValue(BuildKey(sourceId, category, name), out definition!);

    /// <summary>
    /// 按路径缓存加载纹理；被导出排除或导入失败时返回 false，让调用方启用替代视觉。
    /// </summary>
    public bool TryGetTexture(InternalVisualDefinition definition, out Texture2D texture)
    {
        if (!_textures.TryGetValue(definition.AssetPath, out Texture2D? cached))
        {
            cached = ResourceLoader.Exists(definition.AssetPath)
                ? ResourceLoader.Load<Texture2D>(definition.AssetPath)
                : null;
            _textures[definition.AssetPath] = cached;
        }

        texture = cached!;
        return cached is not null;
    }

    /// <summary>
    /// 解析全部映射并拒绝空字段、重复键和未知枚举，避免游戏与图鉴对同一条目产生分歧。
    /// </summary>
    private static Dictionary<string, InternalVisualDefinition> LoadDefinitions()
    {
        var result = new Dictionary<string, InternalVisualDefinition>(StringComparer.Ordinal);
        if (Godot.FileAccess.FileExists(ManifestPath))
        {
            AddDefinitions(result, Godot.FileAccess.GetFileAsString(ManifestPath));
        }

        using DirAccess? directory = DirAccess.Open(PackManifestRoot);
        if (directory is not null)
        {
            foreach (string fileName in directory.GetFiles()
                         .Where(name => name.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                         .Order(StringComparer.Ordinal))
            {
                AddDefinitions(result,
                    Godot.FileAccess.GetFileAsString($"{PackManifestRoot}/{fileName}"));
            }
        }

        return result;
    }

    /// <summary>
    /// 把一份映射文档严格加入共享字典；空字段、未知枚举或跨文件重复键都会立即终止加载。
    /// </summary>
    private static void AddDefinitions(
        Dictionary<string, InternalVisualDefinition> result,
        string json)
    {
        using JsonDocument document = JsonDocument.Parse(json);
        foreach (JsonElement item in document.RootElement.GetProperty("entries").EnumerateArray())
        {
            string sourceId = RequiredString(item, "sourceId");
            string name = RequiredString(item, "name");
            InternalVisualCategory category = Enum.Parse<InternalVisualCategory>(
                RequiredString(item, "category"), false);
            InternalVisualKind kind = Enum.Parse<InternalVisualKind>(
                RequiredString(item, "kind"), false);
            int variant = item.TryGetProperty("variant", out JsonElement value) ? value.GetInt32() : 0;
            string? proxySourceWork = item.TryGetProperty("proxySourceWork", out JsonElement proxy)
                ? RequiredString(item, "proxySourceWork")
                : null;
            var definition = new InternalVisualDefinition(
                sourceId, category, name, AssetRoot + RequiredString(item, "asset"), kind,
                variant, proxySourceWork);
            result.Add(BuildKey(sourceId, category, name), definition);
        }
    }

    /// <summary>
    /// 读取必需字符串并拒绝空白值，使内部资源问题在测试中立即暴露。
    /// </summary>
    private static string RequiredString(JsonElement item, string property)
    {
        string? value = item.GetProperty(property).GetString();
        return !string.IsNullOrWhiteSpace(value)
            ? value
            : throw new InvalidDataException($"Internal visual property '{property}' is empty.");
    }

    /// <summary>
    /// 使用记录分隔符建立稳定复合键，避免普通标点出现在中文名时产生碰撞。
    /// </summary>
    private static string BuildKey(
        string sourceId,
        InternalVisualCategory category,
        string name) => $"{sourceId}\u001f{category}\u001f{name}";
}
