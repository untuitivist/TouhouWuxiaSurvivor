using System.Text.Json;
using Godot;
using static TouhouWuxiaSurvivor.Tools.InternalAssets.InternalImageTransformer;

namespace TouhouWuxiaSurvivor.Tools.InternalAssets;

/// <summary>
/// 保存一次原作素材构建的路径与来源集合，并集中执行图像加载、掩码合并、格式解析和 PNG 写入。
/// </summary>
internal sealed class InternalAssetBuildContext
{
    /// <summary>获取用户提供且已验证存在的外部素材根目录。</summary>
    internal string SourceRoot { get; }

    /// <summary>获取所有内部规范化资源共用的 Godot 输出根路径。</summary>
    internal string OutputRoot { get; }

    /// <summary>获取本次实际读取的相对来源集合，供二进制复制和最终哈希审计共享。</summary>
    internal ISet<string> UsedSources { get; }

    /// <summary>
    /// 绑定一次构建所需的输入、输出和审计集合，使各专用构建器无需重复持有路径规则。
    /// </summary>
    internal InternalAssetBuildContext(
        string sourceRoot,
        string outputRoot,
        ISet<string> usedSources)
    {
        SourceRoot = sourceRoot;
        OutputRoot = outputRoot;
        UsedSources = usedSources;
    }

    /// <summary>
    /// 加载颜色图并按可选 `mask` 的红通道覆盖 Alpha，同时登记两者供最终 SHA-256 清单审计。
    /// </summary>
    internal Image LoadImage(JsonElement definition)
    {
        string relative = definition.GetProperty("source").GetString()!;
        UsedSources.Add(relative);
        Image image = Image.LoadFromFile(ResolveSource(relative));
        image.Convert(Image.Format.Rgba8);
        if (!definition.TryGetProperty("mask", out JsonElement maskElement))
        {
            return image;
        }

        string maskRelative = maskElement.GetString()!;
        UsedSources.Add(maskRelative);
        Image mask = Image.LoadFromFile(ResolveSource(maskRelative));
        return MergeAlpha(image, mask);
    }

    /// <summary>
    /// 从清单输出字段取得相对路径并保存 PNG，使全部构建类型遵守相同的目录与错误处理规则。
    /// </summary>
    internal void Save(Image image, JsonElement output)
    {
        Save(image, output.GetString()!);
    }

    /// <summary>
    /// 确保内部输出目录存在并保存 PNG；非 OK 结果会转成异常，阻止工具产生假成功状态。
    /// </summary>
    internal void Save(Image image, string relativeOutput)
    {
        string path = ProjectSettings.GlobalizePath(OutputRoot + relativeOutput);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        Error error = image.SavePng(path);
        if (error != Error.Ok)
        {
            throw new IOException(
                $"Could not save internal preview {path}: {error}.");
        }
    }

    /// <summary>
    /// 把清单中的四整数数组转换为 Godot 裁切矩形，供场景、帧与立绘共享严格坐标格式。
    /// </summary>
    internal static Rect2I ReadRect(JsonElement element) => new(
        element[0].GetInt32(), element[1].GetInt32(),
        element[2].GetInt32(), element[3].GetInt32());

    /// <summary>
    /// 把清单中的两个整数转换为尺寸或目标坐标，供画布大小和图层位置使用相同格式。
    /// </summary>
    internal static Vector2I ReadPoint(JsonElement element) => new(
        element[0].GetInt32(), element[1].GetInt32());

    /// <summary>
    /// 把清单中的四个 0-255 通道转换为 Godot 颜色，保留场景定义声明的透明度。
    /// </summary>
    internal static Color ReadColor(JsonElement element) => Color.Color8(
        (byte)element[0].GetInt32(), (byte)element[1].GetInt32(),
        (byte)element[2].GetInt32(), (byte)element[3].GetInt32());

    /// <summary>
    /// 将统一使用正斜杠的清单相对路径解析到本次构建的外部素材根目录。
    /// </summary>
    private string ResolveSource(string relative)
    {
        return Path.Combine(
            SourceRoot, relative.Replace('/', Path.DirectorySeparatorChar));
    }
}
