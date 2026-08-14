using System.Globalization;
using System.Text.RegularExpressions;
using Godot;

namespace TouhouWuxiaSurvivor.Versioning;

/// <summary>
/// 从 Godot 项目设置读取、解析并校验阶段优先版本号，避免运行时、文档和导出脚本各自理解格式。
/// </summary>
public static class GameVersion
{
    /// <summary>指向项目唯一运行时版本来源的 Godot 设置键。</summary>
    public const string ProjectSettingPath = "application/config/version";

    private static readonly Regex FormatPattern = new(
        @"^(?<stage>alpha|beta|rc|stable)-(?<major>0|[1-9][0-9]*)\.(?<release>0|[1-9][0-9]*)\.(?<optimization>0|[1-9][0-9]*)$",
        RegexOptions.CultureInvariant | RegexOptions.NonBacktracking);

    /// <summary>每次访问都从项目设置读取当前版本，防止运行时持有已过期的缓存值。</summary>
    public static string Current => CurrentDescriptor.ToString();

    /// <summary>读取并返回已经拆分为阶段与三个数字维度的当前项目版本。</summary>
    public static GameVersionDescriptor CurrentDescriptor => ReadValidated();

    /// <summary>
    /// 判断文本是否严格满足 <c>{stage}-{major}.{release}.{optimization}</c>，包括阶段集合和前导零规则。
    /// </summary>
    /// <param name="value">待检查文本；空值、空串、空白前后缀和旧式版本均无效。</param>
    /// <returns>仅当完整文本可被类型化解析时返回 <see langword="true"/>。</returns>
    public static bool IsValidFormat(string? value) => TryParse(value, out _);

    /// <summary>
    /// 尝试把阶段优先版本拆为强类型字段；任一数字溢出、阶段未知或格式不规范都会原子地失败。
    /// </summary>
    /// <param name="value">需要解析的完整版本文本。</param>
    /// <param name="descriptor">成功时返回规范版本描述；失败时返回空值。</param>
    /// <returns>全部四个部分均合法且可表示时返回 <see langword="true"/>。</returns>
    public static bool TryParse(string? value, out GameVersionDescriptor? descriptor)
    {
        descriptor = null;
        if (string.IsNullOrEmpty(value))
        {
            return false;
        }

        Match match = FormatPattern.Match(value);
        if (!match.Success || !TryParseStage(match.Groups["stage"].Value, out GameReleaseStage stage) ||
            !TryParseNumber(match, "major", out int major) ||
            !TryParseNumber(match, "release", out int release) ||
            !TryParseNumber(match, "optimization", out int optimization))
        {
            return false;
        }

        descriptor = new GameVersionDescriptor(stage, major, release, optimization);
        return true;
    }

    /// <summary>
    /// 解析一个阶段优先版本，并在输入不合法时以明确异常拒绝继续生成版本或产物名称。
    /// </summary>
    /// <param name="value">需要解析的完整版本文本。</param>
    /// <returns>通过全部格式和数值边界检查的版本描述。</returns>
    /// <exception cref="FormatException">输入不符合当前版本规则。</exception>
    public static GameVersionDescriptor Parse(string value)
    {
        if (TryParse(value, out GameVersionDescriptor? descriptor))
        {
            return descriptor!;
        }

        throw new FormatException(
            $"Invalid game version '{value}'. Expected stage-major.release.optimization " +
            "with stage alpha, beta, rc, or stable.");
    }

    /// <summary>从唯一项目设置键读取版本，缺失或错误时阻止游戏继续使用不一致元数据。</summary>
    private static GameVersionDescriptor ReadValidated()
    {
        if (!ProjectSettings.HasSetting(ProjectSettingPath))
        {
            throw new InvalidDataException(
                $"Missing required project version setting: {ProjectSettingPath}.");
        }

        string value = ProjectSettings.GetSetting(ProjectSettingPath).AsString();
        try
        {
            return Parse(value);
        }
        catch (FormatException exception)
        {
            throw new InvalidDataException(exception.Message, exception);
        }
    }

    /// <summary>把已由正则限定的阶段标记映射到类型化枚举，避免调用方比较魔法字符串。</summary>
    private static bool TryParseStage(string token, out GameReleaseStage stage)
    {
        stage = token switch
        {
            "alpha" => GameReleaseStage.Alpha,
            "beta" => GameReleaseStage.Beta,
            "rc" => GameReleaseStage.ReleaseCandidate,
            "stable" => GameReleaseStage.Stable,
            _ => default,
        };
        return token is "alpha" or "beta" or "rc" or "stable";
    }

    /// <summary>按不允许符号和区域格式的十进制规则解析一个命名数字组，并拒绝整数溢出。</summary>
    private static bool TryParseNumber(Match match, string groupName, out int value) =>
        int.TryParse(match.Groups[groupName].Value, NumberStyles.None,
            CultureInfo.InvariantCulture, out value);
}
