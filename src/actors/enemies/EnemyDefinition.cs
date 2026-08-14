using TouhouWuxiaSurvivor.World.Biomes;

namespace TouhouWuxiaSurvivor.Actors.Enemies;

/// <summary>
/// 保存一种敌人的只读战斗数值、显示名称、出现节奏和掉落概率，并分别记录出生耐久与奖励用基础耐久。
/// </summary>
public sealed class EnemyDefinition
{
    public EnemyArchetype Archetype { get; }
    public string DisplayName { get; }
    public int MaxHealth { get; }
    /// <summary>保存未乘无尽生命倍率的策划耐久，供击破奖励避免重复消费时间成长。</summary>
    public int BaseMaxHealth { get; }
    public float MoveSpeed { get; }
    public float CollisionRadius { get; }
    public float SpawnWeight { get; }
    public float UnlockTime { get; }
    public float DropChance { get; }
    public IReadOnlyList<BiomeId> AllowedBiomes { get; }
    public bool ExplodesOnDeath { get; }
    public string? RequiredContentPack { get; }
    public int ContactDamage { get; }
    public EnemyAiProfile AiProfile { get; }
    public EnemyProjectileProfile ProjectileProfile { get; }
    public bool IsBoss { get; }
    public string? CharacterId { get; }

    /// <summary>
    /// 构造一份经过目录集中管理的敌人定义，避免运行实体自行决定平衡参数。
    /// </summary>
    public EnemyDefinition(
        EnemyArchetype archetype,
        string displayName,
        int maxHealth,
        float moveSpeed,
        float collisionRadius,
        float spawnWeight,
        float unlockTime,
        float dropChance,
        IReadOnlyList<BiomeId> allowedBiomes,
        bool explodesOnDeath = false,
        string? requiredContentPack = null,
        int contactDamage = 1,
        EnemyAiProfile? aiProfile = null,
        EnemyProjectileProfile? projectileProfile = null,
        bool isBoss = false,
        string? characterId = null,
        int? baseMaxHealth = null)
    {
        Archetype = archetype;
        DisplayName = displayName;
        MaxHealth = maxHealth;
        BaseMaxHealth = Math.Max(1, baseMaxHealth ?? maxHealth);
        MoveSpeed = moveSpeed;
        CollisionRadius = collisionRadius;
        SpawnWeight = spawnWeight;
        UnlockTime = unlockTime;
        DropChance = dropChance;
        AllowedBiomes = allowedBiomes;
        ExplodesOnDeath = explodesOnDeath;
        RequiredContentPack = requiredContentPack;
        ContactDamage = Math.Max(1, contactDamage);
        AiProfile = aiProfile ?? EnemyAiProfile.Chase;
        ProjectileProfile = projectileProfile ?? EnemyProjectileProfile.None;
        IsBoss = isBoss;
        CharacterId = characterId;
    }

    /// <summary>
    /// 判断敌人是否属于指定群系；空群系列表表示可在所有地区出现的通用敌人。
    /// </summary>
    public bool CanSpawnIn(BiomeId biome) =>
        AllowedBiomes.Count == 0 || AllowedBiomes.Contains(biome);
}
