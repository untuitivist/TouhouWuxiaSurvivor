using Godot;
using TouhouWuxiaSurvivor.Gameplay.SpellCards.Definitions;
using TouhouWuxiaSurvivor.Gameplay.SpellCards.Runtime;
using TouhouWuxiaSurvivor.Ui.Hud.SpellCards;

namespace TouhouWuxiaSurvivor.Tests.Integration;

/// <summary>
/// 验证奥义 HUD 的稳定排序、逐卡冷却遮罩、被动待机提示及节点复用，不依赖真实时间等待。
/// </summary>
public partial class SpellCardHudTest : Node
{
    /// <summary>使用两张真实本体奥义构造可控快照，并检查遮罩下边缘按剩余周期向上收缩。</summary>
    public override async void _Ready()
    {
        int exitCode = 0;
        var strip = new SpellCardHudStrip { Size = new Vector2(183.0f, 28.0f) };
        try
        {
            AddChild(strip);
            SpellCardDefinition offensive = SpellCardCatalog.All.First(card =>
                card.SourcePackId == "base" &&
                SpellCardSlotPolicy.Classify(card) == SpellCardSlotKind.Offensive);
            SpellCardDefinition support = SpellCardCatalog.All.First(card =>
                card.SourcePackId == "base" &&
                SpellCardSlotPolicy.Classify(card) == SpellCardSlotKind.Support);
            SpellCardRuntimeSnapshot first = CreateSnapshot(
                support, 2.0f, false, offensive, 8.0f, false);
            strip.SetSnapshot(first);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

            SpellCardCooldownIcon offensiveIcon = strip.GetIcon(0);
            SpellCardCooldownIcon supportIcon = strip.GetIcon(1);
            Require(strip.Visible && strip.VisibleIconCount == 2 &&
                offensiveIcon.CardId == offensive.Id && supportIcon.CardId == support.Id,
                "HUD icons were not ordered by offensive then support slot.");
            Require(Mathf.IsEqualApprox(offensiveIcon.CooldownRatio, 0.8f) &&
                Mathf.IsEqualApprox(offensiveIcon.GetCooldownMaskRect().Size.Y, 23.0f),
                "Initial cooldown mask did not cover the expected top-anchored height.");

            SpellCardRuntimeSnapshot second = CreateSnapshot(
                support, 0.0f, false, offensive, 3.0f, false);
            strip.SetSnapshot(second);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            Require(ReferenceEquals(offensiveIcon, strip.GetIcon(0)) &&
                offensiveIcon.GetCooldownMaskRect().Size.Y == 9.0f,
                "Cooldown refresh rebuilt icons or failed to shrink the mask upward.");
            Require(supportIcon.IsWaitingForCondition && supportIcon.CooldownRatio == 0.0f &&
                supportIcon.TooltipText.Contains("等待战况条件", StringComparison.Ordinal),
                "Passive spell readiness was still presented as an active cooldown.");
            GD.Print("Spell card HUD test passed.");
        }
        catch (Exception exception)
        {
            GD.PushError(exception.ToString());
            exitCode = 1;
        }
        finally
        {
            strip.QueueFree();
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            GetTree().Quit(exitCode);
        }
    }

    /// <summary>故意以护持在前建立快照，确认展示层不会继承输入集合的偶然顺序。</summary>
    private static SpellCardRuntimeSnapshot CreateSnapshot(
        SpellCardDefinition support,
        float supportRemaining,
        bool supportTriggered,
        SpellCardDefinition offensive,
        float offensiveRemaining,
        bool offensiveTriggered) => new(
            [support, offensive],
            [
                new(support, 10.0f, supportRemaining, supportTriggered),
                new(offensive, 10.0f, offensiveRemaining, offensiveTriggered),
            ]);

    /// <summary>将任一显示契约失败转换为带有具体原因的测试异常。</summary>
    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
