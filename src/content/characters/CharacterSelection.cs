namespace TouhouWuxiaSurvivor.Content.Characters;

/// <summary>
/// 表示下一局或当前局选定自机的不可变快照，避免菜单后续改动污染运行世界。
/// </summary>
public sealed class CharacterSelection
{
    public CharacterDefinition Current { get; }
    public string CharacterId => Current.CharacterId;

    /// <summary>
    /// 从目录中的完整角色定义建立选择快照，并保留同一稳定身份供 Boss 排除使用。
    /// </summary>
    public CharacterSelection(CharacterDefinition current)
    {
        ArgumentNullException.ThrowIfNull(current);
        Current = current;
    }
}
