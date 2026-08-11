namespace TouhouWuxiaSurvivor.Ui.Stats;

using TouhouWuxiaSurvivor.Gameplay.SpellCards.Runtime;

/// <summary>
/// 保存属性面板一次刷新所需的最终数值与来源摘要，使界面不依赖任何战斗节点。
/// </summary>
public sealed class CharacterStatsSnapshot
{
    public string CharacterName { get; }
    public int CurrentHealth { get; }
    public int MaxHealth { get; }
    public int Level { get; }
    public long Experience { get; }
    public long ExperienceToNext { get; }
    public long TotalExperience { get; }
    public int Damage { get; }
    public float FireInterval { get; }
    public float MoveSpeed { get; }
    public float TargetRange { get; }
    public float ProjectileSpeed { get; }
    public float AttractionRange { get; }
    public string PermanentSummary { get; }
    public string RunBuildSummary { get; }
    public SpellCardRuntimeSnapshot SpellCards { get; }

    /// <summary>
    /// 建立不可变角色快照，供暂停后的属性页稳定显示同一帧数据。
    /// </summary>
    public CharacterStatsSnapshot(
        string characterName,
        int currentHealth,
        int maxHealth,
        int level,
        long experience,
        long experienceToNext,
        long totalExperience,
        int damage,
        float fireInterval,
        float moveSpeed,
        float targetRange,
        float projectileSpeed,
        float attractionRange,
        string permanentSummary,
        string runBuildSummary,
        SpellCardRuntimeSnapshot spellCards)
    {
        CharacterName = characterName;
        CurrentHealth = currentHealth;
        MaxHealth = maxHealth;
        Level = level;
        Experience = experience;
        ExperienceToNext = experienceToNext;
        TotalExperience = totalExperience;
        Damage = damage;
        FireInterval = fireInterval;
        MoveSpeed = moveSpeed;
        TargetRange = targetRange;
        ProjectileSpeed = projectileSpeed;
        AttractionRange = attractionRange;
        PermanentSummary = permanentSummary;
        RunBuildSummary = runBuildSummary;
        SpellCards = spellCards;
    }
}
