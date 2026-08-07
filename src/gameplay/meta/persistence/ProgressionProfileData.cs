using TouhouWuxiaSurvivor.Gameplay.Meta.Definitions;

namespace TouhouWuxiaSurvivor.Gameplay.Meta.Persistence;

/// <summary>
/// 保存版本化局外进度，并负责把旧档、损坏数值和未知修行修复为当前契约。
/// </summary>
public sealed class ProgressionProfileData
{
    public const int CurrentVersion = 1;
    public int Version { get; set; } = CurrentVersion;
    public int Money { get; set; }
    public int LifetimeMoney { get; set; }
    public int CompletedRuns { get; set; }
    public Dictionary<string, int> CultivationRanks { get; set; } = [];
    public List<string> SettledRunIds { get; set; } = [];

    /// <summary>
    /// 创建没有货币、战绩或修行的当前版本档案。
    /// </summary>
    public static ProgressionProfileData CreateDefault() => new();

    /// <summary>
    /// 返回指定稳定 ID 的修行重数，尚未购买或未知 ID 均返回零。
    /// </summary>
    public int GetRank(string id) => CultivationRanks.GetValueOrDefault(id);

    /// <summary>
    /// 钳制货币与重数、移除未知定义，并只保留最近三十二个不重复结算 ID。
    /// </summary>
    public void Repair()
    {
        Version = CurrentVersion;
        Money = Math.Max(0, Money);
        LifetimeMoney = Math.Max(Money, LifetimeMoney);
        CompletedRuns = Math.Max(0, CompletedRuns);
        CultivationRanks ??= [];
        CultivationRanks = CultivationCatalog.All.ToDictionary(
            definition => definition.Id,
            definition => Math.Clamp(GetRank(definition.Id), 0, definition.MaxRank));
        SettledRunIds ??= [];
        SettledRunIds = SettledRunIds
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.Ordinal)
            .TakeLast(32)
            .ToList();
    }

    /// <summary>
    /// 建立深复制候选档案，使保存失败可以保留当前内存状态而不发生半次购买。
    /// </summary>
    public ProgressionProfileData Clone() => new()
    {
        Version = Version,
        Money = Money,
        LifetimeMoney = LifetimeMoney,
        CompletedRuns = CompletedRuns,
        CultivationRanks = new Dictionary<string, int>(CultivationRanks),
        SettledRunIds = [.. SettledRunIds],
    };
}
