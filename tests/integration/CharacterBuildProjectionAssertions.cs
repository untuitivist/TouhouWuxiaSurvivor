using TouhouWuxiaSurvivor.Content;
using TouhouWuxiaSurvivor.Gameplay.Progression.Definitions;
using TouhouWuxiaSurvivor.Gameplay.Progression.Runtime;
using TouhouWuxiaSurvivor.Ui.Stats.Build;

namespace TouhouWuxiaSurvivor.Tests.Integration;

/// <summary>
/// 集中审计 E 构筑页的六路有限、六路无尽、弹幕特化与四种视觉态，避免主场景测试超过行数上限。
/// </summary>
public static class CharacterBuildProjectionAssertions
{
    /// <summary>运行完整基础路线与状态投影审计，任一缺项都会抛出可定位的测试异常。</summary>
    public static void Verify()
    {
        CharacterBuildViewModel model = CharacterBuildViewModelFactory.Create(
            new RunBuildState(), ContentPackSelection.BaseOnly, 1);
        RunUpgradeDefinition[] finite = RunUpgradeCatalog.All.Where(item =>
            item.RequiredContentPack is null && !item.IsRepeatable &&
            item.Category != RunUpgradeCategory.SpellCard).ToArray();
        RunUpgradeDefinition[] endless = RunUpgradeCatalog.All.Where(item =>
            item.RequiredContentPack is null && item.IsRepeatable).ToArray();
        Require(finite.Length == 6 && endless.Length == 6,
            "Build projection no longer contains six finite and six endless base routes.");
        Require(finite.Concat(endless).Select(item => Node(model, item.Id)).All(node =>
            node.CurrentEffectText.StartsWith("当前：", StringComparison.Ordinal) &&
            (node.NextEffectText.StartsWith("下一重：", StringComparison.Ordinal) ||
                node.NextEffectText.StartsWith("后续：", StringComparison.Ordinal))),
            "A finite or endless route omitted current-rank or next-rank semantics.");
        VerifyBehaviorSpecializations(model, finite);
        VerifyFourVisualStates();
    }

    /// <summary>确认普通弹数量、贯穿、收束和中心螺旋作为行为特化显示。</summary>
    private static void VerifyBehaviorSpecializations(
        CharacterBuildViewModel model,
        IEnumerable<RunUpgradeDefinition> finite)
    {
        string[] ids = finite.SelectMany(item => item.Specializations)
            .Where(item => item.Effect is RunSpecializationEffect.OrdinaryProjectiles or
                RunSpecializationEffect.ProjectilePierce or
                RunSpecializationEffect.ConvergingOrdinary or
                RunSpecializationEffect.BarrageSpiralArms)
            .Select(item => item.Id).ToArray();
        Require(ids.Length >= 3 && ids.All(id =>
            Node(model, id).CategoryName == "弹幕特化" &&
            Node(model, id).NextEffectText.StartsWith("选择后：", StringComparison.Ordinal)),
            "Projectile behavior specializations are not distinguished from numeric traits.");
    }

    /// <summary>确认精确锁因在图谱上稳定归并为可达、已得、圆满与封锁四态。</summary>
    private static void VerifyFourVisualStates()
    {
        var build = new RunBuildState();
        RunUpgradeDefinition needle = Required("needle_damage");
        CharacterBuildNodeView available = Node(CharacterBuildViewModelFactory.Create(
            build, ContentPackSelection.BaseOnly, 1), needle.Id);
        build.Apply(needle);
        CharacterBuildNodeView learned = Node(CharacterBuildViewModelFactory.Create(
            build, ContentPackSelection.BaseOnly, 2), needle.Id);
        while (build.CanUpgrade(needle))
        {
            build.Apply(needle);
        }

        CharacterBuildViewModel masteredModel = CharacterBuildViewModelFactory.Create(
            build, ContentPackSelection.BaseOnly, 9);
        CharacterBuildNodeView mastered = Node(masteredModel, needle.Id);
        build.ApplySpecialization(needle, needle.Specializations[0], 9);
        CharacterBuildNodeView locked = Node(CharacterBuildViewModelFactory.Create(
            build, ContentPackSelection.BaseOnly, 9), "needle_rain");
        Require(CharacterBuildNodeStateText.GetMarker(available) == "+" &&
            CharacterBuildNodeStateText.GetMarker(learned).Contains("/", StringComparison.Ordinal) &&
            CharacterBuildNodeStateText.GetMarker(mastered) == "圆" &&
            CharacterBuildNodeStateText.GetMarker(locked) == "锁",
            "Build nodes no longer expose the four visual states.");
    }

    /// <summary>按稳定 ID 返回正式升级定义，目录缺项时立即失败。</summary>
    private static RunUpgradeDefinition Required(string id) =>
        RunUpgradeCatalog.FindById(id) ?? throw new InvalidOperationException(
            $"Missing upgrade: {id}");

    /// <summary>按稳定 ID 返回模型节点，避免测试依赖目录位置。</summary>
    private static CharacterBuildNodeView Node(CharacterBuildViewModel model, string id) =>
        model.Nodes.Single(item => item.Id == id);

    /// <summary>将审计失败转换为包含具体原因的测试异常。</summary>
    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
