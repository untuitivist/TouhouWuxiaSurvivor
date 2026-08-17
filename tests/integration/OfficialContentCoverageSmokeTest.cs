using Godot;
using TouhouWuxiaSurvivor.Actors.Enemies;
using TouhouWuxiaSurvivor.Content;
using TouhouWuxiaSurvivor.Content.Characters;
using TouhouWuxiaSurvivor.Gameplay.SpellCards.Definitions;
using TouhouWuxiaSurvivor.World.Biomes;
using TouhouWuxiaSurvivor.World.Official;
using TouhouWuxiaSurvivor.World.Structures;

namespace TouhouWuxiaSurvivor.Tests.Integration;

/// <summary>
/// 验证 TH01 至 TH20 的迁移库存仍完整注册，并保持运行时生成与本体隔离而不误报玩法完成。
/// </summary>
public partial class OfficialContentCoverageSmokeTest : Node
{
    private const ulong Seed = 20260728;

    /// <summary>
    /// 逐作检查五类内容契约，并确认纯本体采样不会返回任何正作地区。
    /// </summary>
    public override void _Ready()
    {
        try
        {
            Require(ContentPackCatalog.All.Count == 20, "Manifest catalog must contain TH01-TH20.");
            Require(OfficialWorldContentCatalog.All.Count == 60,
                "World catalog must contain three regions for every game.");
            Require(SpellCardCatalog.All.Count >= ContentPackCatalog.All.Count * 2,
                "Spell catalog must contain at least two cards for every official work.");
            foreach (ContentPackDefinition pack in ContentPackCatalog.All)
            {
                VerifyPackage(pack);
            }

            VerifyCombinedSelection();
            VerifyBaseIsolation();
            GD.Print("Official content coverage smoke test passed.");
            GetTree().Quit();
        }
        catch (Exception exception)
        {
            GD.PushError(exception.ToString());
            GetTree().Quit(1);
        }
    }

    /// <summary>
    /// 同时启用二十作并在大范围内确认全部地区和结构都能共存，而不是后选内容覆盖前项。
    /// </summary>
    private static void VerifyCombinedSelection()
    {
        var selection = new ContentPackSelection(
            ContentPackCatalog.All.Select(pack => pack.Id));
        var biomes = new BiomeSelector(Seed, selection);
        var foundBiomes = new HashSet<BiomeId>();
        for (long y = -6000; y <= 6000; y += 43)
        {
            for (long x = -6000; x <= 6000; x += 43)
            {
                BiomeId biome = biomes.Select(x, y);
                if (OfficialWorldContentCatalog.TryGet(biome, out _))
                {
                    foundBiomes.Add(biome);
                }
            }
        }

        Require(OfficialWorldContentCatalog.All.All(item => foundBiomes.Contains(item.Biome)),
            "Combined selection did not distribute all official biomes.");
        var structures = new StructureLocator(Seed, biomes);
        HashSet<StructureId> foundStructures = structures
            .FindInBounds(-10000, -10000, 10000, 10000)
            .Select(item => item.Id)
            .ToHashSet();
        Require(OfficialWorldContentCatalog.All.All(item => foundStructures.Contains(item.Structure)),
            "Combined selection did not distribute all official structures.");
    }

    /// <summary>
    /// 核对单作清单和运行目录，并在确定性大范围内找到对应地区与结构实例。
    /// </summary>
    private static void VerifyPackage(ContentPackDefinition pack)
    {
        ContentPackStatus expectedStatus = pack.Id == ContentPackIds.EmbodimentOfScarletDevil
            ? ContentPackStatus.Development
            : ContentPackStatus.Inventory;
        Require(pack.Status == expectedStatus && pack.Selectable,
            $"Package lifecycle status or existing selection access is inaccurate: {pack.Id}");
        IReadOnlyList<OfficialWorldContentDefinition> worlds =
            OfficialWorldContentCatalog.GetByPack(pack.Id);
        Require(worlds.Count == 3, $"Package must have exactly three runtime regions: {pack.Id}");
        Require(pack.Additions.Count(item => item.Category == "地区") == 3 &&
            pack.Additions.Count(item => item.Category == "结构") == 3 &&
            pack.Additions.Count(item => item.Category == "敌人") == 3,
            $"Package must declare exactly three regions, structures and enemies: {pack.Id}");
        Require(worlds.All(world => HasAddition(pack, "地区", world.BiomeName) &&
                HasAddition(pack, "结构", world.StructureName) &&
                HasAddition(pack, "敌人", world.EnemyName)),
            $"Manifest additions do not match all runtime regions: {pack.Id}");

        var selection = new ContentPackSelection([pack.Id]);
        VerifyCharacters(pack, selection);
        VerifySpellCards(pack);
        var biomes = new BiomeSelector(Seed, selection);
        var structures = new StructureLocator(Seed, biomes);
        HashSet<StructureId> generatedStructures = structures
            .FindInBounds(-5000, -5000, 5000, 5000)
            .Select(item => item.Id)
            .ToHashSet();
        foreach (OfficialWorldContentDefinition world in worlds)
        {
            Require(FindBiome(biomes, world.Biome), $"Biome did not generate: {pack.Id}");
            Require(generatedStructures.Contains(world.Structure),
                $"Structure did not generate: {pack.Id}/{world.Structure}");
            Require(EnemyCatalog.All.Any(enemy => enemy.RequiredContentPack == pack.Id &&
                enemy.DisplayName == world.EnemyName && enemy.CanSpawnIn(world.Biome)),
                $"Enemy did not register for its biome: {pack.Id}/{world.EnemyName}");
        }
    }

    /// <summary>
    /// 逐名回查作品清单角色，确认规范角色定义保留该来源，并同时具备可玩与 Boss 两套身份。
    /// </summary>
    private static void VerifyCharacters(
        ContentPackDefinition pack,
        ContentPackSelection selection)
    {
        ContentAddition[] additions = pack.Additions
            .Where(item => item.Category == "角色").ToArray();
        Require(additions.Length > 0, $"Package has no character additions: {pack.Id}");
        IReadOnlySet<string> availableIds = CharacterCatalog.GetAvailable(selection)
            .Select(character => character.CharacterId).ToHashSet(StringComparer.Ordinal);
        foreach (ContentAddition addition in additions)
        {
            CharacterDefinition? character = CharacterCatalog.FindByDisplayName(addition.Name);
            Require(character is not null && character.IsPlayable && character.IsBoss &&
                character.AvailableSourcePackIds.Contains(pack.Id, StringComparer.Ordinal) &&
                availableIds.Contains(character.CharacterId),
                $"Manifest character is not a playable and boss-capable runtime definition: " +
                $"{pack.Id}/{addition.Name}");
        }
    }

    /// <summary>
    /// 核对每部作品至少两张结构化符卡、选择页名称投影和合法角色归属，防止只做文字清单而无运行定义。
    /// </summary>
    private static void VerifySpellCards(ContentPackDefinition pack)
    {
        SpellCardDefinition[] cards = SpellCardCatalog.All
            .Where(card => card.SourcePackId == pack.Id).ToArray();
        HashSet<string> manifestNames = pack.Additions
            .Where(item => item.Category == "符卡")
            .Select(item => item.Name).ToHashSet(StringComparer.Ordinal);
        Require(cards.Length >= 2 && manifestNames.Count >= 2,
            $"Package must provide at least two spell cards: {pack.Id}");
        foreach (SpellCardDefinition card in cards)
        {
            CharacterDefinition? owner = CharacterCatalog.FindById(card.OwnerCharacterId);
            Require(manifestNames.Contains(card.FullName) && owner is not null &&
                owner.IsPlayable && owner.IsBoss,
                $"Spell card is missing its manifest projection or character owner: " +
                $"{pack.Id}/{card.FullName}");
        }
    }

    /// <summary>
    /// 以固定步长扫描足够大的坐标范围，确认目标正作地区实际进入世界分布。
    /// </summary>
    private static bool FindBiome(BiomeSelector selector, BiomeId expected)
    {
        for (long y = -3000; y <= 3000; y += 53)
        {
            for (long x = -3000; x <= 3000; x += 53)
            {
                if (selector.Select(x, y) == expected)
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// 在纯本体的大范围样本中拒绝任何正作目录群系，确保未勾选内容不会泄漏。
    /// </summary>
    private static void VerifyBaseIsolation()
    {
        var selector = new BiomeSelector(Seed, ContentPackSelection.BaseOnly);
        for (long y = -2400; y <= 2400; y += 71)
        {
            for (long x = -2400; x <= 2400; x += 71)
            {
                Require(!OfficialWorldContentCatalog.TryGet(selector.Select(x, y), out _),
                    "Base-only world leaked an official-game biome.");
            }
        }
    }

    /// <summary>
    /// 判断清单是否在指定中文分类下登记了与运行目录完全一致的名称。
    /// </summary>
    private static bool HasAddition(ContentPackDefinition pack, string category, string name) =>
        pack.Additions.Any(item => item.Category == category && item.Name == name);

    /// <summary>
    /// 将覆盖契约失败转换为带有明确原因的测试异常。
    /// </summary>
    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
