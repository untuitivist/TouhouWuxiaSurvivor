using Godot;
using TouhouWuxiaSurvivor.Gameplay.Meta.Definitions;
using TouhouWuxiaSurvivor.Gameplay.Meta.Persistence;

namespace TouhouWuxiaSurvivor.Gameplay.Meta.Runtime;

/// <summary>
/// 管理档案购买、重置和单局防重复结算，并只在持久化成功后发布新状态。
/// </summary>
public sealed class ProgressionProfileManager
{
    private const string DefaultPath = "user://progression.json";
    private readonly IProgressionProfileStore _store;
    public ProgressionProfileData Current { get; private set; }
    public event Action? Changed;

    /// <summary>
    /// 从注入存储加载初始档案，供磁盘运行时和内存测试共享全部业务规则。
    /// </summary>
    public ProgressionProfileManager(IProgressionProfileStore store)
    {
        _store = store;
        Current = store.Load();
        Current.Repair();
    }

    /// <summary>
    /// 创建指向 Godot 用户目录正式档案的管理器实例。
    /// </summary>
    public static ProgressionProfileManager CreateDefault() => new(
        new JsonProgressionProfileStore(ProjectSettings.GlobalizePath(DefaultPath)));

    /// <summary>
    /// 检查定义、解锁、重数、余额与保存结果，并以候选副本完成一次原子购买。
    /// </summary>
    public CultivationPurchaseResult Purchase(string definitionId)
    {
        CultivationDefinition? definition = CultivationCatalog.Find(definitionId);
        if (definition is null)
        {
            return new CultivationPurchaseResult(CultivationPurchaseStatus.Unknown, null);
        }

        int rank = Current.GetRank(definition.Id);
        if (Current.LifetimeMoney < definition.UnlockLifetimeMoney)
        {
            return new CultivationPurchaseResult(CultivationPurchaseStatus.Locked, definition);
        }

        if (rank >= definition.MaxRank)
        {
            return new CultivationPurchaseResult(CultivationPurchaseStatus.MaxRank, definition);
        }

        int cost = definition.GetCost(rank);
        if (Current.Money < cost)
        {
            return new CultivationPurchaseResult(
                CultivationPurchaseStatus.InsufficientJade, definition);
        }

        ProgressionProfileData candidate = Current.Clone();
        candidate.Money -= cost;
        candidate.CultivationRanks[definition.Id] = rank + 1;
        if (!_store.TrySave(candidate))
        {
            return new CultivationPurchaseResult(CultivationPurchaseStatus.SaveFailed, definition);
        }

        Current = candidate;
        Changed?.Invoke();
        return new CultivationPurchaseResult(CultivationPurchaseStatus.Purchased, definition);
    }

    /// <summary>
    /// 首次结算时记录运行 ID、累计收入和局数；重复 ID 或保存失败不会再次发奖。
    /// </summary>
    public RunSettlementResult SettleRun(string runId, int reward)
    {
        if (string.IsNullOrWhiteSpace(runId) || Current.SettledRunIds.Contains(runId))
        {
            return new RunSettlementResult(false, 0, Current.Money);
        }

        int safeReward = Math.Clamp(reward, 0, 80);
        ProgressionProfileData candidate = Current.Clone();
        candidate.Money += safeReward;
        candidate.LifetimeMoney += safeReward;
        candidate.CompletedRuns++;
        candidate.SettledRunIds.Add(runId);
        candidate.Repair();
        if (!_store.TrySave(candidate))
        {
            return new RunSettlementResult(false, 0, Current.Money);
        }

        Current = candidate;
        Changed?.Invoke();
        return new RunSettlementResult(true, safeReward, Current.Money);
    }

    /// <summary>
    /// 尝试把档案原子覆盖为默认状态，保存失败时保留当前进度并返回失败。
    /// </summary>
    public bool Reset()
    {
        ProgressionProfileData candidate = ProgressionProfileData.CreateDefault();
        candidate.Repair();
        if (!_store.TrySave(candidate))
        {
            return false;
        }

        Current = candidate;
        Changed?.Invoke();
        return true;
    }
}
