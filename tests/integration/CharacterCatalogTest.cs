using Godot;
using TouhouWuxiaSurvivor.Content;
using TouhouWuxiaSurvivor.Content.Characters;

namespace TouhouWuxiaSurvivor.Tests.Integration;

/// <summary>
/// 验证全部清单角色的规范身份、内容隔离、自机选择与 Boss 排除契约。
/// </summary>
public partial class CharacterCatalogTest : Node
{
    /// <summary>
    /// 执行角色目录的完整契约测试，并用进程退出码向自动测试报告结果。
    /// </summary>
    public override void _Ready()
    {
        try
        {
            VerifyEveryManifestCharacterResolves();
            VerifyStableUniqueIdentity();
            VerifySelectionBoundaries();
            VerifyDualRoleProfiles();
            VerifyBossCandidateExclusion();
            GD.Print("Character catalog test passed.");
            GetTree().Quit();
        }
        catch (Exception exception)
        {
            GD.PushError(exception.ToString());
            GetTree().Quit(1);
        }
        finally
        {
            CharacterSelectionService.ResetToDefault();
        }
    }

    /// <summary>
    /// 逐项核对本体和二十个正作清单中的角色都能解析，并记录其实际来源包。
    /// </summary>
    private static void VerifyEveryManifestCharacterResolves()
    {
        IEnumerable<ContentPackDefinition> packs = [ContentPackCatalog.Base, .. ContentPackCatalog.All];
        int manifestEntryCount = 0;
        foreach (ContentPackDefinition pack in packs)
        {
            foreach (ContentAddition addition in pack.Additions.Where(item => item.Category == "角色"))
            {
                manifestEntryCount++;
                CharacterDefinition character = CharacterCatalog.FindByDisplayName(addition.Name) ??
                    throw new InvalidOperationException(
                        $"Manifest character did not resolve: {addition.Name}");
                Require(character.AvailableSourcePackIds.Contains(pack.Id, StringComparer.Ordinal),
                    $"Character did not retain source package: {pack.Id}/{addition.Name}");
            }
        }

        Require(manifestEntryCount == CharacterCatalog.All.Count + 1,
            "Only the repeated Mima manifest entry should merge into a canonical identity.");
        CharacterDefinition mima = CharacterCatalog.GetRequiredByDisplayName("魅魔");
        Require(mima.AvailableSourcePackIds.SequenceEqual(
            [ContentPackIds.HighlyResponsiveToPrayers, ContentPackIds.StoryOfEasternWonderland]),
            "Mima did not merge TH01 and TH02 into one ordered source list.");
    }

    /// <summary>
    /// 确认所有角色 ID 唯一且重复读取不会变化，默认身份固定为本体博丽灵梦。
    /// </summary>
    private static void VerifyStableUniqueIdentity()
    {
        string[] firstRead = CharacterCatalog.All.Select(character => character.CharacterId).ToArray();
        string[] secondRead = CharacterCatalog.All.Select(character => character.CharacterId).ToArray();
        Require(firstRead.Distinct(StringComparer.Ordinal).Count() == firstRead.Length,
            "Character ids are not unique.");
        Require(firstRead.SequenceEqual(secondRead), "Character catalog order or ids are unstable.");
        Require(CharacterCatalog.Default.DisplayName == "博丽灵梦" &&
            CharacterCatalog.Default.SourcePackId == ContentPackCatalog.Base.Id,
            "Default character is not base Reimu Hakurei.");
        Require(CharacterCatalog.Default.CharacterId == "character_base_00",
            "Reimu id no longer matches her locked first manifest address.");
        Require(CharacterCatalog.GetRequiredByDisplayName("魅魔").CharacterId ==
            "character_th01_hrtp_04",
            "Mima id no longer matches her locked first manifest address.");
        CharacterSelectionService.ResetToDefault();
        Require(CharacterSelectionService.Current.CharacterId == CharacterCatalog.Default.CharacterId,
            "Selection service did not default to Reimu Hakurei.");
    }

    /// <summary>
    /// 确认禁用正作角色不可选择，启用来源后可选择，跨作角色启用任一来源均有效。
    /// </summary>
    private static void VerifySelectionBoundaries()
    {
        CharacterDefinition remilia = CharacterCatalog.GetRequiredByDisplayName("蕾米莉亚·斯卡蕾特");
        bool rejected = false;
        try
        {
            CharacterSelectionService.Apply(remilia.CharacterId, ContentPackSelection.BaseOnly);
        }
        catch (InvalidOperationException)
        {
            rejected = true;
        }

        Require(rejected, "A disabled-package character was accepted.");
        var eosd = new ContentPackSelection([ContentPackIds.EmbodimentOfScarletDevil]);
        CharacterSelectionService.Apply(remilia.CharacterId, eosd);
        Require(CharacterSelectionService.Current.CharacterId == remilia.CharacterId,
            "An enabled-package character could not be selected.");

        CharacterDefinition mima = CharacterCatalog.GetRequiredByDisplayName("魅魔");
        var soew = new ContentPackSelection([ContentPackIds.StoryOfEasternWonderland]);
        CharacterSelectionService.Apply(mima.CharacterId, soew);
        Require(CharacterSelectionService.Current.CharacterId == mima.CharacterId,
            "A canonical character was not enabled by its secondary source package.");
        _ = new RunContentContext(soew, CharacterSelectionService.Current);
    }

    /// <summary>
    /// 确认每个登记角色都同时具备可游玩和 Boss 标志，以及完整的两套正数属性。
    /// </summary>
    private static void VerifyDualRoleProfiles()
    {
        foreach (CharacterDefinition character in CharacterCatalog.All)
        {
            Require(character.IsPlayable && character.IsBoss,
                $"Character is missing a gameplay role: {character.DisplayName}");
            Require(character.PlayableProfile.MaxHealth > 0.0f &&
                character.PlayableProfile.MoveSpeedMultiplier > 0.0f &&
                character.PlayableProfile.AttackMultiplier > 0.0f,
                $"Playable profile is incomplete: {character.DisplayName}");
            Require(character.BossProfile.MaxHealth > 0.0f &&
                character.BossProfile.MoveSpeed > 0.0f &&
                character.BossProfile.ContactDamage > 0.0f &&
                character.BossProfile.CollisionRadius > 0.0f,
                $"Boss profile is incomplete: {character.DisplayName}");
        }
    }

    /// <summary>
    /// 确认候选仅来自启用内容、精确排除当前 ID，且只有灵梦时返回真正的空池。
    /// </summary>
    private static void VerifyBossCandidateExclusion()
    {
        CharacterDefinition reimu = CharacterCatalog.Default;
        Require(CharacterBossCatalog.GetCandidates(
            ContentPackSelection.BaseOnly, reimu.CharacterId).Count == 0,
            "Single-character boss pool incorrectly filled the player back in.");

        var eosd = new ContentPackSelection([ContentPackIds.EmbodimentOfScarletDevil]);
        CharacterDefinition remilia = CharacterCatalog.GetRequiredByDisplayName("蕾米莉亚·斯卡蕾特");
        IReadOnlyList<CharacterDefinition> candidates = CharacterBossCatalog.GetCandidates(
            eosd, new CharacterSelection(remilia));
        Require(candidates.Count == 7, "TH06 boss pool should contain Reimu plus six other characters.");
        Require(candidates.All(character => character.CharacterId != remilia.CharacterId),
            "Selected player character leaked into the boss pool.");
        Require(candidates.All(character => CharacterCatalog.IsAvailable(character, eosd)),
            "Boss pool contains a character from disabled content.");
    }

    /// <summary>
    /// 将角色目录契约失败转换为带有明确原因的测试异常。
    /// </summary>
    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
