namespace TouhouWuxiaSurvivor.Content.Characters;

/// <summary>
/// 为跨作品共享角色选择本局实际启用的内容来源，防止规范角色身份泄漏未启用包的素材。
/// </summary>
public static class CharacterContentSourceResolver
{
    /// <summary>优先采用角色规范来源；其未启用时按冻结内容顺序选择角色的首个有效来源。</summary>
    public static string Resolve(CharacterDefinition character, RunContentContext context)
    {
        ArgumentNullException.ThrowIfNull(character);
        ArgumentNullException.ThrowIfNull(context);
        if (IsActive(character.SourcePackId, context) &&
            character.AvailableSourcePackIds.Contains(
                character.SourcePackId, StringComparer.Ordinal))
        {
            return character.SourcePackId;
        }

        foreach (ActiveContentPackIdentity active in context.ActiveContentPacks)
        {
            if (character.AvailableSourcePackIds.Contains(active.Id, StringComparer.Ordinal))
            {
                return active.Id;
            }
        }

        throw new InvalidOperationException(
            $"Character has no active content source: {character.CharacterId}");
    }

    /// <summary>判断包身份是否存在于开局冻结快照，不重新读取可变的菜单选择。</summary>
    private static bool IsActive(string sourcePackId, RunContentContext context) =>
        context.ActiveContentPacks.Any(pack => string.Equals(
            pack.Id, sourcePackId, StringComparison.Ordinal));
}
