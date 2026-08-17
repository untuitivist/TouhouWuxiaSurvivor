using TouhouWuxiaSurvivor.Content;
using TouhouWuxiaSurvivor.Content.Characters;
using TouhouWuxiaSurvivor.World.Official;
using TouhouWuxiaSurvivor.World.Tiles;

namespace TouhouWuxiaSurvivor.Ui.Compendium;

/// <summary>从共享角色目录建立图鉴，展示自机、Boss、攻击节奏与本局身份互斥规则。</summary>
public static class CharacterCompendiumEntryFactory
{
    /// <summary>按清单来源逐作投影角色；同一角色跨作品登记时保留各内容包自己的素材身份。</summary>
    public static IReadOnlyList<CompendiumEntry> CreateAll()
    {
        var entries = new List<CompendiumEntry>();
        AddSource(entries, ContentPackCatalog.Base);
        foreach (ContentPackDefinition source in ContentPackCatalog.All)
        {
            AddSource(entries, source);
        }

        return entries;
    }

    /// <summary>把一个来源的角色清单连接到唯一运行时定义，缺少定义时立即报告内容错误。</summary>
    private static void AddSource(
        List<CompendiumEntry> entries,
        ContentPackDefinition source)
    {
        foreach (ContentAddition character in source.Additions.Where(item => item.Category == "角色"))
        {
            CharacterDefinition definition = CharacterCatalog.GetRequiredByDisplayName(character.Name);
            PlayableCharacterProfile player = definition.PlayableProfile;
            BossCharacterProfile boss = definition.BossProfile;
            string role = CharacterCombatRoleText.GetName(definition.CombatRole);
            string identity = $"{role} · 可选自机 · 可作为角色 Boss";
            entries.Add(new CompendiumEntry(
                CompendiumCategory.Character, character.Name,
                CompendiumSourceText.GetId(source), CompendiumSourceText.GetLabel(source),
                $"{identity} · {CharacterCombatRoleText.Describe(definition.CombatRole)}",
                [
                    new("玩法身份", identity, true),
                    new("定位说明", CharacterCombatRoleText.Describe(definition.CombatRole), true),
                    new("自机生命", player.MaxHealth.ToString("0")),
                    new("自机身法", $"×{player.MoveSpeedMultiplier:0.00}"),
                    new("自机攻势", $"×{player.AttackMultiplier:0.00}"),
                    new("攻击间隔", $"×{player.AttackIntervalMultiplier:0.00}"),
                    new("奥义周天", $"{player.UltimateIntervalSeconds:0.##} 秒"),
                    new("奥义承载", player.UltimateTargetCapacity.ToString()),
                    new("Boss 生命", boss.MaxHealth.ToString("0")),
                    new("Boss 身法", boss.MoveSpeed.ToString("0")),
                    new("Boss 触伤", boss.ContactDamage.ToString("0")),
                    new("互斥与素材", "选为本局自机时不进入本局 Boss 候选 · " +
                        CompendiumVisualProvenanceCatalog.Placeholder, true),
                ], GetPreviewTile(source), source.Number % 4));
        }
    }

    /// <summary>本体角色使用神社地表；可选包角色使用所属作品外围地区作为预览底色。</summary>
    private static TileId GetPreviewTile(ContentPackDefinition source) => source.Number == 0
        ? TileId.ShrineGrassBase
        : OfficialWorldContentCatalog.GetByPack(source.Id)[0].BaseTile;
}
