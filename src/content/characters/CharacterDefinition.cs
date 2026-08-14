namespace TouhouWuxiaSurvivor.Content.Characters;

/// <summary>
/// 描述一个规范化角色身份，并同时携带自机与 Boss 两套独立基础属性。
/// </summary>
public sealed class CharacterDefinition
{
    public string CharacterId { get; }
    public string SourcePackId { get; }
    public int SourceNumber { get; }
    public string DisplayName { get; }
    public CharacterCombatRole CombatRole { get; }
    public IReadOnlyList<string> AvailableSourcePackIds { get; }
    public bool IsPlayable { get; }
    public bool IsBoss { get; }
    public PlayableCharacterProfile PlayableProfile { get; }
    public BossCharacterProfile BossProfile { get; }

    /// <summary>
    /// 构造不可变角色定义，并复制来源包列表以隔离目录构建阶段的临时集合。
    /// </summary>
    public CharacterDefinition(
        string characterId,
        string sourcePackId,
        int sourceNumber,
        string displayName,
        CharacterCombatRole combatRole,
        IEnumerable<string> availableSourcePackIds,
        PlayableCharacterProfile playableProfile,
        BossCharacterProfile bossProfile)
    {
        CharacterId = characterId;
        SourcePackId = sourcePackId;
        SourceNumber = sourceNumber;
        DisplayName = displayName;
        CombatRole = combatRole;
        AvailableSourcePackIds = availableSourcePackIds
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        IsPlayable = true;
        IsBoss = true;
        PlayableProfile = playableProfile;
        BossProfile = bossProfile;
    }
}
