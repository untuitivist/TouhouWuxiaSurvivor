namespace TouhouWuxiaSurvivor.Content.Characters;

/// <summary>
/// 提供角色基础属性的归一化预算评分，供内容校验与后续数值调整共享同一计算口径。
/// </summary>
public static class CharacterBalanceBudget
{
    /// <summary>
    /// 将生命、移动、普攻与奥义效率按实战占比折算为自机总预算；标准均衡角色得分为一。
    /// </summary>
    public static float EvaluatePlayable(PlayableCharacterProfile profile)
    {
        float survivability = profile.MaxHealth / 6.0f * 0.25f;
        float mobility = profile.MoveSpeedMultiplier * 0.20f;
        float offense = profile.AttackMultiplier /
            profile.AttackIntervalMultiplier * 0.30f;
        float ultimate = 6.0f / profile.UltimateIntervalSeconds *
            profile.UltimateTargetCapacity / 7.0f * 0.25f;
        return survivability + mobility + offense + ultimate;
    }

    /// <summary>
    /// 将耐久、追击、接触威胁与体型折算为 Boss 总预算，用于限制定位之间的极端强度差。
    /// </summary>
    public static float EvaluateBoss(BossCharacterProfile profile)
    {
        float endurance = profile.MaxHealth / 900.0f * 0.50f;
        float pursuit = profile.MoveSpeed / 36.0f * 0.20f;
        float contact = profile.ContactDamage / 2.0f * 0.20f;
        float body = profile.CollisionRadius / 20.0f * 0.10f;
        return endurance + pursuit + contact + body;
    }
}
