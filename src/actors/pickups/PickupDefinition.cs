namespace TouhouWuxiaSurvivor.Actors.Pickups;

/// <summary>
/// 保存一种掉落物的中文显示名、抽取权重、持续时间和玩家数值倍率。
/// </summary>
public sealed class PickupDefinition
{
    public PickupKind Kind { get; }
    public string DisplayName { get; }
    public float DropWeight { get; }
    public float Duration { get; }
    public float MoveSpeedMultiplier { get; }
    public float FireRateMultiplier { get; }
    public bool EnablesSpiral { get; }

    /// <summary>
    /// 构造一份完整的掉落定义，使拾取实体只负责生命周期而不硬编码强化规则。
    /// </summary>
    public PickupDefinition(
        PickupKind kind,
        string displayName,
        float dropWeight,
        float duration,
        float moveSpeedMultiplier,
        float fireRateMultiplier,
        bool enablesSpiral)
    {
        Kind = kind;
        DisplayName = displayName;
        DropWeight = dropWeight;
        Duration = duration;
        MoveSpeedMultiplier = moveSpeedMultiplier;
        FireRateMultiplier = fireRateMultiplier;
        EnablesSpiral = enablesSpiral;
    }
}
