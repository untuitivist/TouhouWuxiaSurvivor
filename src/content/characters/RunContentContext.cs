namespace TouhouWuxiaSurvivor.Content.Characters;

/// <summary>
/// 将本局启用内容与自机选择冻结为同一上下文，供世界、成长和 Boss 系统共享。
/// </summary>
public sealed class RunContentContext
{
    public const string RuleSetId = "base-survival";
    public const string RuleSetVersion = "1.0.0";
    public ContentPackSelection ContentSelection { get; }
    public CharacterSelection CharacterSelection { get; }
    public long WorldSeed { get; }
    public IReadOnlyList<ActiveContentPackIdentity> ActiveContentPacks { get; }
    public string RegistryFingerprint { get; }
    public string RunFingerprint { get; }

    /// <summary>
    /// 构造一致的局内内容快照，并拒绝与启用内容不相容的角色组合。
    /// </summary>
    public RunContentContext(
        ContentPackSelection contentSelection,
        CharacterSelection characterSelection,
        long worldSeed = 0)
    {
        ArgumentNullException.ThrowIfNull(contentSelection);
        ArgumentNullException.ThrowIfNull(characterSelection);
        IReadOnlyList<ContentPackDefinition> activeDefinitions =
            ContentPackCatalog.ResolveActive(contentSelection);
        if (!CharacterCatalog.IsAvailable(characterSelection.Current, contentSelection))
        {
            throw new InvalidOperationException(
                $"Run character is not available: {characterSelection.Current.DisplayName}");
        }

        ContentSelection = contentSelection;
        CharacterSelection = characterSelection;
        WorldSeed = worldSeed;
        ActiveContentPackIdentity[] activeIdentities = activeDefinitions
            .Select(definition => new ActiveContentPackIdentity(definition))
            .ToArray();
        ActiveContentPacks = Array.AsReadOnly(activeIdentities);
        RegistryFingerprint = ContentFingerprint.HashParts(
            ActiveContentPacks.Select(pack => pack.ToFingerprintMaterial()));
        RunFingerprint = ContentFingerprint.HashParts(
        [
            RuleSetId,
            RuleSetVersion,
            WorldSeed.ToString(System.Globalization.CultureInfo.InvariantCulture),
            CharacterSelection.CharacterId,
            RegistryFingerprint,
        ]);
    }
}
