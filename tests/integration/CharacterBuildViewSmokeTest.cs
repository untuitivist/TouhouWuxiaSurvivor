using Godot;
using TouhouWuxiaSurvivor.Content;
using TouhouWuxiaSurvivor.Gameplay.Progression.Definitions;
using TouhouWuxiaSurvivor.Gameplay.Progression.Runtime;
using TouhouWuxiaSurvivor.Ui.Stats;
using TouhouWuxiaSurvivor.Ui.Stats.Build;

namespace TouhouWuxiaSurvivor.Tests.Integration;

/// <summary>
/// 验证构筑页筛选、节点选择与详情联动均为只读，并且核心浏览不依赖滚动容器。
/// </summary>
public partial class CharacterBuildViewSmokeTest : Node
{
    /// <summary>
    /// 用真实升级目录建立混合构筑，依次切换筛选和节点并检查运行状态不被界面修改。
    /// </summary>
    public override async void _Ready()
    {
        CharacterBuildView? view = null;
        int exitCode = 0;
        try
        {
            var build = new RunBuildState();
            RunUpgradeDefinition damage = RunUpgradeCatalog.FindById("needle_damage")!;
            RunUpgradeDefinition breathing = RunUpgradeCatalog.FindById("hakurei_breathing")!;
            Require(build.Apply(damage) && build.Apply(damage) && build.Apply(breathing),
                "Could not prepare a representative build.");
            int ranksBefore = build.TotalRanks;
            CharacterBuildViewModel model = CharacterBuildViewModelFactory.Create(
                build, ContentPackSelection.BaseOnly, 6);
            view = GD.Load<PackedScene>("res://src/ui/stats/CharacterBuildView.tscn")
                .Instantiate<CharacterBuildView>();
            AddChild(view);
            view.SetModel(model);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

            Require(view.FindChildren("*", "ScrollContainer").Count == 0,
                "Build view introduced a scroll container for core information.");
            Require(view.Graph.VisibleNodeCount > 0 && view.SelectedNodeId is not null,
                "Build graph did not initialize visible or selected nodes.");
            view.SelectFilter(CharacterBuildFilter.Learned);
            Require(view.CurrentFilter == CharacterBuildFilter.Learned &&
                view.Graph.VisibleNodeCount == 2,
                "Learned filter did not expose the two acquired foundations.");
            Require(view.Graph.SelectNode(damage.Id),
                "Graph could not select an acquired node by stable id.");
            Label detail = view.GetNode<Label>("Body/DetailsFrame/Details/Name");
            Require(detail.Text == damage.DisplayName,
                "Selecting a node did not update the fixed detail pane.");
            Label effect = view.GetNode<Label>("Body/DetailsFrame/Details/Effect");
            Require(effect.Text.Contains("当前：", StringComparison.Ordinal) &&
                effect.Text.Contains("下一重：", StringComparison.Ordinal),
                "Build details do not distinguish active and next-rank effects.");
            Require(view.GetNode<Label>("Summary/Role").Text.Contains(
                    model.CombatRoleName, StringComparison.Ordinal) &&
                view.GetNode<Label>("Summary/Spells").Text.Contains("攻0/4", StringComparison.Ordinal),
                "Build summary omitted character role or spell-slot capacity.");
            Require(build.TotalRanks == ranksBefore && build.GetRank(damage.Id) == 2,
                "Inspecting the build graph mutated the live build.");
            view.SelectFilter(CharacterBuildFilter.SpellCard);
            Require(view.Graph.VisibleNodeCount == 6,
                "Base-only build view must expose the complete permanent 4+2 spell pool.");
            GD.Print("Character build view smoke test passed.");
        }
        catch (Exception exception)
        {
            exitCode = 1;
            GD.PushError(exception.ToString());
        }
        finally
        {
            view?.QueueFree();
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            GetTree().Quit(exitCode);
        }
    }

    /// <summary>
    /// 将构筑交互契约失败转换为包含具体原因的测试异常。
    /// </summary>
    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
