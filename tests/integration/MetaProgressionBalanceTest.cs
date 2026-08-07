using Godot;
using TouhouWuxiaSurvivor.Gameplay.Meta.Definitions;
using TouhouWuxiaSurvivor.Gameplay.Meta.Persistence;
using TouhouWuxiaSurvivor.Gameplay.Meta.Runtime;
using TouhouWuxiaSurvivor.Tests.Support;

namespace TouhouWuxiaSurvivor.Tests.Integration;

/// <summary>
/// 验证幻想乡钱财、档案修复、整备解锁、失败回滚和永久加成满足局外策划契约。
/// </summary>
public partial class MetaProgressionBalanceTest : Node
{
    /// <summary>
    /// 顺序执行全部纯领域断言，并以明确退出码报告任何局外成长回归。
    /// </summary>
    public override void _Ready()
    {
        try
        {
            VerifyRewardFormula();
            VerifyProfileRepair();
            VerifySettlementAndPurchases();
            VerifySaveFailureRollback();
            VerifyJsonRoundTrip();
            GD.Print("Meta progression balance test passed.");
            GetTree().Quit();
        }
        catch (Exception exception)
        {
            GD.PushError(exception.ToString());
            GetTree().Quit(1);
        }
    }

    /// <summary>
    /// 确认立即死亡无收入、三项战绩共同计分且极端长局被钳制为八十钱。
    /// </summary>
    private static void VerifyRewardFormula()
    {
        Require(RunRewardCalculator.Calculate(44.9, 11, 1) == 0,
            "Immediate low-performance death must not award jade.");
        Require(RunRewardCalculator.Calculate(90, 24, 4) == 7,
            "Run reward components were not added correctly.");
        Require(RunRewardCalculator.Calculate(99999, 99999, 999) == 80,
            "Run reward must be capped at eighty jade.");
    }

    /// <summary>
    /// 确认损坏数值、未知修行、超上限重数和过长结算历史都会被迁移到当前安全范围。
    /// </summary>
    private static void VerifyProfileRepair()
    {
        var profile = new ProgressionProfileData
        {
            Version = -4,
            Money = -10,
            LifetimeMoney = -20,
            CompletedRuns = -2,
            CultivationRanks = new Dictionary<string, int>
            {
                ["hakurei_barrier"] = 99,
                ["unknown"] = 8,
            },
            SettledRunIds = Enumerable.Range(0, 40).Select(index => $"run-{index}").ToList(),
        };
        profile.Repair();
        Require(profile.Version == ProgressionProfileData.CurrentVersion &&
            profile.Money == 0 && profile.LifetimeMoney == 0 &&
            profile.CompletedRuns == 0,
            "Profile scalar repair is incorrect.");
        Require(profile.GetRank("hakurei_barrier") == 3 &&
            !profile.CultivationRanks.ContainsKey("unknown") &&
            profile.SettledRunIds.Count == 32 && profile.SettledRunIds[0] == "run-8",
            "Profile collection repair is incorrect.");
    }

    /// <summary>
    /// 结算钱财、拒绝重复 ID、跨过累计门槛购买整备，并核对实际局内基础加成。
    /// </summary>
    private static void VerifySettlementAndPurchases()
    {
        var store = new MemoryProgressionProfileStore();
        var manager = new ProgressionProfileManager(store);
        Require(manager.Purchase("floating_practice").Status == CultivationPurchaseStatus.Locked,
            "Movement cultivation should begin locked.");

        RunSettlementResult first = manager.SettleRun("run-a", 30);
        RunSettlementResult duplicate = manager.SettleRun("run-a", 30);
        Require(first.WasSettled && first.Reward == 30 && first.Balance == 30 &&
            !duplicate.WasSettled && manager.Current.CompletedRuns == 1,
            "Run settlement is not idempotent.");
        Require(manager.Purchase("floating_practice").Succeeded &&
            manager.Purchase("yin_yang_resonance").Succeeded,
            "Unlocked cultivation could not be purchased.");

        manager.SettleRun("run-b", 70);
        Require(manager.Purchase("persuasion_needle_tuning").Succeeded,
            "Lifetime jade did not unlock damage cultivation.");
        var bonuses = new ProfileRunBonuses(manager.Current);
        Require(bonuses.DamageBonus == 1 && bonuses.MaxHealthBonus == 0 &&
            Mathf.IsEqualApprox(bonuses.MoveSpeedMultiplier, 1.02f) &&
            Mathf.IsEqualApprox(bonuses.SpiritAttractionMultiplier, 1.08f),
            "Profile ranks were not projected to runtime bonuses.");
    }

    /// <summary>
    /// 模拟存储拒绝写入，确认购买不会扣除余额、提高重数或发布半完成状态。
    /// </summary>
    private static void VerifySaveFailureRollback()
    {
        var profile = ProgressionProfileData.CreateDefault();
        profile.Money = 100;
        profile.LifetimeMoney = 100;
        var store = new MemoryProgressionProfileStore(profile) { FailSaves = true };
        var manager = new ProgressionProfileManager(store);
        CultivationPurchaseResult result = manager.Purchase("hakurei_barrier");
        Require(result.Status == CultivationPurchaseStatus.SaveFailed &&
            manager.Current.Money == 100 &&
            manager.Current.GetRank("hakurei_barrier") == 0,
            "Failed save mutated the live profile.");
    }

    /// <summary>
    /// 在项目生成目录实际往返 JSON，确认原子替换完成、数据可读且文本没有 UTF-8 BOM。
    /// </summary>
    private static void VerifyJsonRoundTrip()
    {
        string path = ProjectSettings.GlobalizePath(
            "res://.godot/meta_profile_contract.json");
        var profile = ProgressionProfileData.CreateDefault();
        profile.Money = 27;
        profile.LifetimeMoney = 40;
        profile.CultivationRanks["hakurei_barrier"] = 1;
        var store = new JsonProgressionProfileStore(path);
        Require(store.TrySave(profile), "JSON profile could not be atomically saved.");
        ProgressionProfileData loaded = store.Load();
        byte[] bytes = File.ReadAllBytes(path);
        bool hasBom = bytes.Length >= 3 &&
            bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF;
        Require(loaded.Money == 27 && loaded.LifetimeMoney == 40 &&
            loaded.GetRank("hakurei_barrier") == 1,
            "JSON profile did not round-trip its current contract.");
        Require(!hasBom && !File.Exists(path + ".tmp"),
            "Profile JSON has a BOM or left an incomplete temporary file.");
    }

    /// <summary>
    /// 将局外成长契约失败转换为包含具体原因的测试异常。
    /// </summary>
    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
