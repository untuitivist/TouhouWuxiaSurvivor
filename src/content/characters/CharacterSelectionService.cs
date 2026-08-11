namespace TouhouWuxiaSurvivor.Content.Characters;

/// <summary>
/// 保存菜单为下一局选定的角色，并在应用时校验对应本体或正作内容是否已经启用。
/// </summary>
public static class CharacterSelectionService
{
    public static CharacterSelection Current { get; private set; } =
        new(CharacterCatalog.Default);

    /// <summary>
    /// 按稳定标识应用角色选择；未登记或属于禁用内容包的角色会被明确拒绝。
    /// </summary>
    public static void Apply(string characterId, ContentPackSelection contentSelection)
    {
        CharacterDefinition character = CharacterCatalog.GetRequired(characterId);
        if (!CharacterCatalog.IsAvailable(character, contentSelection))
        {
            throw new InvalidOperationException(
                $"Character is not available in the selected content: {character.DisplayName}");
        }

        Current = new CharacterSelection(character);
    }

    /// <summary>
    /// 将下一局角色恢复为本体博丽灵梦，供主菜单初始化和测试隔离状态使用。
    /// </summary>
    public static void ResetToDefault() => Current = new CharacterSelection(CharacterCatalog.Default);
}
