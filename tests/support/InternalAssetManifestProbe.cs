using Godot;
using System.Text.Json;

namespace TouhouWuxiaSurvivor.Tests.Support;

/// <summary>
/// 为集成测试统一读取中央与逐作品内部素材清单，避免覆盖数和来源数随 DLC 扩展后继续硬编码。
/// </summary>
public static class InternalAssetManifestProbe
{
    private const string MappingPath = "res://assets/internal_original/preview_mappings.json";
    private const string MappingRoot = "res://assets/internal_original/mappings";
    private const string BuildPath = "res://tools/internal_assets/build_manifest.json";
    private const string BuildRoot = "res://tools/internal_assets/packs";

    /// <summary>
    /// 读取所有映射条目的稳定字段，中央清单与每作清单使用同一返回结构供覆盖和纹理测试共享。
    /// </summary>
    public static IReadOnlyList<(
        string SourceId, string Category, string Name, string Asset, string Kind)> ReadMappings()
    {
        var result = new List<(string, string, string, string, string)>();
        foreach (string path in EnumerateJsonPaths(MappingPath, MappingRoot))
        {
            using JsonDocument document = JsonDocument.Parse(Godot.FileAccess.GetFileAsString(path));
            foreach (JsonElement entry in document.RootElement.GetProperty("entries").EnumerateArray())
            {
                result.Add((
                    Required(entry, "sourceId"),
                    Required(entry, "category"),
                    Required(entry, "name"),
                    Required(entry, "asset"),
                    Required(entry, "kind")));
            }
        }

        return result;
    }

    /// <summary>
    /// 递归收集全部构建定义中的 source 与 mask 路径，包含合成立绘图层和二进制音频输入。
    /// </summary>
    public static IReadOnlySet<string> ReadBuildSources()
    {
        var result = new HashSet<string>(StringComparer.Ordinal);
        foreach (string path in EnumerateJsonPaths(BuildPath, BuildRoot))
        {
            using JsonDocument document = JsonDocument.Parse(Godot.FileAccess.GetFileAsString(path));
            CollectSources(document.RootElement, result);
        }

        return result;
    }

    /// <summary>
    /// 读取素材缺失条目的复合键，并强制每条提供中文原因，避免无视觉内容被静默包装成完成状态。
    /// </summary>
    public static IReadOnlySet<string> ReadUnavailableIdentities()
    {
        var result = new HashSet<string>(StringComparer.Ordinal);
        foreach (string path in EnumerateJsonPaths(BuildPath, BuildRoot))
        {
            using JsonDocument document = JsonDocument.Parse(Godot.FileAccess.GetFileAsString(path));
            if (!document.RootElement.TryGetProperty(
                    "unavailableEntries", out JsonElement unavailable))
            {
                continue;
            }

            foreach (JsonElement entry in unavailable.EnumerateArray())
            {
                string sourceId = Required(entry, "sourceId");
                string category = Required(entry, "category");
                string name = Required(entry, "name");
                Required(entry, "reasonZh");
                RequireUnique(result, $"{sourceId}\u001f{category}\u001f{name}", path);
            }
        }

        return result;
    }

    /// <summary>
    /// 先返回必需中央文件，再以稳定文件名顺序枚举资源目录中的 JSON；Godot DirAccess 可兼容 PCK。
    /// </summary>
    private static IEnumerable<string> EnumerateJsonPaths(string requiredPath, string directoryPath)
    {
        if (!Godot.FileAccess.FileExists(requiredPath))
        {
            throw new FileNotFoundException($"Required internal manifest is missing: {requiredPath}.");
        }

        yield return requiredPath;
        using DirAccess? directory = DirAccess.Open(directoryPath);
        if (directory is null)
        {
            yield break;
        }

        foreach (string name in directory.GetFiles()
                     .Where(value => value.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                     .Order(StringComparer.Ordinal))
        {
            yield return $"{directoryPath}/{name}";
        }
    }

    /// <summary>
    /// 深度遍历对象和数组，仅把名为 source 或 mask 的字符串视为外部输入，避免误收 sourceId。
    /// </summary>
    private static void CollectSources(JsonElement element, HashSet<string> result)
    {
        if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (JsonElement child in element.EnumerateArray()) CollectSources(child, result);
            return;
        }

        if (element.ValueKind != JsonValueKind.Object)
        {
            return;
        }

        foreach (JsonProperty property in element.EnumerateObject())
        {
            if ((property.Name is "source" or "mask") &&
                property.Value.ValueKind == JsonValueKind.String)
            {
                result.Add(property.Value.GetString()!);
            }
            else
            {
                CollectSources(property.Value, result);
            }
        }
    }

    /// <summary>
    /// 读取映射必需字段并拒绝空字符串，使测试错误直接指出损坏属性而非稍后纹理加载失败。
    /// </summary>
    private static string Required(JsonElement element, string property)
    {
        string? value = element.GetProperty(property).GetString();
        return !string.IsNullOrWhiteSpace(value)
            ? value
            : throw new InvalidDataException($"Internal mapping property is empty: {property}.");
    }

    /// <summary>
    /// 把缺失声明加入集合并拒绝跨清单重复，使一个图鉴条目只能拥有一个明确的降级原因。
    /// </summary>
    private static void RequireUnique(HashSet<string> values, string identity, string path)
    {
        if (!values.Add(identity))
        {
            throw new InvalidDataException(
                $"Unavailable internal entry is declared more than once: {identity} in {path}.");
        }
    }
}
