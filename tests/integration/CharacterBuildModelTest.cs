using Godot;
using TouhouWuxiaSurvivor.Content;
using TouhouWuxiaSurvivor.Content.Characters;
using TouhouWuxiaSurvivor.Gameplay.Progression.Definitions;
using TouhouWuxiaSurvivor.Gameplay.Progression.Runtime;
using TouhouWuxiaSurvivor.Gameplay.SpellCards.Definitions;
using TouhouWuxiaSurvivor.Ui.Stats.Build;

namespace TouhouWuxiaSurvivor.Tests.Integration;

/// <summary>
/// 验证 E 键构筑可视化模型的亲和、节点、关系、锁因、符卡触发和稳定查询契约。
/// </summary>
public partial class CharacterBuildModelTest : Node
{
    /// <summary>
    /// 依次覆盖空构筑、升重、特化、无尽前置、内容开关与自动符卡，并以退出码报告回归。
    /// </summary>
    public override void _Ready()
    {
        try
        {
            VerifyEmptyBuild();
            VerifyLearnedAffinityAndAdvance();
            VerifySpecializationStates();
            VerifyEndlessPrerequisite();
            CharacterBuildProjectionAssertions.Verify();
            VerifySpellCardProjection();
            VerifyRoleAndSlotProjection();
            VerifyStableQuery();
            GD.Print("Character build model test passed.");
            GetTree().Quit();
        }
        catch (Exception exception)
        {
            GD.PushError(exception.ToString());
            GetTree().Quit(1);
        }
    }

    /// <summary>
    /// 确认空构筑包含完整目录与十二项特化，五类亲和均为零且内容包符卡显示明确锁因。
    /// </summary>
    private static void VerifyEmptyBuild()
    {
        CharacterBuildViewModel model = CharacterBuildViewModelFactory.Create(
            new RunBuildState(), ContentPackSelection.BaseOnly, 1);
        int specializationCount = RunUpgradeCatalog.All.Sum(item =>
            item.Specializations.Count);
        Require(model.Nodes.Count == RunUpgradeCatalog.All.Count + specializationCount,
            "Build model omitted catalog or specialization nodes.");
        Require(model.Affinities.Count == 5 && model.Affinities.All(item =>
            item.Value == 0 && item.Share == 0.0f && !item.IsDominant),
            "Empty build reported a false affinity.");
        Require(model.Nodes.Any(item =>
            item.Category == RunUpgradeCategory.SpellCard &&
            item.State == CharacterBuildNodeState.LockedContent &&
            !item.CanAdvance &&
            item.LockReason.Contains("未启用", StringComparison.Ordinal)),
            "Disabled spell-card content has no explicit lock reason.");
    }

    /// <summary>
    /// 确认一重武学同时表现为已习得且可继续修炼，并只把实际选择计入对应亲和。
    /// </summary>
    private static void VerifyLearnedAffinityAndAdvance()
    {
        var build = new RunBuildState();
        build.Apply(Required("needle_damage"));
        CharacterBuildViewModel model = CharacterBuildViewModelFactory.Create(
            build, ContentPackSelection.BaseOnly, 2);
        CharacterBuildNodeView needle = Node(model, "needle_damage");
        Require(needle.State == CharacterBuildNodeState.Learned &&
            needle.CurrentRank == 1 && needle.CanAdvance,
            "Learned multi-rank art lost its advancement state.");
        Require(needle.CurrentEffectText.Contains("1/5 重", StringComparison.Ordinal) &&
            needle.NextEffectText.Contains("2/5 重", StringComparison.Ordinal),
            "Finite art omitted current-rank or next-rank meaning.");
        Require(model.Affinities.Single(item =>
                item.Affinity == RunUpgradeAffinity.Force).Value == 1 &&
            model.Affinities.Single(item =>
                item.Affinity == RunUpgradeAffinity.Precision).Value == 1,
            "Chosen art did not project its two affinities.");
    }

    /// <summary>
    /// 确认特化依次经历缺基础、缺境界、可选、已圆满与同门互斥五个可解释阶段。
    /// </summary>
    private static void VerifySpecializationStates()
    {
        var build = new RunBuildState();
        RunUpgradeDefinition needle = Required("needle_damage");
        string firstId = needle.Specializations[0].Id;
        string secondId = needle.Specializations[1].Id;
        CharacterBuildViewModel early = CharacterBuildViewModelFactory.Create(
            build, ContentPackSelection.BaseOnly, 1);
        Require(Node(early, firstId).State == CharacterBuildNodeState.LockedPrerequisite,
            "Untrained specialization did not report its base-rank gate.");
        for (int rank = 0; rank < 3; rank++)
        {
            build.Apply(needle);
        }

        CharacterBuildViewModel levelGate = CharacterBuildViewModelFactory.Create(
            build, ContentPackSelection.BaseOnly, 7);
        Require(Node(levelGate, firstId).State == CharacterBuildNodeState.LockedLevel,
            "Specialization did not expose its run-level gate.");
        CharacterBuildViewModel available = CharacterBuildViewModelFactory.Create(
            build, ContentPackSelection.BaseOnly, 8);
        Require(Node(available, firstId).CanAdvance && Node(available, secondId).CanAdvance,
            "Eligible specialization branches are not interactive.");
        build.ApplySpecialization(needle, needle.Specializations[0], 8);
        CharacterBuildViewModel chosen = CharacterBuildViewModelFactory.Create(
            build, ContentPackSelection.BaseOnly, 8);
        Require(Node(chosen, firstId).State == CharacterBuildNodeState.Mastered &&
            Node(chosen, secondId).State == CharacterBuildNodeState.LockedExclusion,
            "Chosen and sibling specializations do not expose final branch states.");
    }

    /// <summary>
    /// 确认无尽延续在基础修行未满时显示前置重数，练满后进入可继续修炼集合。
    /// </summary>
    private static void VerifyEndlessPrerequisite()
    {
        var build = new RunBuildState();
        RunUpgradeDefinition needle = Required("needle_damage");
        CharacterBuildViewModel locked = CharacterBuildViewModelFactory.Create(
            build, ContentPackSelection.BaseOnly, 1);
        CharacterBuildNodeView endless = Node(locked, "endless_damage");
        Require(endless.State == CharacterBuildNodeState.LockedPrerequisite &&
            endless.LockReason.Contains("5重", StringComparison.Ordinal),
            "Endless art omitted its five-rank prerequisite.");
        for (int rank = 0; rank < needle.MaxRank; rank++)
        {
            build.Apply(needle);
        }

        CharacterBuildViewModel open = CharacterBuildViewModelFactory.Create(
            build, ContentPackSelection.BaseOnly, 9);
        Require(Node(open, "endless_damage").CanAdvance,
            "Mastered base art did not unlock endless continuation.");
        CharacterBuildNodeView openEndless = Node(open, "endless_damage");
        Require(openEndless.CurrentEffectText.Contains("尚未修习", StringComparison.Ordinal) &&
            openEndless.NextEffectText.Contains("第 1 重", StringComparison.Ordinal),
            "Endless art omitted its unbounded next-rank meaning.");
    }

    /// <summary>
    /// 启用一张真实符卡所属作品并满足前置，确认节点展示自动触发与属性系数且关系边已满足。
    /// </summary>
    private static void VerifySpellCardProjection()
    {
        RunUpgradeDefinition spell = RunUpgradeCatalog.All.First(item =>
            item.Category == RunUpgradeCategory.SpellCard);
        ContentPackSelection content = spell.RequiredContentPack is null
            ? ContentPackSelection.BaseOnly
            : new ContentPackSelection([spell.RequiredContentPack]);
        var build = new RunBuildState();
        foreach (RunUpgradeRequirement requirement in spell.Requirements)
        {
            RunUpgradeDefinition prerequisite = Required(requirement.RequiredUpgradeId);
            for (int rank = 0; rank < requirement.MinimumRank; rank++)
            {
                build.Apply(prerequisite);
            }
        }

        CharacterBuildViewModel model = CharacterBuildViewModelFactory.Create(build, content, 9);
        CharacterBuildNodeView node = Node(model, spell.Id);
        Require(node.CanAdvance && node.TriggerText.Contains("响应", StringComparison.Ordinal) &&
            node.TriggerText.Contains("周天", StringComparison.Ordinal) &&
            node.TriggerText.Contains("攻势", StringComparison.Ordinal),
            "Spell-card node omitted activation or scaling data.");
        Require(Enum.GetValues<SpellCardGeometryKind>().Any(kind =>
                node.TriggerText.Contains(SpellCardGeometryText.GetName(kind),
                    StringComparison.Ordinal)),
            "Spell-card node omitted its runtime geometry strategy.");
        Require(model.Relations.Any(item => item.ToNodeId == spell.Id &&
            item.Kind == CharacterBuildRelationKind.Requirement && item.IsSatisfied),
            "Spell-card prerequisite relation was not projected as satisfied.");
    }

    /// <summary>
    /// 悟得一张正式奥义后确认角色定位、四攻二护持分类与实时倍率语义同时进入模型。
    /// </summary>
    private static void VerifyRoleAndSlotProjection()
    {
        RunUpgradeDefinition spell = RunUpgradeCatalog.All.First(item =>
            item.Category == RunUpgradeCategory.SpellCard &&
            item.RequiredContentPack is null);
        var build = new RunBuildState();
        foreach (RunUpgradeRequirement requirement in spell.Requirements)
        {
            RunUpgradeDefinition prerequisite = Required(requirement.RequiredUpgradeId);
            for (int rank = 0; rank < requirement.MinimumRank; rank++)
            {
                build.Apply(prerequisite);
            }
        }

        Require(build.Apply(spell), "Could not prepare a learned base spell card.");
        CharacterBuildViewModel model = CharacterBuildViewModelFactory.Create(
            build, ContentPackSelection.BaseOnly, 9, CharacterCombatRole.Formation);
        CharacterBuildNodeView node = Node(model, spell.Id);
        Require(model.CombatRoleName == "阵法" &&
            model.OffensiveSpellCount + model.SupportSpellCount == 1,
            "Build model omitted role or shared spell-slot occupancy.");
        Require(node.TriggerText.Contains("实时", StringComparison.Ordinal) &&
            (node.TriggerText.Contains("主攻 4槽", StringComparison.Ordinal) ||
                node.TriggerText.Contains("护持 2槽", StringComparison.Ordinal)),
            "Spell node omitted slot class or live attribute scaling semantics.");
    }

    /// <summary>
    /// 确认筛选与中文搜索采用稳定字段，重复查询不会因集合枚举顺序产生节点跳动。
    /// </summary>
    private static void VerifyStableQuery()
    {
        var build = new RunBuildState();
        build.Apply(Required("needle_damage"));
        CharacterBuildViewModel model = CharacterBuildViewModelFactory.Create(
            build, ContentPackSelection.BaseOnly, 2);
        string[] first = CharacterBuildQuery.Apply(model.Nodes,
            CharacterBuildFilter.Available, CharacterBuildSort.Name, "针")
            .Select(item => item.Id).ToArray();
        string[] second = CharacterBuildQuery.Apply(model.Nodes,
            CharacterBuildFilter.Available, CharacterBuildSort.Name, "针")
            .Select(item => item.Id).ToArray();
        Require(first.Length > 0 && first.SequenceEqual(second),
            "Interactive build query is empty or unstable.");
    }

    /// <summary>
    /// 按稳定 ID 返回正式升级定义，目录缺项时立即给出可定位的测试异常。
    /// </summary>
    private static RunUpgradeDefinition Required(string id) =>
        RunUpgradeCatalog.FindById(id) ??
        throw new InvalidOperationException($"Missing upgrade: {id}");

    /// <summary>
    /// 从模型按稳定 ID 返回唯一节点，防止测试依赖目录位置或中文名称。
    /// </summary>
    private static CharacterBuildNodeView Node(CharacterBuildViewModel model, string id) =>
        model.Nodes.Single(item => item.Id == id);

    /// <summary>
    /// 将任一数据契约失败转换为包含具体原因的测试异常。
    /// </summary>
    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
