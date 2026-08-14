namespace TouhouWuxiaSurvivor.Gameplay.SpellCards.Contracts;

/// <summary>
/// 定义奥义运行时读取基础属性的最小边界，使效果与计时系统不依赖具体玩家、武器或成长组件。
/// </summary>
public interface ISpellCardAttributeProvider
{
    /// <summary>捕获当前施展瞬间的一致属性快照，调用方不得持有后继续观察可变组件。</summary>
    SpellCardBaseAttributes Capture();
}
