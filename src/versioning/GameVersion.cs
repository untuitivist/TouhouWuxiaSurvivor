using System.Text.RegularExpressions;
using Godot;

namespace TouhouWuxiaSurvivor.Versioning;

/// <summary>
/// 从 Godot 项目设置读取并校验游戏版本，避免在运行时代码中维护第二份版本常量。
/// </summary>
public static class GameVersion
{
    /// <summary>
    /// 指向项目唯一运行时版本来源的 Godot 设置键，调用方可据此执行一致性审计。
    /// </summary>
    public const string ProjectSettingPath = "application/config/version";

    private static readonly Regex FormatPattern = new(
        @"^(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)-(alpha|beta|rc|stable)$",
        RegexOptions.CultureInvariant | RegexOptions.NonBacktracking);

    /// <summary>
    /// 每次访问都从 ProjectSettings 读取当前版本并立即校验，确保运行时不会使用陈旧缓存。
    /// </summary>
    public static string Current => ReadValidated();

    /// <summary>
    /// 判断候选文本是否满足非负整数三段版本号和四种允许阶段组成的完整格式。
    /// </summary>
    /// <param name="value">需要检查的版本文本；空值、空串和带空白文本均视为无效。</param>
    /// <returns>仅当文本严格满足 <c>x.y.z-stage</c> 规则时返回 <see langword="true"/>。</returns>
    public static bool IsValidFormat(string? value) =>
        !string.IsNullOrEmpty(value) && FormatPattern.IsMatch(value);

    /// <summary>
    /// 从唯一项目设置键读取版本；设置缺失或格式错误时抛出异常阻止错误版本继续运行。
    /// </summary>
    /// <returns>已经通过严格格式校验的项目版本文本。</returns>
    /// <exception cref="InvalidDataException">版本设置缺失，或文本不符合版本规则。</exception>
    private static string ReadValidated()
    {
        if (!ProjectSettings.HasSetting(ProjectSettingPath))
        {
            throw new InvalidDataException(
                $"Missing required project version setting: {ProjectSettingPath}.");
        }

        string value = ProjectSettings.GetSetting(ProjectSettingPath).AsString();
        if (!IsValidFormat(value))
        {
            throw new InvalidDataException(
                $"Invalid project version '{value}'. Expected x.y.z-stage with " +
                "stage alpha, beta, rc, or stable.");
        }

        return value;
    }
}
