namespace TouhouWuxiaSurvivor.Ui.Stats;

using TouhouWuxiaSurvivor.Content.Characters;
using TouhouWuxiaSurvivor.Gameplay.SpellCards.Runtime;
using TouhouWuxiaSurvivor.Ui;
using TouhouWuxiaSurvivor.Ui.Stats.Build;

/// <summary>
/// 保存属性面板一次刷新所需的最终数值与来源摘要，使界面不依赖任何战斗节点。
/// </summary>
public sealed class CharacterStatsSnapshot
{
    public string CharacterName { get; }
    public CharacterCombatRole CombatRole { get; }
    public string CombatRoleName => CharacterCombatRoleText.GetName(CombatRole);
    public string CombatRoleDescription => CharacterCombatRoleText.Describe(CombatRole);
    public int CurrentHealth { get; }
    public int MaxHealth { get; }
    public int Level { get; }
    public long Experience { get; }
    public long ExperienceToNext { get; }
    public long TotalExperience { get; }
    public int VolleyTotalDamage { get; }
    public int AimedProjectileCount { get; }
    public int AimedTotalDamage { get; }
    public int BarrageProjectileCount { get; }
    public int BarrageTotalDamage { get; }
    public int ProjectileCount { get; }
    public int MinimumProjectileDamage { get; }
    public int MaximumProjectileDamage { get; }
    public int SecondaryVolleyDamage { get; }
    public float FireInterval { get; }
    public float MoveSpeed { get; }
    public float TargetRange { get; }
    public float ProjectileSpeed { get; }
    public float AttractionRange { get; }
    public string PermanentSummary { get; }
    public CharacterBuildViewModel Build { get; }
    public SpellCardRuntimeSnapshot SpellCards { get; }

    /// <summary>
    /// 建立不可变角色快照，供暂停后的属性页稳定显示同一帧数据。
    /// </summary>
    public CharacterStatsSnapshot(
        string characterName,
        CharacterCombatRole combatRole,
        int currentHealth,
        int maxHealth,
        int level,
        long experience,
        long experienceToNext,
        long totalExperience,
        int volleyTotalDamage,
        int aimedProjectileCount,
        int aimedTotalDamage,
        int barrageProjectileCount,
        int barrageTotalDamage,
        int projectileCount,
        int minimumProjectileDamage,
        int maximumProjectileDamage,
        int secondaryVolleyDamage,
        float fireInterval,
        float moveSpeed,
        float targetRange,
        float projectileSpeed,
        float attractionRange,
        string permanentSummary,
        CharacterBuildViewModel build,
        SpellCardRuntimeSnapshot spellCards)
    {
        CharacterName = characterName;
        CombatRole = combatRole;
        CurrentHealth = currentHealth;
        MaxHealth = maxHealth;
        Level = level;
        Experience = experience;
        ExperienceToNext = experienceToNext;
        TotalExperience = totalExperience;
        VolleyTotalDamage = volleyTotalDamage;
        AimedProjectileCount = aimedProjectileCount;
        AimedTotalDamage = aimedTotalDamage;
        BarrageProjectileCount = barrageProjectileCount;
        BarrageTotalDamage = barrageTotalDamage;
        ProjectileCount = projectileCount;
        MinimumProjectileDamage = minimumProjectileDamage;
        MaximumProjectileDamage = maximumProjectileDamage;
        SecondaryVolleyDamage = secondaryVolleyDamage;
        FireInterval = fireInterval;
        MoveSpeed = moveSpeed;
        TargetRange = targetRange;
        ProjectileSpeed = projectileSpeed;
        AttractionRange = attractionRange;
        PermanentSummary = permanentSummary;
        Build = build;
        SpellCards = spellCards;
    }
}
