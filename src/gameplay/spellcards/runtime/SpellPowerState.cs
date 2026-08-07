namespace TouhouWuxiaSurvivor.Gameplay.SpellCards.Runtime;

/// <summary>
/// 管理一局内共享符卡灵力，统一约束收取换算、容量上限和原子扣费。
/// </summary>
public sealed class SpellPowerState
{
    public const int MaximumPower = 100;
    public const int PowerPerSpiritValue = 4;

    public int CurrentPower { get; private set; }

    /// <summary>
    /// 将正数灵息价值按固定倍率转换为灵力，并把结果限制在容量范围内。
    /// </summary>
    public int GainFromSpirit(int spiritValue)
    {
        if (spiritValue <= 0)
        {
            return 0;
        }

        int previous = CurrentPower;
        CurrentPower = Math.Clamp(
            CurrentPower + spiritValue * PowerPerSpiritValue,
            0,
            MaximumPower);
        return CurrentPower - previous;
    }

    /// <summary>
    /// 在灵力足够时一次性扣除正数消耗，失败时保持状态完全不变。
    /// </summary>
    public bool TrySpend(int cost)
    {
        if (cost <= 0 || CurrentPower < cost)
        {
            return false;
        }

        CurrentPower -= cost;
        return true;
    }

    /// <summary>
    /// 仅供测试和确定性奖励流程设置灵力，并始终执行容量裁剪。
    /// </summary>
    public void SetPower(int value) => CurrentPower = Math.Clamp(value, 0, MaximumPower);
}
