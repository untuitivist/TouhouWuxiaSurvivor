using Godot;
using TouhouWuxiaSurvivor.Content;
using TouhouWuxiaSurvivor.World.Biomes;
using TouhouWuxiaSurvivor.World.Official;

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
            new(EnemyArchetype.Fairy, "野妖精", 2, 52.0f, 5.0f, 38.0f, 0.0f, 0.28f, []),
            new(EnemyArchetype.Kedama, "毛玉", 3, 42.0f, 6.0f, 34.0f, 0.0f, 0.32f,
                [BiomeId.Common, BiomeId.HakureiShrine, BiomeId.HumanVillage]),
            new(EnemyArchetype.Insect, "妖虫", 3, 46.0f, 6.0f, 30.0f, 15.0f, 0.35f,
                [BiomeId.MagicForest]),
            new(EnemyArchetype.YinYangOrb, "阴阳玉", 5, 68.0f, 7.0f, 18.0f, 45.0f, 0.42f,
                [BiomeId.Common, BiomeId.YoukaiMountain]),
            new(EnemyArchetype.ForestSpirit, "森林精怪", 7, 34.0f, 9.0f, 18.0f, 60.0f, 0.48f,
                [BiomeId.MagicForest]),
            new(EnemyArchetype.MountainSpirit, "山精", 10, 30.0f, 10.0f, 20.0f, 90.0f, 0.55f,
                [BiomeId.YoukaiMountain]),
            new(EnemyArchetype.VillageOutlaw, "流窜妖怪", 8, 48.0f, 8.0f, 14.0f, 105.0f, 0.52f,
                [BiomeId.HumanVillage, BiomeId.Common]),
            new(EnemyArchetype.WanderingYoukai, "夜行妖怪", 12, 42.0f, 9.0f, 10.0f, 150.0f, 0.62f, []),
            new(EnemyArchetype.GreatYoukai, "大妖怪", 30, 36.0f, 12.0f, 3.0f, 240.0f, 0.95f, []),
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
            content.PackId);
    }

    /// <summary>
    /// 生成三段生存曲线：外围快速低耐久、核心均衡、深层低频高耐久，并加入最多两点扰动。
    /// </summary>
    private static OfficialEnemyStats GetOfficialStats(int number, int regionIndex)
    {
        int variation = number * 7 % 3;
        return regionIndex switch
        {
            0 => new OfficialEnemyStats(
                4 + variation, 58.0f + number % 3 * 2.0f, 6.0f,
                18.0f, 12.0f + number % 4 * 6.0f, 0.30f),
            1 => new OfficialEnemyStats(
                10 + variation * 2, 46.0f + number % 3 * 2.0f, 8.0f,
                9.0f, 90.0f + number % 4 * 8.0f, 0.46f),
            _ => new OfficialEnemyStats(
                24 + variation * 3, 36.0f + number % 3 * 2.0f, 11.0f,
                3.2f, 210.0f + number % 4 * 10.0f, 0.70f),
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
