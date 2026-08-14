using Godot;
using TouhouWuxiaSurvivor.Content;
using TouhouWuxiaSurvivor.World.Biomes;
using TouhouWuxiaSurvivor.World.Official;
using TouhouWuxiaSurvivor.Gameplay.Pacing;

namespace TouhouWuxiaSurvivor.Actors.Enemies;

/// <summary>
/// 集中声明本体和内容包敌人数值，并按群系、生存时间和权重选择当前允许刷新的敌人。
/// </summary>
public static class EnemyCatalog
{
    public static IReadOnlyList<EnemyDefinition> All { get; } = BuildAll();

    /// <summary>
    /// 先建立九类本体敌人，再为每个正作地区生成一类专属敌人，避免重复维护包 ID。
    /// </summary>
    private static IReadOnlyList<EnemyDefinition> BuildAll()
    {
        var definitions = new List<EnemyDefinition>
        {
            new(EnemyArchetype.Fairy, "野妖精", 18, 52.0f, 5.0f, 38.0f, 0.0f, 0.018f, [],
                aiProfile: EnemyAiProfile.OrbitShooter,
                projectileProfile: EnemyProjectileProfile.Aimed),
            new(EnemyArchetype.Kedama, "毛玉", 24, 42.0f, 6.0f, 34.0f, 0.0f, 0.022f,
                [BiomeId.Common, BiomeId.HakureiShrine, BiomeId.HumanVillage]),
            new(EnemyArchetype.Insect, "妖虫", 26, 46.0f, 6.0f, 30.0f,
                (float)RunPacingTimeline.RisingSeconds, 0.026f,
                [BiomeId.MagicForest], aiProfile: EnemyAiProfile.Charger),
            new(EnemyArchetype.YinYangOrb, "阴阳玉", 42, 68.0f, 7.0f, 18.0f,
                (float)RunPacingTimeline.SwarmingSeconds, 0.030f,
                [BiomeId.Common, BiomeId.YoukaiMountain],
                aiProfile: EnemyAiProfile.OrbitShooter,
                projectileProfile: EnemyProjectileProfile.Fan),
            new(EnemyArchetype.ForestSpirit, "森林精怪", 58, 34.0f, 9.0f, 18.0f,
                (float)RunPacingTimeline.SwarmingSeconds, 0.034f,
                [BiomeId.MagicForest], aiProfile: EnemyAiProfile.Charger),
            new(EnemyArchetype.MountainSpirit, "山精", 82, 30.0f, 10.0f, 20.0f,
                (float)RunPacingTimeline.BarrageSeconds, 0.040f,
                [BiomeId.YoukaiMountain]),
            new(EnemyArchetype.VillageOutlaw, "流窜妖怪", 68, 48.0f, 8.0f, 14.0f,
                (float)RunPacingTimeline.BarrageSeconds, 0.038f,
                [BiomeId.HumanVillage, BiomeId.Common],
                aiProfile: EnemyAiProfile.OrbitShooter,
                projectileProfile: EnemyProjectileProfile.Aimed),
            new(EnemyArchetype.WanderingYoukai, "夜行妖怪", 98, 42.0f, 9.0f, 10.0f,
                (float)RunPacingTimeline.CrisisSeconds, 0.048f, [],
                aiProfile: EnemyAiProfile.Charger),
            new(EnemyArchetype.GreatYoukai, "大妖怪", 240, 36.0f, 12.0f, 3.0f,
                (float)RunPacingTimeline.CrisisSeconds, 0.080f, [],
                contactDamage: 2,
                aiProfile: EnemyAiProfile.OrbitShooter,
                projectileProfile: EnemyProjectileProfile.Fan),
        };

        foreach (OfficialWorldContentDefinition content in OfficialWorldContentCatalog.All)
        {
            definitions.Add(CreateOfficialEnemy(content));
        }

        return definitions;
    }

    /// <summary>
    /// 按外围、核心、深层三段统一曲线生成数值，作品编号只制造小幅生态差异而不决定难度。
    /// </summary>
    private static EnemyDefinition CreateOfficialEnemy(OfficialWorldContentDefinition content)
    {
        EnemyArchetype archetype = (content.Number, content.RegionIndex) switch
        {
            (6, 1) => EnemyArchetype.ScarletMistInsect,
            (8, 0) => EnemyArchetype.BambooSpirit,
            _ => EnemyArchetype.OfficialSpirit,
        };
        OfficialEnemyStats stats = GetOfficialStats(content.Number, content.RegionIndex);
        return new EnemyDefinition(
            archetype,
            content.EnemyName,
            stats.Health,
            stats.Speed,
            stats.Radius,
            stats.Weight,
            stats.UnlockTime,
            stats.DropChance,
            [content.Biome],
            content.Number == 6 && content.RegionIndex == 1,
            content.PackId,
            contactDamage: content.RegionIndex == 2 ? 2 : 1,
            aiProfile: GetOfficialAiProfile(content.RegionIndex),
            projectileProfile: content.RegionIndex == 1
                ? EnemyProjectileProfile.Aimed
                : EnemyProjectileProfile.None);
    }

    /// <summary>
    /// 让每部正作的外围、核心、深层各自采用追击、绕射、突进职责，形成稳定而可预期的生态组合。
    /// </summary>
    private static EnemyAiProfile GetOfficialAiProfile(int regionIndex) => regionIndex switch
    {
        0 => EnemyAiProfile.Chase,
        1 => EnemyAiProfile.OrbitShooter,
        _ => EnemyAiProfile.Charger,
    };

    /// <summary>
    /// 生成三段生存曲线：外围快速低耐久、核心均衡、深层低频高耐久，并加入最多两点扰动。
    /// </summary>
    private static OfficialEnemyStats GetOfficialStats(int number, int regionIndex)
    {
        _ = number;
        return regionIndex switch
        {
            0 => new OfficialEnemyStats(
                38, 60.0f, 6.0f, 18.0f, 18.0f, 0.022f),
            1 => new OfficialEnemyStats(
                92, 48.0f, 8.0f, 9.0f, 96.0f, 0.038f),
            _ => new OfficialEnemyStats(
                216, 38.0f, 11.0f, 3.2f, 220.0f, 0.065f),
        };
    }

    /// <summary>
    /// 封装一次正作敌人数值计算结果，避免构造调用依赖难以审查的位置参数。
    /// </summary>
    private readonly record struct OfficialEnemyStats(
        int Health,
        float Speed,
        float Radius,
        float Weight,
        float UnlockTime,
        float DropChance);

    /// <summary>
    /// 只从当前群系、时间阶段和内容选择允许的敌人中加权随机，形成地区生态差异。
    /// </summary>
    public static EnemyDefinition Choose(
        RandomNumberGenerator random,
        double elapsedSeconds,
        BiomeId biome,
        ContentPackSelection content)
    {
        float totalWeight = 0.0f;
        foreach (EnemyDefinition definition in All)
        {
            if (IsAvailable(definition, elapsedSeconds, biome, content))
            {
                totalWeight += definition.SpawnWeight;
            }
        }

        float targetWeight = random.RandfRange(0.0f, totalWeight);
        foreach (EnemyDefinition definition in All)
        {
            if (!IsAvailable(definition, elapsedSeconds, biome, content))
            {
                continue;
            }

            targetWeight -= definition.SpawnWeight;
            if (targetWeight <= 0.0f)
            {
                return definition;
            }
        }

        return All[0];
    }

    /// <summary>
    /// 同时检查时间、群系和内容包，确保地区专属敌人与正作内容不会泄漏。
    /// </summary>
    private static bool IsAvailable(
        EnemyDefinition definition,
        double elapsedSeconds,
        BiomeId biome,
        ContentPackSelection content) =>
        elapsedSeconds >= definition.UnlockTime &&
        definition.CanSpawnIn(biome) &&
        (definition.RequiredContentPack is null || content.IsEnabled(definition.RequiredContentPack));
}
