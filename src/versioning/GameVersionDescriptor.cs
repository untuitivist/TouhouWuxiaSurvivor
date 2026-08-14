using System.Globalization;

namespace TouhouWuxiaSurvivor.Versioning;

/// <summary>
/// 保存解析后的开发阶段、主版本、发布序号和优化序号，并集中生成各平台需要的版本文本。
/// </summary>
public sealed record GameVersionDescriptor
{
    public GameReleaseStage Stage { get; }
    public int Major { get; }
    public int Release { get; }
    public int Optimization { get; }

    /// <summary>仅允许版本解析器建立已经校验过的不可变版本描述。</summary>
    internal GameVersionDescriptor(
        GameReleaseStage stage,
        int major,
        int release,
        int optimization)
    {
        Stage = stage;
        Major = major;
        Release = release;
        Optimization = optimization;
    }

    /// <summary>将三个游戏数字维度映射为 Windows 接受的四段纯数字资源版本。</summary>
    public string ToWindowsNumericVersion() => string.Create(
        CultureInfo.InvariantCulture,
        $"{Major}.{Release}.{Optimization}.0");

    /// <summary>按唯一规范重新组成 <c>{stage}-{major}.{release}.{optimization}</c> 文本。</summary>
    public override string ToString() => string.Create(
        CultureInfo.InvariantCulture,
        $"{GetStageToken()}-{Major}.{Release}.{Optimization}");

    /// <summary>把阶段枚举映射到版本文件名允许的小写 ASCII 标记。</summary>
    private string GetStageToken() => Stage switch
    {
        GameReleaseStage.Alpha => "alpha",
        GameReleaseStage.Beta => "beta",
        GameReleaseStage.ReleaseCandidate => "rc",
        GameReleaseStage.Stable => "stable",
        _ => throw new InvalidOperationException($"Unsupported game release stage: {Stage}."),
    };
}
