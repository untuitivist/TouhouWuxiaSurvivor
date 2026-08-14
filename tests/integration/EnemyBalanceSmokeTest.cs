using Godot;
using TouhouWuxiaSurvivor.Actors.Enemies;
using TouhouWuxiaSurvivor.Content;
using TouhouWuxiaSurvivor.Gameplay.Spawning;
using TouhouWuxiaSurvivor.World.Official;

namespace TouhouWuxiaSurvivor.Tests.Integration;

/// <summary>
/// 验证正作三段敌人曲线、威胁评估和全局刷怪节奏满足数值策划边界。
/// </summary>
public partial class EnemyBalanceSmokeTest : Node
{
    /// <summary>
    /// 逐作比较外围、核心、深层敌人，并验证作品编号不会制造难度膨胀。
    /// </summary>
    public override void _Ready()
    {
        try
        {
            foreach (ContentPackDefinition pack in ContentPackCatalog.All)
            {
                VerifyPackageCurve(pack);
            }

            VerifyCrossPackageVariance();
            VerifyThreatProfiles();
            VerifySpawnPacing();
            GD.Print("Enemy balance smoke test passed.");
            GetTree().Quit();
        }
        catch (Exception exception)
        {
            GD.PushError(exception.ToString());
            GetTree().Quit(1);
        }
    }

    /// <summary>
    /// 确认同一作品随地区深入而提高耐久、解锁时间和掉落率，同时降低刷新权重。
    /// </summary>
    private static void VerifyPackageCurve(ContentPackDefinition pack)
    {
        EnemyDefinition[] enemies = OfficialWorldContentCatalog.GetByPack(pack.Id)
            .OrderBy(world => world.RegionIndex)
            .Select(world => EnemyCatalog.All.Single(enemy =>
                enemy.RequiredContentPack == pack.Id && enemy.DisplayName == world.EnemyName))
            .ToArray();
        Require(enemies.Length == 3, $"Package curve is incomplete: {pack.Id}");
        Require(enemies[0].MaxHealth < enemies[1].MaxHealth &&
            enemies[1].MaxHealth < enemies[2].MaxHealth,
            $"Health tiers are not increasing: {pack.Id}");
        Require(enemies[0].UnlockTime < enemies[1].UnlockTime &&
            enemies[1].UnlockTime < enemies[2].UnlockTime,
            $"Unlock tiers are not increasing: {pack.Id}");
        Require(enemies[0].SpawnWeight > enemies[1].SpawnWeight &&
            enemies[1].SpawnWeight > enemies[2].SpawnWeight,
            $"Spawn weights are not decreasing: {pack.Id}");
        Require(enemies[0].DropChance < enemies[1].DropChance &&
            enemies[1].DropChance < enemies[2].DropChance,
            $"Drop tiers are not increasing: {pack.Id}");
    }

    /// <summary>
    /// 限制作品间同层差异，防止编号较大的作品天然成为高难内容包。
    /// </summary>
    private static void VerifyCrossPackageVariance()
    {
        EnemyDefinition[] official = EnemyCatalog.All
            .Where(enemy => enemy.RequiredContentPack is not null)
            .ToArray();
        foreach (int regionIndex in Enumerable.Range(0, 3))
        {
            string[] names = OfficialWorldContentCatalog.All
                .Where(world => world.RegionIndex == regionIndex)
                .Select(world => world.EnemyName)
                .ToArray();
            EnemyDefinition[] tier = official.Where(enemy => names.Contains(enemy.DisplayName)).ToArray();
            Require(tier.Max(enemy => enemy.MaxHealth) - tier.Min(enemy => enemy.MaxHealth) <= 6,
                $"Same-tier health variance is too wide: {regionIndex}");
            Require(tier.Max(enemy => enemy.MoveSpeed) - tier.Min(enemy => enemy.MoveSpeed) <= 4.0f,
                $"Same-tier speed variance is too wide: {regionIndex}");
        }
    }

    /// <summary>
    /// 确认威胁评级完整且大妖怪高于野妖精，预计击破时间与基础射速严格一致。
    /// </summary>
    private static void VerifyThreatProfiles()
    {
        foreach (EnemyDefinition enemy in EnemyCatalog.All)
        {
            EnemyBalanceProfile profile = EnemyBalanceProfile.Evaluate(enemy);
            Require(profile.ThreatRank is >= 1 and <= 5, "Threat rank is outside 1-5.");
            Require(Math.Abs(profile.BaseTimeToKill - enemy.MaxHealth /
                EnemyBalanceProfile.BaseWeaponDamage *
                EnemyBalanceProfile.BaseWeaponInterval) < 0.001f,
                "Base time-to-kill formula drifted.");
        }

        EnemyDefinition fairy = EnemyCatalog.All.First(enemy => enemy.Archetype == EnemyArchetype.Fairy);
        EnemyDefinition great = EnemyCatalog.All.First(enemy => enemy.Archetype == EnemyArchetype.GreatYoukai);
        Require(EnemyBalanceProfile.Evaluate(great).ThreatRank >
            EnemyBalanceProfile.Evaluate(fairy).ThreatRank,
            "Great youkai must rate above the opening fairy.");
    }

    /// <summary>
    /// 验证刷怪批次只在策划节点跳档，间隔单调下降且存活上限受硬限制。
    /// </summary>
    private static void VerifySpawnPacing()
    {
        Require(EnemySpawnPacing.GetBatchSize(0) == 1 &&
            EnemySpawnPacing.GetBatchSize(120) == 2 &&
            EnemySpawnPacing.GetBatchSize(240) == 3 &&
            EnemySpawnPacing.GetBatchSize(420) == 4,
            "Spawn batch milestones are incorrect.");
        Require(EnemySpawnPacing.GetSpawnInterval(0) >
            EnemySpawnPacing.GetSpawnInterval(300) &&
            EnemySpawnPacing.GetSpawnInterval(300) >
            EnemySpawnPacing.GetSpawnInterval(600),
            "Spawn interval is not decreasing.");
        Require(EnemySpawnPacing.GetAliveLimit(0, 140) == 36 &&
            EnemySpawnPacing.GetAliveLimit(1200, 140) == 140,
            "Dynamic alive limit is incorrect.");
    }

    /// <summary>
    /// 将数值策划契约失败转换为带明确原因的测试异常。
    /// </summary>
    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
