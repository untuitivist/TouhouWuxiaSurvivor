namespace TouhouWuxiaSurvivor.Diagnostics.Performance;

/// <summary>
/// 汇总一秒采样所需的局内状态，避免诊断器遍历 ECS 实体或依赖具体场景节点结构。
/// </summary>
public sealed class PerformanceDiagnosticsRuntimeSnapshot
{
    public string SceneName { get; }
    public string ActiveContent { get; }
    public string CharacterId { get; }
    public string CharacterName { get; }
    public double RunSeconds { get; }
    public float PlayerX { get; }
    public float PlayerY { get; }
    public int ActiveChunks { get; }
    public int PendingChunks { get; }
    public int AliveEnemies { get; }
    public int EnemyPoolCount { get; }
    public int AliveBosses { get; }
    public int DefeatedEnemies { get; }
    public int Projectiles { get; }
    public int EnemyProjectiles { get; }
    public int ProjectileCapacity { get; }
    public int Pickups { get; }
    public int Spirits { get; }
    public int Level { get; }
    public int MappedVisuals { get; }
    public int FallbackVisuals { get; }
    public long PlayerCollisionChecks { get; }
    public bool TreePaused { get; }
    public string ActiveModal { get; }

    /// <summary>
    /// 构造一次世界聚合快照；所有计数均来自现有池属性，不执行实体级查询。
    /// </summary>
    public PerformanceDiagnosticsRuntimeSnapshot(
        string sceneName, string activeContent, string characterId, string characterName,
        double runSeconds, float playerX, float playerY,
        int activeChunks, int pendingChunks,
        int aliveEnemies, int enemyPoolCount, int aliveBosses, int defeatedEnemies,
        int projectiles, int enemyProjectiles, int projectileCapacity,
        int pickups, int spirits, int level, int mappedVisuals, int fallbackVisuals,
        long playerCollisionChecks = 0, bool treePaused = false, string activeModal = "none")
    {
        SceneName = sceneName;
        ActiveContent = activeContent;
        CharacterId = characterId;
        CharacterName = characterName;
        RunSeconds = runSeconds;
        PlayerX = playerX;
        PlayerY = playerY;
        ActiveChunks = activeChunks;
        PendingChunks = pendingChunks;
        AliveEnemies = aliveEnemies;
        EnemyPoolCount = enemyPoolCount;
        AliveBosses = aliveBosses;
        DefeatedEnemies = defeatedEnemies;
        Projectiles = projectiles;
        EnemyProjectiles = enemyProjectiles;
        ProjectileCapacity = projectileCapacity;
        Pickups = pickups;
        Spirits = spirits;
        Level = level;
        MappedVisuals = mappedVisuals;
        FallbackVisuals = fallbackVisuals;
        PlayerCollisionChecks = playerCollisionChecks;
        TreePaused = treePaused;
        ActiveModal = activeModal;
    }

    /// <summary>
    /// 在主菜单或场景切换间隙创建零负载快照，保持 JSONL 字段稳定且可直接横向比较。
    /// </summary>
    public static PerformanceDiagnosticsRuntimeSnapshot Empty(string sceneName) => new(
        sceneName, "none", "none", "none", 0.0, 0.0f, 0.0f,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
}
