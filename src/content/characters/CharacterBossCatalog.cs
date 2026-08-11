namespace TouhouWuxiaSurvivor.Content.Characters;

/// <summary>
/// 从本局可用角色中产生 Boss 候选，并以稳定角色身份严格排除当前自机。
/// </summary>
public static class CharacterBossCatalog
{
    /// <summary>
    /// 返回启用内容中的全部 Boss 角色，绝不在候选为空时把当前自机重新填回。
    /// </summary>
    public static IReadOnlyList<CharacterDefinition> GetCandidates(
        ContentPackSelection selection,
        string excludedCharacterId) => CharacterCatalog.GetAvailable(selection)
            .Where(character => character.IsBoss && !string.Equals(
                character.CharacterId, excludedCharacterId, StringComparison.Ordinal))
            .ToArray();

    /// <summary>
    /// 使用完整角色选择快照查询 Boss 候选，避免调用方手工提取并可能传错身份。
    /// </summary>
    public static IReadOnlyList<CharacterDefinition> GetCandidates(
        ContentPackSelection selection,
        CharacterSelection excludedCharacter) =>
        GetCandidates(selection, excludedCharacter.CharacterId);
}
