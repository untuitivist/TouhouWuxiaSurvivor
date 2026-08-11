using Godot;
using System.Text.Json;
using TouhouWuxiaSurvivor.Ui.Compendium;

namespace TouhouWuxiaSurvivor.Tests.Integration;

/// <summary>
/// 动态审计内部原作素材的跨作代用和暂缺声明，防止新增内容包绕过来源标注。
/// </summary>
public partial class InternalAssetProvenanceTest : Node
{
    private const string MappingPath = "res://assets/internal_original/preview_mappings.json";
    private const string MappingRoot = "res://assets/internal_original/mappings";
    private const string BuildPath = "res://tools/internal_assets/build_manifest.json";
    private const string BuildRoot = "res://tools/internal_assets/packs";
    private static readonly string[] ProxyFields =
        ["asset", "kind", "proxySourceWork", "reasonZh", "reviewStatus"];

    /// <summary>
    /// 读取当前全部清单并验证代用和暂缺契约；失败时用非零退出码阻断持续集成。
    /// </summary>
    public override void _Ready()
    {
        try
        {
            Dictionary<string, IReadOnlyDictionary<string, string>> mappings =
                ReadEntries(MappingPath, MappingRoot, "entries");
            Dictionary<string, IReadOnlyDictionary<string, string>> proxies =
                ReadEntries(BuildPath, BuildRoot, "proxyAssets");
            Dictionary<string, IReadOnlyDictionary<string, string>> unavailable =
                ReadEntries(BuildPath, BuildRoot, "unavailableEntries");
            VerifyProxies(mappings, proxies);
            VerifyUnavailable(mappings, unavailable);
            GD.Print($"Internal asset provenance test passed: " +
                $"{proxies.Count} proxies, {unavailable.Count} unavailable entries.");
            GetTree().Quit();
        }
        catch (Exception exception)
        {
            GD.PushError(exception.ToString());
            GetTree().Quit(1);
        }
    }

    /// <summary>
    /// 验证每条构建代用声明和运行时映射一一对应，且所有审计字段保持完全相同。
    /// </summary>
    private static void VerifyProxies(
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> mappings,
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> proxies)
    {
        foreach ((string identity, IReadOnlyDictionary<string, string> proxy) in proxies)
        {
            foreach (string field in ProxyFields) RequiredField(proxy, field, identity);
            Require(proxy["reviewStatus"] == "proxy-reviewed",
                $"Proxy reviewStatus must be proxy-reviewed: {identity}.");
            Require(!StringComparer.OrdinalIgnoreCase.Equals(
                    proxy["sourceId"], proxy["proxySourceWork"]),
                $"Proxy cannot cite its own work as source: {identity}.");
            Require(mappings.TryGetValue(identity, out var mapping),
                $"Declared proxy has no runtime mapping: {identity}.");
            foreach (string field in ProxyFields)
            {
                string runtimeValue = RequiredField(mapping!, field, identity);
                Require(runtimeValue == proxy[field],
                    $"Proxy field differs between build and runtime manifests: " +
                    $"{identity}/{field}.");
            }
        }

        foreach ((string identity, IReadOnlyDictionary<string, string> mapping) in mappings)
        {
            bool carriesProxyMetadata = ProxyFields.Skip(2)
                .Any(field => mapping.ContainsKey(field));
            Require(!carriesProxyMetadata || proxies.ContainsKey(identity),
                $"Runtime proxy has no proxyAssets declaration: {identity}.");
        }
    }

    /// <summary>
    /// 验证所有暂缺身份真实存在于图鉴，具有原因，并且不会同时得到运行时视觉映射。
    /// </summary>
    private static void VerifyUnavailable(
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> mappings,
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> unavailable)
    {
        HashSet<string> compendium = CompendiumCatalog.All
            .Select(entry => Identity(entry.SourceId, entry.Category.ToString(), entry.Name))
            .ToHashSet(StringComparer.Ordinal);
        foreach ((string identity, IReadOnlyDictionary<string, string> entry) in unavailable)
        {
            RequiredField(entry, "reasonZh", identity);
            Require(compendium.Contains(identity),
                $"Unavailable declaration has no compendium entry: {identity}.");
            Require(!mappings.ContainsKey(identity),
                $"Unavailable declaration also has a runtime mapping: {identity}.");
        }
    }

    /// <summary>
    /// 按稳定文件顺序读取中央及逐作品 JSON 的指定数组，并拒绝跨文件重复身份。
    /// </summary>
    private static Dictionary<string, IReadOnlyDictionary<string, string>> ReadEntries(
        string requiredPath, string directoryPath, string arrayName)
    {
        var result = new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.Ordinal);
        foreach (string path in EnumerateJsonPaths(requiredPath, directoryPath))
        {
            using JsonDocument document = JsonDocument.Parse(Godot.FileAccess.GetFileAsString(path));
            if (!document.RootElement.TryGetProperty(arrayName, out JsonElement entries)) continue;
            Require(entries.ValueKind == JsonValueKind.Array,
                $"Manifest property must be an array: {path}/{arrayName}.");
            foreach (JsonElement element in entries.EnumerateArray())
            {
                Dictionary<string, string> entry = ReadEntry(element);
                string identity = Identity(entry["sourceId"], entry["category"], entry["name"]);
                Require(result.TryAdd(identity, entry),
                    $"Manifest identity is declared more than once: {identity} in {path}.");
            }
        }

        return result;
    }

    /// <summary>
    /// 将单条 JSON 记录转换为不可变用途的字符串字典，并先锁定身份必需字段。
    /// </summary>
    private static Dictionary<string, string> ReadEntry(JsonElement element)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (JsonProperty property in element.EnumerateObject())
        {
            if (property.Value.ValueKind == JsonValueKind.String)
            {
                result[property.Name] = property.Value.GetString()!;
            }
        }

        RequiredField(result, "sourceId", "unknown");
        RequiredField(result, "category", "unknown");
        RequiredField(result, "name", "unknown");
        return result;
    }

    /// <summary>
    /// 先返回必需中央清单，再枚举同目录全部逐作品 JSON，使新增 DLC 自动进入审计。
    /// </summary>
    private static IEnumerable<string> EnumerateJsonPaths(
        string requiredPath, string directoryPath)
    {
        Require(Godot.FileAccess.FileExists(requiredPath),
            $"Required provenance manifest is missing: {requiredPath}.");
        yield return requiredPath;
        using DirAccess? directory = DirAccess.Open(directoryPath);
        if (directory is null) yield break;
        foreach (string name in directory.GetFiles()
                     .Where(value => value.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                     .Order(StringComparer.Ordinal))
        {
            yield return $"{directoryPath}/{name}";
        }
    }

    /// <summary>
    /// 读取非空审计字段；错误同时携带身份和字段名，便于定位损坏条目。
    /// </summary>
    private static string RequiredField(
        IReadOnlyDictionary<string, string> entry, string field, string identity)
    {
        Require(entry.TryGetValue(field, out string? value) && !string.IsNullOrWhiteSpace(value),
            $"Required provenance field is missing: {identity}/{field}.");
        return value!;
    }

    /// <summary>
    /// 使用视觉目录约定的记录分隔符组合稳定身份，避免名称拼接产生歧义。
    /// </summary>
    private static string Identity(string sourceId, string category, string name) =>
        $"{sourceId}\u001f{category}\u001f{name}";

    /// <summary>
    /// 把契约违例统一转换为带上下文的测试异常。
    /// </summary>
    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidDataException(message);
    }
}
