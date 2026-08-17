using Godot;
using TouhouWuxiaSurvivor.Actors.Enemies;
using TouhouWuxiaSurvivor.Gameplay.Progression.Definitions;
using TouhouWuxiaSurvivor.Gameplay.SpellCards.Definitions;
using TouhouWuxiaSurvivor.Ui.Compendium;
using TouhouWuxiaSurvivor.World.Structures;

namespace TouhouWuxiaSurvivor.Tests.Integration;

/// <summary>验证图鉴六类内容逐项来自正式目录，并同步弹型姿态、AI、结构和素材归属。</summary>
public partial class CompendiumRuntimeSyncTest : Node
{
    /// <summary>执行目录数量、逐项字段和代理来源契约，失败时返回非零退出码。</summary>
    public override void _Ready()
    {
        try
        {
            VerifyBuildCatalog();
            VerifySpellCatalog();
            VerifyEnemyCatalog();
            VerifyWorldAndCharacterCatalogs();
            GD.Print("Compendium runtime sync test passed.");
            GetTree().Quit();
        }
        catch (Exception exception)
        {
            GD.PushError(exception.ToString());
            GetTree().Quit(1);
        }
    }

    /// <summary>确认每项有限、无尽修行和行为特化恰好拥有一个武学条目。</summary>
    private static void VerifyBuildCatalog()
    {
        RunUpgradeDefinition[] upgrades = RunUpgradeCatalog.All.Where(definition =>
            definition.Category != RunUpgradeCategory.SpellCard).ToArray();
        int expected = upgrades.Length + upgrades.Sum(definition =>
            definition.Specializations.Count);
        CompendiumEntry[] entries = Entries(CompendiumCategory.Build);
        Require(entries.Length == expected,
            $"Build compendium count drifted from runtime catalog: {entries.Length}/{expected}.");
        foreach (RunUpgradeDefinition upgrade in upgrades)
        {
            CompendiumEntry entry = RequireSingle(entries, upgrade.DisplayName);
            Require(entry.Details.Contains(upgrade.EffectText, StringComparison.Ordinal) &&
                entry.Details.Contains(upgrade.IsRepeatable ? "无上限" : $"0/{upgrade.MaxRank}",
                    StringComparison.Ordinal),
                $"Build entry omitted rank or effect: {upgrade.Id}.");
            foreach (RunUpgradeSpecialization specialization in upgrade.Specializations)
            {
                CompendiumEntry branch = RequireSingle(entries, specialization.DisplayName);
                Require(branch.Details.Contains(specialization.EffectText, StringComparison.Ordinal) &&
                    branch.Details.Contains("当前无互斥", StringComparison.Ordinal),
                    $"Specialization entry drifted from runtime behavior: {specialization.Id}.");
            }
        }
    }

    /// <summary>逐张奥义核对弹型中文名、实时姿态、自动规则及最终素材来源。</summary>
    private static void VerifySpellCatalog()
    {
        CompendiumEntry[] entries = Entries(CompendiumCategory.SpellCard);
        Require(entries.Length == SpellCardCatalog.All.Count,
            "Spell-card compendium count drifted from the runtime catalog.");
        foreach (SpellCardDefinition spell in SpellCardCatalog.All)
        {
            CompendiumEntry entry = RequireSingle(entries, spell.FullName, spell.SourcePackId);
            Require(entry.Details.Contains(
                    SpellBulletStyleSemantics.GetDisplayName(spell.BulletStyleKind),
                    StringComparison.Ordinal) &&
                entry.Details.Contains(
                    SpellBulletStyleSemantics.DescribePose(spell.BulletStyleKind),
                    StringComparison.Ordinal) &&
                entry.Details.Contains(SpellCardPatternText.GetName(spell.Pattern.Kind),
                    StringComparison.Ordinal) &&
                entry.Details.Contains(spell.Pattern.OriginalReference,
                    StringComparison.Ordinal) &&
                !entry.Details.Contains(CompendiumVisualProvenanceCatalog.Placeholder,
                    StringComparison.Ordinal),
                $"Spell entry omitted bullet semantics or provenance: {spell.Id}.");
        }

        CompendiumEntry fantasySeal = RequireSingle(entries, "灵符「梦想封印」", "base");
        Require(fantasySeal.VisualSourceName.Contains("审核代理 TH06",
                StringComparison.Ordinal) &&
            fantasySeal.Details.Contains("本体常驻奥义源自东方红魔乡",
                StringComparison.Ordinal),
            "Base proxy provenance is absent from the spell-card compendium.");
        CompendiumEntry scarlet = entries.First(entry => entry.SourceId == "th06_eosd");
        Require(scarlet.VisualSourceName.Contains("本包原生素材", StringComparison.Ordinal),
            "TH06 spell card did not report its native content-pack atlas.");
    }

    /// <summary>逐个敌人确认强度档、AI、弹幕与固定基础属性原则进入详情。</summary>
    private static void VerifyEnemyCatalog()
    {
        CompendiumEntry[] entries = Entries(CompendiumCategory.Enemy);
        Require(entries.Length == EnemyCatalog.All.Count,
            "Enemy compendium count drifted from the combat catalog.");
        foreach (EnemyDefinition enemy in EnemyCatalog.All)
        {
            CompendiumEntry entry = RequireSingle(entries, enemy.DisplayName,
                enemy.RequiredContentPack ?? "base");
            Require(entry.Details.Contains("强度类型", StringComparison.Ordinal) &&
                entry.Details.Contains("行动 AI", StringComparison.Ordinal) &&
                entry.Details.Contains("敌方弹幕", StringComparison.Ordinal) &&
                entry.Details.Contains("不随压力档位提升", StringComparison.Ordinal),
                $"Enemy entry omitted current runtime semantics: {enemy.DisplayName}.");
        }
    }

    /// <summary>锁定结构空间配置及角色攻击节奏均已进入图鉴，而非只保留旧摘要。</summary>
    private static void VerifyWorldAndCharacterCatalogs()
    {
        CompendiumEntry[] structures = Entries(CompendiumCategory.Structure);
        Require(structures.Length == StructureCatalog.All.Count && structures.All(entry =>
                entry.Details.Contains("候选间距", StringComparison.Ordinal) &&
                entry.Details.Contains("地图发现", StringComparison.Ordinal)),
            "Structure compendium omitted placement or discovery rules.");
        CompendiumEntry[] characters = Entries(CompendiumCategory.Character);
        Require(characters.Length > 0 && characters.All(entry =>
                entry.Details.Contains("攻击间隔", StringComparison.Ordinal) &&
                entry.Details.Contains("本局 Boss 候选", StringComparison.Ordinal)),
            "Character compendium omitted cadence or player/Boss exclusion semantics.");
    }

    /// <summary>按分类取得最终已补素材来源的不可变条目集合。</summary>
    private static CompendiumEntry[] Entries(CompendiumCategory category) =>
        CompendiumCatalog.All.Where(entry => entry.Category == category).ToArray();

    /// <summary>按来源与中文名查找唯一条目，重复或缺失都属于图鉴身份错误。</summary>
    private static CompendiumEntry RequireSingle(
        IEnumerable<CompendiumEntry> entries,
        string name,
        string? sourceId = null) => entries.Single(entry => entry.Name == name &&
            (sourceId is null || entry.SourceId == sourceId));

    /// <summary>把同步契约失败转换为带具体条目上下文的测试异常。</summary>
    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
