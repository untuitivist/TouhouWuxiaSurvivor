namespace TouhouWuxiaSurvivor.Content.Characters;

/// <summary>
/// 将本局启用内容与自机选择冻结为同一上下文，供世界、成长和 Boss 系统共享。
/// </summary>
public sealed class RunContentContext
{
    public ContentPackSelection ContentSelection { get; }
    public CharacterSelection CharacterSelection { get; }

    /// <summary>
    /// 构造一致的局内内容快照，并拒绝与启用内容不相容的角色组合。
    /// </summary>
    public RunContentContext(
        ContentPackSelection contentSelection,
        CharacterSelection characterSelection)
    {
        ArgumentNullException.ThrowIfNull(contentSelection);
        ArgumentNullException.ThrowIfNull(characterSelection);
        if (!CharacterCatalog.IsAvailable(characterSelection.Current, contentSelection))
        {
            throw new InvalidOperationException(
                $"Run character is not available: {characterSelection.Current.DisplayName}");
        }

        ContentSelection = contentSelection;
        CharacterSelection = characterSelection;
    }
}
