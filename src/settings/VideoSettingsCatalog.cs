using Godot;

namespace TouhouWuxiaSurvivor.Settings;

/// <summary>
/// 定义设置界面与存档迁移共同使用的视频选项，防止界面显示值和引擎实际值发生分叉。
/// </summary>
public static class VideoSettingsCatalog
{
    private static readonly Vector2I[] ResolutionValues =
    [
        new(640, 360),
        new(960, 540),
        new(1280, 720),
        new(1600, 900),
        new(1920, 1080),
        new(2560, 1440),
        new(3840, 2160),
    ];

    private static readonly int[] FpsLimitValues = [30, 60, 120, 144, 0];

    public static IReadOnlyList<Vector2I> Resolutions => ResolutionValues;
    public static IReadOnlyList<int> FpsLimits => FpsLimitValues;
    public static Vector2I DefaultResolution => new(1280, 720);
    public const int DefaultMaxFps = 60;

    /// <summary>
    /// 返回指定持久化分辨率的稳定选项索引；无法表示的旧值返回默认分辨率索引。
    /// </summary>
    public static int FindResolutionIndex(int width, int height)
    {
        int index = Array.FindIndex(ResolutionValues, value =>
            value.X == width && value.Y == height);
        return index >= 0 ? index : Array.IndexOf(ResolutionValues, DefaultResolution);
    }

    /// <summary>
    /// 返回指定帧率上限的稳定选项索引；异常旧值返回默认六十帧索引。
    /// </summary>
    public static int FindFpsIndex(int fps)
    {
        return Array.IndexOf(FpsLimitValues, NormalizeFpsLimit(fps));
    }

    /// <summary>
    /// 按经过界面校验的索引读取分辨率，越界时明确回退为默认值。
    /// </summary>
    public static Vector2I GetResolution(int index) =>
        index >= 0 && index < ResolutionValues.Length
            ? ResolutionValues[index]
            : DefaultResolution;

    /// <summary>
    /// 按经过界面校验的索引读取帧率上限，越界时明确回退为默认值。
    /// </summary>
    public static int GetFpsLimit(int index) =>
        index >= 0 && index < FpsLimitValues.Length
            ? FpsLimitValues[index]
            : DefaultMaxFps;

    /// <summary>
    /// 保留正式选项，非法值映射到最近的有限档位；等距时取较低档，绝不意外变为无限制。
    /// </summary>
    public static int NormalizeFpsLimit(int fps)
    {
        if (Array.IndexOf(FpsLimitValues, fps) >= 0) return fps;
        return FpsLimitValues
            .Where(value => value > 0)
            .OrderBy(value => Math.Abs((long)value - fps))
            .ThenBy(value => value)
            .First();
    }

    /// <summary>
    /// 把旧存档中界面无法表达的视频值修复成正式选项，并返回可审计的前后差异。
    /// </summary>
    public static GameSettingsRepairReport Normalize(GameSettingsData settings)
    {
        int originalWidth = settings.ResolutionWidth;
        int originalHeight = settings.ResolutionHeight;
        int originalMaxFps = settings.MaxFps;
        Vector2I resolution = GetResolution(FindResolutionIndex(originalWidth, originalHeight));
        int maxFps = NormalizeFpsLimit(originalMaxFps);
        settings.ResolutionWidth = resolution.X;
        settings.ResolutionHeight = resolution.Y;
        settings.MaxFps = maxFps;
        return new GameSettingsRepairReport(
            originalWidth, originalHeight,
            resolution.X, resolution.Y,
            originalMaxFps, maxFps);
    }
}
