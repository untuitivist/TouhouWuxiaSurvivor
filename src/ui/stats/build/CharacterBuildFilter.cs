namespace TouhouWuxiaSurvivor.Ui.Stats.Build;

/// <summary>
/// 声明构筑页的稳定筛选语义，避免按钮文字直接参与业务判断。
/// </summary>
public enum CharacterBuildFilter
{
    All,
    Learned,
    Available,
    Locked,
    MartialArt,
    InnerArt,
    SpellCard,
    Specialization,
}
