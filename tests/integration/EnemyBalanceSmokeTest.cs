using Godot;
using TouhouWuxiaSurvivor.Actors.Enemies;
using TouhouWuxiaSurvivor.Content;
using TouhouWuxiaSurvivor.Gameplay.Spawning;
using TouhouWuxiaSurvivor.Gameplay.Pacing;
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
        Require(enemies[0].StrengthTier == EnemyStrengthTier.Common &&
            enemies[1].StrengthTier == EnemyStrengthTier.Veteran &&
            enemies[2].StrengthTier == EnemyStrengthTier.Champion,
            $"Package enemies do not preserve horizontal strength roles: {pack.Id}");
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
    /// 验证总刷新率逐档上升，四档占比从全普通敌人平滑演进，且不存在存活软上限入口。
    /// </summary>
    private static void VerifySpawnPacing()
    {
        double[] seconds = [0, 30, 60, 90, 120, 150, 180, 210];
        double[] rates = [0.80, 1.05, 1.35, 1.70, 2.05, 2.40, 2.75, 3.10];
        for (int index = 0; index < seconds.Length; index++)
        {
            EnemyPressureSnapshot pressure = EnemySpawnPacing.GetPressure(seconds[index]);
            Require(Math.Abs(pressure.SpawnRatePerSecond - rates[index]) < 0.001,
                $"Spawn rate drifted at pressure gear {index}.");
            if (index > 0)
            {
                Require(rates[index] > rates[index - 1],
                    "Total spawn rate did not strictly rise between gears.");
            }
        }

        EnemyPressureSnapshot opening = EnemySpawnPacing.GetPressure(0.0);
        EnemyPressureSnapshot final = EnemySpawnPacing.GetPressure(210.0);
        Require(opening.TierMix.Common == 1.0 && opening.TierMix.Veteran == 0.0 &&
            final.TierMix.Common >= 0.40 && final.TierMix.Champion <= 0.08 &&
            final.TierMix.Veteran > 0.0 && final.TierMix.Elite > 0.0,
            "Enemy tier mix stopped preserving a large common-enemy share.");
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
