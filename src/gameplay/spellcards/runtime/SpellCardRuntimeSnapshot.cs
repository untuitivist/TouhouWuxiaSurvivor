using TouhouWuxiaSurvivor.Gameplay.SpellCards.Definitions;

namespace TouhouWuxiaSurvivor.Gameplay.SpellCards.Runtime;

/// <summary>
/// 封装当前装备符卡、灵力和冷却的只读状态，供 HUD 与属性页稳定投影。
/// </summary>
public sealed class SpellCardRuntimeSnapshot
{
    public IReadOnlyList<SpellCardDefinition> UnlockedCards { get; }
    public int CurrentPower { get; }
    public int MaximumPower { get; }
    public float CooldownRemaining { get; }
    public bool HasUnlockedCard => UnlockedCards.Count > 0;

    /// <summary>
    /// 建立单帧符卡快照，复制已悟得目录以免界面观察到运行集合的后续变化。
    /// </summary>
    public SpellCardRuntimeSnapshot(
        IReadOnlyList<SpellCardDefinition> unlockedCards,
        int currentPower,
        int maximumPower,
        float cooldownRemaining)
    {
        UnlockedCards = unlockedCards.ToArray();
        CurrentPower = currentPower;
        MaximumPower = maximumPower;
        CooldownRemaining = Math.Max(0.0f, cooldownRemaining);
    }
}
