using System.Text.Json;
using Godot;

namespace TouhouWuxiaSurvivor.Content;

/// <summary>
/// 严格解析内容包身份头和目录摘要；无效包在进入菜单或本局前立即失败。
/// </summary>
public static class ContentPackManifestLoader
{
    public const int SupportedSchemaVersion = 1;
    public const string SupportedHostApi = "content-v1";
    private static readonly IReadOnlyDictionary<string, string> CategoryNames =
        new System.Collections.Generic.Dictionary<string, string>
        {
            ["biomes"] = "地区",
            ["structures"] = "结构",
            ["enemies"] = "敌人",
            ["characters"] = "角色",
            ["items"] = "道具",
            ["systems"] = "系统",
        };

    /// <summary>
    /// 读取并验证单个 JSON 清单，保留原文指纹供本局冻结与问题复现。
    /// </summary>
    public static ContentPackDefinition Load(string manifestPath)
    {
        if (!Godot.FileAccess.FileExists(manifestPath))
        {
            throw new InvalidOperationException($"Content manifest does not exist: {manifestPath}");
        }

        string json = Godot.FileAccess.GetFileAsString(manifestPath);
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new InvalidOperationException($"Content manifest is empty: {manifestPath}");
        }

        try
        {
            using JsonDocument document = JsonDocument.Parse(json);
            JsonElement root = document.RootElement;
            RequireKind(root, JsonValueKind.Object, manifestPath, "root");
            int schemaVersion = ReadRequiredInt(root, "schema_version", manifestPath);
            if (schemaVersion != SupportedSchemaVersion)
            {
                throw Invalid(manifestPath,
                    $"unsupported schema_version {schemaVersion}");
            }

            string hostApi = ReadRequiredString(root, "host_api", manifestPath);
            if (hostApi != SupportedHostApi)
            {
                throw Invalid(manifestPath, $"unsupported host_api '{hostApi}'");
            }

            string contentVersion = ReadRequiredString(root, "content_version", manifestPath);
            ValidateVersion(contentVersion, manifestPath);
            string id = ReadRequiredString(root, "id", manifestPath);
            ValidateId(id, manifestPath);
            JsonElement dependencies = ReadRequiredObject(root, "dependencies", manifestPath);
            var additions = new List<ContentAddition>();
            ReadAdditions(root, additions, manifestPath);
            ReadSpellCards(root, additions, manifestPath);
            return new ContentPackDefinition(
                schemaVersion, contentVersion, hostApi,
                ParseKind(ReadRequiredString(root, "kind", manifestPath), manifestPath),
                id, ReadOptionalInt(root, "number"),
                ReadRequiredString(root, "title_zh", manifestPath),
                ReadOptionalString(root, "title_en"),
                ParseStatus(ReadRequiredString(root, "status", manifestPath), manifestPath),
                ReadRequiredBool(root, "selectable", manifestPath),
                ReadStringArray(dependencies, "required", manifestPath),
                ReadStringArray(dependencies, "optional", manifestPath),
                new HashSet<string>(ReadStringArray(root, "capabilities", manifestPath),
                    StringComparer.Ordinal),
                additions, manifestPath, ContentFingerprint.HashText(json));
        }
        catch (JsonException exception)
        {
            throw new InvalidOperationException(
                $"Invalid content manifest JSON: {manifestPath}", exception);
        }
    }

    /// <summary>
    /// 遍历清单中的已知分类数组，把每个具名条目转换为带中文分类的内容条目。
    /// </summary>
    private static void ReadAdditions(
        JsonElement root,
        List<ContentAddition> destination,
        string path)
    {
        if (!root.TryGetProperty("additions", out JsonElement additions))
        {
            return;
        }

        RequireKind(additions, JsonValueKind.Object, path, "additions");
        foreach ((string key, string categoryName) in CategoryNames)
        {
            if (!additions.TryGetProperty(key, out _))
            {
                continue;
            }

            foreach (string value in ReadStringArray(additions, key, path))
            {
                destination.Add(new ContentAddition(categoryName, value));
            }
        }
    }

    /// <summary>
    /// 把根级结构化符卡数组投影为内容选择详情，界面只读取名称而不复制战斗数值解析逻辑。
    /// </summary>
    private static void ReadSpellCards(
        JsonElement root,
        List<ContentAddition> destination,
        string path)
    {
        if (!root.TryGetProperty("spellcards", out JsonElement cards))
        {
            return;
        }

        RequireKind(cards, JsonValueKind.Array, path, "spellcards");
        foreach (JsonElement card in cards.EnumerateArray())
        {
            RequireKind(card, JsonValueKind.Object, path, "spellcards item");
            destination.Add(new ContentAddition(
                "符卡", ReadRequiredString(card, "name", path)));
        }
    }

    /// <summary>
    /// 读取必填非空字符串，并拒绝静默空身份。
    /// </summary>
    private static string ReadRequiredString(JsonElement source, string key, string path)
    {
        if (!source.TryGetProperty(key, out JsonElement value) ||
            value.ValueKind != JsonValueKind.String ||
            string.IsNullOrWhiteSpace(value.GetString()))
        {
            throw Invalid(path, $"missing or invalid string '{key}'");
        }

        return value.GetString()!;
    }

    /// <summary>读取可选字符串；本体无需英文标题时返回空值。</summary>
    private static string ReadOptionalString(JsonElement source, string key) =>
        source.TryGetProperty(key, out JsonElement value) &&
        value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? string.Empty
            : string.Empty;

    /// <summary>读取必填整数，拒绝浮点或字符串伪装的版本字段。</summary>
    private static int ReadRequiredInt(JsonElement source, string key, string path)
    {
        if (!source.TryGetProperty(key, out JsonElement value) || !value.TryGetInt32(out int result))
        {
            throw Invalid(path, $"missing or invalid integer '{key}'");
        }

        return result;
    }

    /// <summary>读取可选作品编号；本体缺省为零。</summary>
    private static int ReadOptionalInt(JsonElement source, string key) =>
        source.TryGetProperty(key, out JsonElement value) && value.TryGetInt32(out int result)
            ? result
            : 0;

    /// <summary>读取必填布尔字段，防止字符串真假值被容错吞掉。</summary>
    private static bool ReadRequiredBool(JsonElement source, string key, string path)
    {
        if (!source.TryGetProperty(key, out JsonElement value) ||
            value.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
        {
            throw Invalid(path, $"missing or invalid boolean '{key}'");
        }

        return value.GetBoolean();
    }

    /// <summary>读取必填对象字段并统一报告所属清单。</summary>
    private static JsonElement ReadRequiredObject(JsonElement source, string key, string path)
    {
        if (!source.TryGetProperty(key, out JsonElement value))
        {
            throw Invalid(path, $"missing object '{key}'");
        }

        RequireKind(value, JsonValueKind.Object, path, key);
        return value;
    }

    /// <summary>读取去重字符串数组，重复能力或依赖被视为清单错误。</summary>
    private static IReadOnlyList<string> ReadStringArray(
        JsonElement source,
        string key,
        string path)
    {
        if (!source.TryGetProperty(key, out JsonElement values))
        {
            throw Invalid(path, $"missing array '{key}'");
        }

        RequireKind(values, JsonValueKind.Array, path, key);
        var result = new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (JsonElement value in values.EnumerateArray())
        {
            if (value.ValueKind != JsonValueKind.String ||
                string.IsNullOrWhiteSpace(value.GetString()) || !seen.Add(value.GetString()!))
            {
                throw Invalid(path, $"array '{key}' contains an invalid or duplicate value");
            }

            result.Add(value.GetString()!);
        }

        return result;
    }

    /// <summary>
    /// 严格解析内容完成度；未知值直接指出所属清单，避免界面将损坏状态静默显示成未开发。
    /// </summary>
    private static ContentPackStatus ParseStatus(string value, string manifestPath) => value switch
    {
        "inventory" => ContentPackStatus.Inventory,
        "development" => ContentPackStatus.Development,
        "complete" => ContentPackStatus.Complete,
        _ => throw Invalid(manifestPath, $"unsupported status '{value}'"),
    };

    /// <summary>严格解析本体或可选包身份。</summary>
    private static ContentPackKind ParseKind(string value, string path) => value switch
    {
        "foundation" => ContentPackKind.Foundation,
        "optional" => ContentPackKind.Optional,
        _ => throw Invalid(path, $"unsupported kind '{value}'"),
    };

    /// <summary>要求内容版本使用三段数字，避免展示版本与依赖比较语义分叉。</summary>
    private static void ValidateVersion(string value, string path)
    {
        if (value.Split('.').Length != 3 || !Version.TryParse(value, out _))
        {
            throw Invalid(path, $"content_version is not numeric x.y.z: '{value}'");
        }
    }

    /// <summary>要求包标识只含稳定 ASCII 小写字符，防止路径和存档键漂移。</summary>
    private static void ValidateId(string value, string path)
    {
        bool valid = value.All(character =>
            character is >= 'a' and <= 'z' or >= '0' and <= '9' or '_');
        if (!valid)
        {
            throw Invalid(path, $"content id is not stable ASCII: '{value}'");
        }
    }

    /// <summary>验证 JSON 节点类型并生成统一错误。</summary>
    private static void RequireKind(
        JsonElement value,
        JsonValueKind expected,
        string path,
        string field)
    {
        if (value.ValueKind != expected)
        {
            throw Invalid(path, $"'{field}' must be {expected}");
        }
    }

    /// <summary>生成包含来源路径的清单异常。</summary>
    private static InvalidOperationException Invalid(string path, string reason) =>
        new($"Invalid content manifest '{path}': {reason}.");
}
