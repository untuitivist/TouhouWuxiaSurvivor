using TouhouWuxiaSurvivor.Actors.Enemies;
using TouhouWuxiaSurvivor.Content.Characters;
using TouhouWuxiaSurvivor.Ecs.Combat;

namespace TouhouWuxiaSurvivor.Gameplay.Encounters;

/// <summary>
/// 把角色目录中的 Boss 档案转换为 ECS 敌人定义，并统一应用当前无尽难度快照。
/// </summary>
public static class BossDefinitionFactory
{
    /// <summary>
    /// 为指定角色创建不参与普通权重的 Boss 定义；角色身份、素材来源和接触伤害完整保留。
    /// </summary>
    public static EnemyDefinition Create(
        CharacterDefinition character,
        double elapsedSeconds)
    {
        ArgumentNullException.ThrowIfNull(character);
        BossCharacterProfile profile = character.BossProfile;
        int health = SaturatingPositiveInt(profile.MaxHealth *
            CombatIntensityCurve.GetBossHealthMultiplier(elapsedSeconds));
        int contactDamage = SaturatingPositiveInt(profile.ContactDamage *
            (1.0 + CombatIntensityCurve.GetDamageBonus(elapsedSeconds)));
        float moveSpeed = Math.Min(280.0f, profile.MoveSpeed *
            CombatIntensityCurve.GetMoveSpeedMultiplier(elapsedSeconds));
        return new EnemyDefinition(
            EnemyArchetype.CharacterBoss,
            character.DisplayName,
            health,
            moveSpeed,
            profile.CollisionRadius,
            0.0f,
            0.0f,
            1.0f,
            [],
            requiredContentPack: character.SourcePackId,
            contactDamage: contactDamage,
            aiProfile: EnemyAiProfile.BossPhased,
            projectileProfile: EnemyProjectileProfile.Boss,
            isBoss: true,
            characterId: character.CharacterId);
    }

    /// <summary>把正数浮点战斗数值饱和为可安全相加的整数，拒绝零生命或整数溢出。</summary>
    private static int SaturatingPositiveInt(double value) =>
        (int)Math.Clamp(Math.Ceiling(value), 1.0, int.MaxValue / 2.0);
}
