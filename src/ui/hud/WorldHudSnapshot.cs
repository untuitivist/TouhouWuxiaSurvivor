using TouhouWuxiaSurvivor.World.Biomes;
using TouhouWuxiaSurvivor.World.Coordinates;
using TouhouWuxiaSurvivor.Gameplay.SpellCards.Runtime;
using TouhouWuxiaSurvivor.Gameplay.Pacing;

namespace TouhouWuxiaSurvivor.Ui.Hud;

/// <summary>
/// 保存单帧 HUD 所需的只读世界与战斗数据，使采集逻辑和显示格式完全分离。
/// </summary>
public sealed class WorldHudSnapshot
{
    public ulong Seed { get; }
    public long TileX { get; }
    public long TileY { get; }
    public ChunkCoordinate Chunk { get; }
    public BiomeId Biome { get; }
    public int ActiveChunks { get; }
    public int PendingChunks { get; }
    public int AliveEnemies { get; }
    public int DefeatedEnemies { get; }
    public int CurrentHealth { get; }
    public int MaxHealth { get; }
    public double ElapsedSeconds { get; }
    public string ActiveBuffs { get; }
    public string ActiveContent { get; }
    public int Level { get; }
    public long Experience { get; }
    public long ExperienceToNext { get; }
    public RunPacingSnapshot Pacing { get; }
    public SpellCardRuntimeSnapshot SpellCards { get; }

    /// <summary>
    /// 构造一次完整状态快照，供常驻状态栏和可选调试层读取同一份数据。
    /// </summary>
    public WorldHudSnapshot(
        ulong seed,
        long tileX,
        long tileY,
        ChunkCoordinate chunk,
        BiomeId biome,
        int activeChunks,
        int pendingChunks,
        int aliveEnemies,
        int defeatedEnemies,
        int currentHealth,
        int maxHealth,
        double elapsedSeconds,
        string activeBuffs,
        string activeContent,
        int level,
        long experience,
        long experienceToNext,
        RunPacingSnapshot pacing,
        SpellCardRuntimeSnapshot spellCards)
    {
        Seed = seed;
        TileX = tileX;
        TileY = tileY;
        Chunk = chunk;
        Biome = biome;
        ActiveChunks = activeChunks;
        PendingChunks = pendingChunks;
        AliveEnemies = aliveEnemies;
        DefeatedEnemies = defeatedEnemies;
        CurrentHealth = currentHealth;
        MaxHealth = maxHealth;
        ElapsedSeconds = elapsedSeconds;
        ActiveBuffs = activeBuffs;
        ActiveContent = activeContent;
        Level = level;
        Experience = experience;
        ExperienceToNext = experienceToNext;
        Pacing = pacing;
        SpellCards = spellCards;
    }
}
