using Godot;
using GodotArray = Godot.Collections.Array;
using GodotDictionary = Godot.Collections.Dictionary;

namespace TouhouWuxiaSurvivor.Content;

/// <summary>
/// 从每个内容包自己的 JSON 清单解析目录信息，使新增正作无需修改选择界面布局。
/// </summary>
public static class ContentPackManifestLoader
{
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
    /// 读取单个清单文件并把基础字段和所有分类数组转换为强类型只读定义。
    /// </summary>
    public static ContentPackDefinition Load(string manifestPath)
    {
        Variant parsed = Json.ParseString(Godot.FileAccess.GetFileAsString(manifestPath));
        GodotDictionary manifest = parsed.AsGodotDictionary();
        var additions = new List<ContentAddition>();
        if (manifest.TryGetValue("additions", out Variant additionValue))
        {
            ReadAdditions(additionValue.AsGodotDictionary(), additions);
        }

        return new ContentPackDefinition(
            ReadString(manifest, "id"),
            ReadInt(manifest, "number"),
            ReadString(manifest, "title_zh"),
            ReadString(manifest, "title_en"),
            ReadString(manifest, "status"),
            ReadBool(manifest, "selectable"),
            additions);
    }

    /// <summary>
    /// 遍历清单中的已知分类数组，把每个具名条目转换为带中文分类的增量内容。
    /// </summary>
    private static void ReadAdditions(GodotDictionary source, List<ContentAddition> destination)
    {
        foreach ((string key, string categoryName) in CategoryNames)
        {
            if (!source.TryGetValue(key, out Variant valuesVariant))
            {
                continue;
            }

            GodotArray values = valuesVariant.AsGodotArray();
            foreach (Variant value in values)
            {
                destination.Add(new ContentAddition(categoryName, value.AsString()));
            }
        }
    }

    /// <summary>
    /// 从清单读取字符串字段；缺失字段回退为空字符串以允许目录继续显示损坏项。
    /// </summary>
    private static string ReadString(GodotDictionary source, string key) =>
        source.TryGetValue(key, out Variant value) ? value.AsString() : string.Empty;

    /// <summary>
    /// 从清单读取整数编号；本体清单没有编号时使用零。
    /// </summary>
    private static int ReadInt(GodotDictionary source, string key) =>
        source.TryGetValue(key, out Variant value) ? value.AsInt32() : 0;

    /// <summary>
    /// 从清单读取布尔开关；缺失时默认不可选择，避免未完成内容意外进入游戏。
    /// </summary>
    private static bool ReadBool(GodotDictionary source, string key) =>
        source.TryGetValue(key, out Variant value) && value.AsBool();
}
